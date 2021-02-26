using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Net;

namespace LogCollectorCore
{
    public class LogCollectorMiddleware
    {
        static readonly string method = "POST";

        readonly string startPath = "/";
        readonly System.Threading.CancellationTokenSource _cts;
        readonly System.Threading.Thread _thread;
        readonly HttpLogCollectorOption _option;
        readonly IHostApplicationLifetime _lifetime;
        readonly RequestDelegate _next;
        readonly ILoggerFactory _loggerFactory;
        readonly ILogger _logger;

        ConcurrentQueue<LogRequest> requests = new ConcurrentQueue<LogRequest>();
        Dictionary<string, LogBuffer> buffers = new Dictionary<string, LogBuffer>();

        uint sequence = 0;
        List<LogWorker> processors = new List<LogWorker>();

        public LogCollectorMiddleware(RequestDelegate next, HttpLogCollectorOption option, IHostApplicationLifetime lifetime, ILoggerFactory loggerFactory)
        {
            _option = option;
            _lifetime = lifetime;
            _next = next;
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger(nameof(LogCollectorMiddleware));

            if (_option.ConcurrentCount <= 0)
            {
                throw new ArgumentException($"'ConcurrentCount' must equal or larger then 1. Value: {_option.ConcurrentCount}");
            }

            startPath = _option.BaseUri;
            _logger.LogTrace($"LogProcessor count {_option.ConcurrentCount}");

            for (int i = 0; i < _option.ConcurrentCount; i++)
            {
                var processor = new LogWorker(_loggerFactory);
                processor.Start();

                _logger.LogTrace($"Processor id: {i} start");

                processors.Add(processor);
            }

            _cts = new System.Threading.CancellationTokenSource();

            _thread = new System.Threading.Thread(Update);
            _thread.Start();

            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                _lifetime.StopApplication();
            };

            AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
            {
                _lifetime.StopApplication();
            };

            UnixProcessExitHandler.ProcessExit += _lifetime.StopApplication;

            _lifetime.ApplicationStopped.Register(() =>
            {
                try { _cts.Cancel(); } catch { }
                try { _thread.Join(); } catch { }

                _cts.Dispose();
            });
        }

        private IEnumerator<LogWorker> GetProcessorIterator()
        {
            while(true)
            {
                // Interlocked.Increment is not necessary, but not perfectly distributed.
                uint nextSequence = ++sequence;
                int nextIndex = (int)(nextSequence % processors.Count);

                yield return processors[nextIndex];
            }
        }

        private void Update()
        {
            var processorIterator = GetProcessorIterator();
            var token = _cts.Token;

            while (!token.IsCancellationRequested)
            {
                int requestCount = requests.Count;
                int requestProcessCount = requestCount > _option.RequestMaxCountAtOnce ? _option.RequestMaxCountAtOnce : requestCount;

                for(int i = 0; i < requestProcessCount; i++)
                {
                    if (requests.TryDequeue(out var request) == false)
                        break;

                    if (buffers.TryGetValue(request.TagLog.Tag, out var buffer) == false)
                    {
                        buffer = new LogBuffer(request.TagLog.Tag, request.Outputs, _option.FlushIntervalSeconds, _option.FlushMaxCount);
                        buffers.Add(buffer.Tag, buffer);
                    }

                    buffer.Add(request.TagLog.Log);
                }

                List<string> removedBufferTags = null;

                foreach(var buffer in buffers.Values)
                {
                    if (buffer.TryGetChunk(out var chunk) && processorIterator.MoveNext())
                    {
                        if (buffer.IsEmpty)
                        {
                            if (removedBufferTags == null)
                            {
                                removedBufferTags = new List<string>();
                            }

                            removedBufferTags.Add(buffer.Tag);
                        }

                        var currentProcessor = processorIterator.Current;
                        currentProcessor.Add(()=>
                        {
                            foreach(var output in buffer.Outputs)
                            {
                                output.Invoke(buffer.Tag, chunk);
                            }
                        });
                    }
                }

                if (removedBufferTags != null)
                {
                    foreach(var removeBufferTag in removedBufferTags)
                    {
                        buffers.Remove(removeBufferTag);
                    }
                }

                System.Threading.Thread.Sleep(5);
            }

            _logger.LogInformation("Stop all processes.");

            _logger.LogInformation("Fetch all remained logs.");

            while (requests.TryDequeue(out var request))
            {
                if (buffers.TryGetValue(request.TagLog.Tag, out var buffer) == false)
                {
                    buffer = new LogBuffer(request.TagLog.Tag, request.Outputs, _option.FlushIntervalSeconds, _option.FlushMaxCount);
                    buffers.Add(buffer.Tag, buffer);
                }

                buffer.Add(request.TagLog.Log);
            }

            _logger.LogInformation("Output all remained logs.");

            foreach (var buffer in buffers.Values)
            {
                while(buffer.TryGetChunkForce(out var chunk) && processorIterator.MoveNext())
                {
                    var currentProcessor = processorIterator.Current;
                    currentProcessor.Add(() =>
                    {
                        foreach (var output in buffer.Outputs)
                        {
                            output.Invoke(buffer.Tag, chunk);
                        }
                    });
                }
            }

            _logger.LogInformation("Stop log processors.");

            foreach (var processor in processors)
            {
                processor.Stop();
            }
        }

