using System;

namespace ProcessExec.Client
{
    public class ProcessEventArgs : EventArgs
    {
        public string StdOut { get; set; }
        public string StdError { get; set; }
        public int ExitCode { get; set; }

        public ProcessEventArgs(string stdOut, string stdError, int exitCode)
        {
            StdOut = stdOut;
            StdError = stdError;
            ExitCode = exitCode;
        }
    }
}
