using System.ServiceModel;

namespace ProcessExec.Service
{
    // TODO: Support for multiple processes (Process.Handle), process started message, etc
    public interface IJobObjectServiceCallback
    {
        [OperationContract(IsOneWay = true)]
        void ProcessErrorReceived(string error);

        [OperationContract(IsOneWay = true)]
        void ProcessOutputReceived(string output);

        [OperationContract(IsOneWay = true)]
        void ProcessExit(int exitCode);

        [OperationContract(IsOneWay = true)]
        void ServiceMessageReceived(string message);
    }
}
