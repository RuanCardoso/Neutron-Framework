﻿using System.Runtime.CompilerServices;
using System.Threading;

namespace NeutronNetwork.Helpers
{
    public static class ThreadHelper
    {
        public static int GetThreadID()
        {
            return GetThread().ManagedThreadId;
        }

        public static Thread GetThread()
        {
            return Thread.CurrentThread;
        }

        public static void DoNotAllowSimultaneousAccess(int managedThreadId, [CallerMemberName] string callerMemberName = "")
        {
            if (GetThreadID() != managedThreadId)
                LogHelper.Error($"{callerMemberName} can only be called from the Neutron thread");
        }
    }
}