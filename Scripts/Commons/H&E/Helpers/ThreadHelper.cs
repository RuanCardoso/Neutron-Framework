using System;
using System.Runtime.CompilerServices;
using System.Threading;
using NeutronNetwork;

public static class ThreadHelper
{
    public static int GetThreadID()
    {
        return Thread.CurrentThread.ManagedThreadId;
    }

    public static void DoNotAllowSimultaneousAccess(int ManagedThreadId, [CallerMemberName] string CallerMemberName = "")
    {
        if (GetThreadID() != ManagedThreadId)
            LogHelper.Error($"{CallerMemberName} can only be called from the Neutron thread");
    }
}