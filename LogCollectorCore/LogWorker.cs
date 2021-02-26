using System;
using System.Collections.Concurrent;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace LogCollectorCore
{
    public class LogWorker
    {
        Thread thread;
        CancellationTokenSource cts = new CancellationTokenSource();
        ConcurrentQueue<Action> actions = new ConcurrentQueue<Action>();
        ILogger logger;

        public LogWorker(ILoggerFactory loggerFactory)
        {
            logger = loggerFactory.CreateLogger(nameof(LogWorker));
        }

        public bool Add(Action action)
        {
            if (!cts.Token.IsCancellationRequested)
            {
                actions.Enqueue(action);
                return true;
            }

            return false;
        }

        private void Update()
        {
            var token = cts.Token;

            while(!token.IsCancellationRequested)
            {
                if (actions.TryDequeue(out var action))
                {
                    try
                    {
                        action?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex.Message);
                    }
                }

                Thread.Sleep(10);
            }
        }

        public void Start()
        {
            thread = new Thread(Update);
            thread.Start();
        }

        public void Stop()
        {
            try { cts.Cancel(); } catch { }
            try { thread.Join(); } catch { }

            cts.Dispose();
        }
    }
}
