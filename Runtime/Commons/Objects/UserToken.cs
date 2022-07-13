using NeutronNetwork.Internal.Wrappers;
using NeutronNetwork.Wrappers;
using System;
using System.Net.Sockets;
using System.Threading;

namespace NeutronNetwork.Internal
{
    public class UserToken
    {
        internal int Offset
        {
            get;
            set;
        }

        internal int Count
        {
            get;
            set;
        } = 4;

        internal int SkipOffset
        {
            get;
            set;
        }

        internal byte[] Buffer
        {
            get;
            set;
        } = new byte[8192];

        internal NeutronSafeQueueNonAlloc<byte[]> DataToSendQueue
        {
            get;
        } = new();

        internal Socket socket;



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

        // internal NeutronSocket Socket
        // {
        //     get;
        //     set;
        // }

        public int isReceiving = 0;
        public AutoResetEvent AutoResetEvent { get; set; } = new(false);
        public long BytesTransferredPerSecond { get; set; }
        public long PacketsTransferredPerSecond { get; set; }

        public int IsSending;

        public DateTime OldTime { get; set; }

        public int Pps;

        public double BytesTransferred = 1;

    }
}