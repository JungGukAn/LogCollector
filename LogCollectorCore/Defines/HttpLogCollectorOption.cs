using System;
using System.Collections.Generic;

namespace LogCollectorCore
{
    public class HttpLogCollectorOption
    {
        /// <summary>
        /// Default value is '/'
        /// </summary>
        public string BaseUri { get; set; } = "/";

        /// <summary>
        /// Default value is 2000
        /// </summary>
        public int RequestMaxCountAtOnce { get; set; } = 2000;

        /// <summary>
        /// Default value is 1
        /// </summary>
        public int ConcurrentCount { get; set; } = 1;

        /// <summary>
        /// Default value is 300
        /// </summary>
        public int FlushIntervalSeconds { get; set; } = 300;

        /// <summary>
        /// Default value is none
        /// </summary>
        public int? FlushMaxCount { get; set; } = null;

        public List<Formatters.ILogFormatter> LogFormatters { get; } = new List<Formatters.ILogFormatter>
        {
            new Formatters.BsonFormatter(),
            new Formatters.JsonFormatter(),
            new Formatters.MessagePackFormatter()
        };

        public List<Compresser.ILogCompresser> LogCompressers { get; } = new List<Compresser.ILogCompresser>
        {
            new Compresser.GZipCompresser()
        };

        public List<LogPipeline> Pipelines { get; } = new List<LogPipeline>();

        public HttpLogCollectorOption()
        {
            var defaultPipeline = new LogPipeline.Builder(null);
            defaultPipeline.Add((tag, logs) =>
            {
                Console.WriteLine($"Tag: {tag}, Count: {logs.Count}");
            });
        }

        public void RegistPipeline(string pattern, Action<LogPipeline.Builder> pipelineConfigure)
        {
            var pipelineBuilder = new LogPipeline.Builder(pattern);
            pipelineConfigure?.Invoke(pipelineBuilder);

            Pipelines.Add(pipelineBuilder.ToPipeline());
        }

        public bool IsInvalid(out string invalidReason)
        {
            invalidReason = default;

            if (string.IsNullOrWhiteSpace(BaseUri))
            {
                invalidReason = "BaseUri must not be null or empty.";
                return true;
            }

            if (BaseUri.StartsWith('/') == false)
            {
                invalidReason = "BaseUri must start with '/'";
                return true;
            }

            if (BaseUri.EndsWith('/'))
            {
                invalidReason = "BaseUri can not end with '/'";
                return true;
            }

            try
            {
                var uri = new Uri(BaseUri);
            }
            catch
            {
                invalidReason = "BaseUri invalid uri format.";
                return true;
            }

            if (RequestMaxCountAtOnce <= 0)
            {
                invalidReason = "RequestMaxCountAtOnce must be greater than zero.";
                return true;
            }

            if (ConcurrentCount <= 0)
            {
                invalidReason = "ConcurrentCount must be greater than zero.";
                return true;
            }

            if (FlushIntervalSeconds <= 0)
            {
                invalidReason = "FlushIntervalSeconds must be greater than zero.";
                return true;
            }

            if (FlushMaxCount != null && FlushMaxCount <= 0)
            {
                invalidReason = "FlushMaxCount can not be equal or less than zero.";
                return true;
            }

            if (LogFormatters.Count <= 0)
            {
                invalidReason = "LogFormatters must have at least one item.";
                return true;
            }

            if (Pipelines.Count <= 0)
            {
                invalidReason = "Pipelines must have at least one item.";
                return true;
            }

            return false;
        }
    }
}