        private async Task<byte[]> ReadBody(HttpRequest request)
        {
            int contentLength = (int)request.ContentLength.GetValueOrDefault();
            if (contentLength == 0)
            {
                return null;
            };

            try
            {
                var body = new byte[contentLength];

                int totalReadByteCount = 0;

                while (totalReadByteCount < contentLength)
                {
                    int readByteCount = await request.Body.ReadAsync(body, totalReadByteCount, contentLength - totalReadByteCount);
                    if (readByteCount == 0)
                    {
                        return null;
                    }

                    totalReadByteCount += readByteCount;
                }

                return body;
            }
            catch
            {
                return null;
            }
        }

        LogDynamicObject Convert(byte[] body, Compresser.ILogCompresser compresser, Formatters.ILogFormatter formatter)
        {
            try
            {
                if (compresser != null)
                {
                    body = compresser.Decompress(body);
                }

                return formatter.Convert(body);
            }
            catch(Exception ex)
            {
                _logger.LogWarning(ex.Message);
                return null;
            }
        }

        Formatters.ILogFormatter GetFormatter(string contentType)
        {
            var formatter = _option.LogFormatters?.Find(formatter => formatter.SupportMediaType.Equals(contentType, StringComparison.OrdinalIgnoreCase));
            return formatter;
        }

        Compresser.ILogCompresser GetCompresser(IHeaderDictionary headers)
        {
            Compresser.ILogCompresser compresser = null;

            if (headers.TryGetValue("Content-Encoding", out var headerValues))
            {
                foreach (var headerValue in headerValues)
                {
                    if (string.IsNullOrWhiteSpace(headerValue) == false)
                    {
                        compresser = _option.LogCompressers?.Find(compresser => compresser.Identifier.Equals(headerValue));

                        if (compresser != null)
                            break;
                    }
                }
            }

            return compresser;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var request = context.Request;

            bool isStartsWithLogs = request.Path.StartsWithSegments(startPath, out var remaining);
            if (isStartsWithLogs && remaining.HasValue && request.Method.Equals(method, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    _logger.LogTrace($"Add tag {remaining.Value}");

                    var tags = remaining.Value.Split('/', StringSplitOptions.RemoveEmptyEntries).First().Split('.', StringSplitOptions.RemoveEmptyEntries).ToList();

                    var formatter = GetFormatter(request.ContentType);
                    if (formatter == null)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.ExpectationFailed;
                        return;
                    }

                    Compresser.ILogCompresser compresser = GetCompresser(request.Headers);

                    var body = await ReadBody(request);
                    if (body == null)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        return;
                    }

                    var log = Convert(body, compresser, formatter);
                    if (log == null)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        return;
                    }

                    foreach(var pipeline in _option.Pipelines)
                    {
                        if (pipeline.IsMatch(tags))
                        {
                            var tagLog = new TagLog(log);
                            tagLog.Tag = tags;

                            foreach (var filter in pipeline.Filters)
                            {
                                bool isPass = filter.Invoke(tagLog);
                                if (isPass == false)
                                {
                                    _logger.LogTrace($"Forbid tag {tagLog.Tag}");
                                    context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                                    return;
                                }
                            }

                            requests.Enqueue(new LogRequest { TagLog = tagLog, Outputs = pipeline.Outputs });
                            context.Response.StatusCode = (int)HttpStatusCode.OK;
                            return;
                        }
                    }

                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex.Message);

                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    await context.Response.WriteAsync(ex.Message);
                }
            }
            else
            {
                // Call the next delegate/middleware in the pipeline
                await _next(context);
            }
        }
    }
}
