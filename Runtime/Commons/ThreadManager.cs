using NeutronNetwork.Internal;
using NeutronNetwork.Internal.Wrappers;
using System.Runtime.CompilerServices;
using System.Threading;

namespace NeutronNetwork
{
    public class ThreadManager
    {
        private readonly NeutronSafeDictionary<int, int> _methodIds = new NeutronSafeDictionary<int, int>();
        private readonly NeutronSafeDictionary<string, int> _methodNames = new NeutronSafeDictionary<string, int>();

        /// <summary>
        ///* Retorna "True" se outro thread usar o recurso.
        /// </summary>
        /// <param name="methodId"></param>
        /// <returns></returns>
        public bool BlockSimultaneousAccess(int methodId)
        {
            int threadId = Thread.CurrentThread.ManagedThreadId;
            if (!_methodIds.TryAdd(methodId, threadId))
            {
                if (threadId != _methodIds[methodId])
                    return true;
            }
            return false;
        }

        /// <summary>
        ///* Avisa se outro thread usar o recurso.
        /// </summary>
        /// <param name="methodId"></param>
        /// <returns></returns>
        public void WarnSimultaneousAccess(int methodId)
        {
            int threadId = Thread.CurrentThread.ManagedThreadId;
            if (!_methodIds.TryAdd(methodId, threadId))
            {
                if (threadId != _methodIds[methodId])
                    throw new NeutronException($"Simultaneous access detected! -> original thread id: {_methodIds[methodId]} | current thread id: {threadId} | methodId: {methodId}");
            }
        }

        /// <summary>
        ///* Retorna "True" se outro thread usar o recurso.
        /// </summary>
        /// <param name="methodName"></param>
        /// <returns></returns>
        public bool BlockSimultaneousAccess([CallerMemberName] string methodName = null)
        {
            int threadId = Thread.CurrentThread.ManagedThreadId;
            if (!_methodNames.TryAdd(methodName, threadId))
            {
                if (threadId != _methodNames[methodName])
                    return true;
            }
            return false;
        }

        /// <summary>
        ///* Avisa se outro thread usar o recurso.
        /// </summary>
        /// <param name="methodName"></param>
        /// <returns></returns>
        public void WarnSimultaneousAccess([CallerMemberName] string methodName = null)
        {
            int threadId = Thread.CurrentThread.ManagedThreadId;
            if (!_methodNames.TryAdd(methodName, threadId))
            {
                if (threadId != _methodNames[methodName])
                    throw new NeutronException($"Simultaneous access detected! -> original thread id: {_methodNames[methodName]} | current thread id: {threadId} | methodName: {methodName}");
            }
        }
    }
}
