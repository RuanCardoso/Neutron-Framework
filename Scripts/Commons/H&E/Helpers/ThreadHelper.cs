using System.Runtime.CompilerServices;
using System.Threading;

namespace NeutronNetwork.Helpers
{
    public static class ThreadHelper
    {
        public static int GetThreadID()
        {
            return Thread.CurrentThread.ManagedThreadId;
        }

        public static void DoNotAllowSimultaneousAccess(int managedThreadId, [CallerMemberName] string callerMemberName = "")
        {
            if (GetThreadID() != managedThreadId)
                LogHelper.Error($"{callerMemberName} can only be called from the Neutron thread");
        }

        public static bool DoNotAllowOnClientSide(string funcName)
        {
#if !UNITY_SERVER
            return LogHelper.Error($"This function[{funcName}] is not available on the client side.");
#else
            return true;
#endif
        }

        public static bool DoNotAllowOnServerSide(string funcName)
        {
#if UNITY_SERVER
            return LogHelper.Error($"This function[{funcName}] is not available on the server side.");
#else
            return true;
#endif
        }
    }
}