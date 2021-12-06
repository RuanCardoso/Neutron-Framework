using NeutronNetwork.Wrappers;
using System;
using System.Net.Sockets;
using System.Threading;

namespace NeutronNetwork.Internal
{
    public class UserToken
    {
        internal NeutronPool<SocketAsyncEventArgs> PooledSocketAsyncEventArgsForReceive
        {
            get;
            set;
        }

        internal NeutronPool<SocketAsyncEventArgs> PooledSocketAsyncEventArgsForSend
        {
            get;
            set;
        }

        internal NeutronBlockingQueue<SocketAsyncEventArgs> AsyncEventArgsBlockingQueue
        {
            get;
        } = new();

        internal CancellationTokenSource SourceToken
        {
            get;
            set;
        }

        public NeutronSocket Socket
        {
            get;
            set;
        }
    }
}