using System;
using System.Threading;
using Mono.Unix;
using Mono.Unix.Native;

namespace LogCollectorCore
{
    public class UnixProcessExitHandler
    {
        public static event Action ProcessExit;

        static UnixProcessExitHandler()
        {
            var thread = new Thread(CatchSignal);
            thread.Start();
        }

        static void CatchSignal()
        {
            UnixSignal[] unixSignals = new UnixSignal[]
            {
                new UnixSignal(Signum.SIGHUP),
                new UnixSignal(Signum.SIGINT),
                new UnixSignal(Signum.SIGQUIT),
                new UnixSignal(Signum.SIGABRT),
                new UnixSignal(Signum.SIGALRM),
                new UnixSignal(Signum.SIGTERM),
            };

            try
            {
                UnixSignal.WaitAny(unixSignals, -1);
            }
            catch { }

            ProcessExit?.Invoke();
        }
    }
}
