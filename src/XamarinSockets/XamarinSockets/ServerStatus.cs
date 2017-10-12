using System;
using System.Collections.Generic;
using System.Text;

namespace XamarinSockets
{
    public enum ServerStatus
    {
        Started,
        Stopped
    }
    public enum ServerFailedToStart
    {
        ServerIsRunning
    }
    public enum ServerfailedToStop
    {
        ServerIsNotRunning
    }
}
