using NeutronNetwork.Packets;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace NeutronNetwork.Internal
{
    public class NeutronSocket
    {
        private Socket _socket;
        private SocketMode _socketMode;
        private Protocol _protocol;
        private bool _isListen;

        private readonly List<NeutronSocket> _sockets = new();

        public void Init(SocketMode socketMode, Protocol protocol, NonAllocEndPoint endPoint, int backLog)
        {
            if (endPoint == null)
                throw new Exception("Endpoint is null!");

            if (protocol == Protocol.Tcp)
            {
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _socket.Bind(endPoint);
            }

            if (socketMode == SocketMode.Server && protocol == Protocol.Tcp)
            {
                if (backLog <= 0)
                    throw new Exception("Invalid backlog value!");

                _socket.Listen(backLog);
                _isListen = true;
            }

            _socketMode = socketMode;
            _protocol = protocol;
        }

        public void Connect(NonAllocEndPoint endPoint)
        {
            CheckConnectIsValidForThisSocket(endPoint);
            _socket.Connect(endPoint);
        }

        public Task ConnectTAsync(NonAllocEndPoint endPoint)
        {
            CheckConnectIsValidForThisSocket(endPoint);
            return _socket.ConnectAsync(endPoint);
        }

        public ValueTask ConnectVTAsync(NonAllocEndPoint endPoint)
        {
            CheckConnectIsValidForThisSocket(endPoint);
            return new ValueTask(ConnectTAsync(endPoint));
        }

        public NeutronSocket AcceptSocket()
        {
            CheckAcceptIsValidForThisSocket();
            NeutronSocket socket = new() { _socket = _socket.Accept() };
            _sockets.Add(socket);
            return socket;
        }

        public async Task<NeutronSocket> AcceptSocketTAsync()
        {
            CheckAcceptIsValidForThisSocket();
            NeutronSocket socket = new() { _socket = await _socket.AcceptAsync() };
            _sockets.Add(socket);
            return socket;
        }

        public ValueTask<NeutronSocket> AcceptSocketVTAsync()
        {
            CheckAcceptIsValidForThisSocket();
            return new ValueTask<NeutronSocket>(AcceptSocketTAsync());
        }

        public void Receive()
        {
            //_socket.Receive()
        }

        public void Close()
        {
            try
            {
                if (_isListen)
                {
                    foreach (NeutronSocket socket in _sockets)
                        socket.Close();
                    _sockets.Clear();
                }

                if (_socket.Connected)
                    _socket.Shutdown(SocketShutdown.Both);
            }
            finally
            {
                _socket.Close();
            }
        }

        private void CheckAcceptIsValidForThisSocket()
        {
            if ((_socketMode != SocketMode.Server && _protocol != Protocol.Tcp) || _socket == null)
                throw new Exception("This property is not valid for this socket.");
        }

        private void CheckConnectIsValidForThisSocket(NonAllocEndPoint endPoint)
        {
            if (endPoint == null)
                throw new Exception("Endpoint is null!");

            if (endPoint.Address == IPAddress.Any)
                throw new Exception("Invalid IP Address!");

            if ((_socketMode == SocketMode.Server) || _socket == null)
                throw new Exception("This property is not valid for this socket.");
        }
    }

    public enum SocketMode
    {
        Server,
        Client,
    }
}