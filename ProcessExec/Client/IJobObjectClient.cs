using System;
using ProcessExec.Service;

namespace ProcessExec.Client
{
    public interface IJobObjectClient : IJobObjectService
    {
        void Register();
        void Unregister();

        event EventHandler<EventArgs<string>> OutputReceived;
        event EventHandler<EventArgs<string>> ErrorReceived;
        event EventHandler<EventArgs<int>> ProcessExited;
        event EventHandler<EventArgs<string>> ServiceMessageReceived;
    }
}
