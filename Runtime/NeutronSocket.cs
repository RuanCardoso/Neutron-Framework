using NeutronNetwork.Constants;
using NeutronNetwork.Internal.Packets;
using NeutronNetwork.Packets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

// Created by: Ruan Cardoso(Brasil)
// Email: neutron050322@gmail.com
// License: MIT
namespace NeutronNetwork.Internal
{
    /// <summary>
    /// This class is responsible for handling the network connection.
    /// </summary>
    public class NeutronSocket
    {
        /// <summary>
        /// This is a pool of socket events.
        /// </summary>
        internal static NeutronPool<SocketAsyncEventArgs> PooledSocketAsyncEventArgsForAccept
        {
            get;
            set;
        }

        /// <summary>
        /// The socket that is used to listen for incoming connections.
        /// </summary>
        private Socket _socket;
        /// <summary>
        /// The mode of the socket.
        /// </summary>
        private SocketMode _socketMode;
        /// <summary>
        /// The protocol of the socket.
        /// </summary>
        private Protocol _protocol;
        /// <summary>
        /// Indicates if the socket is listening for incoming connections.
        /// </summary>
        private bool _isListen;
        /// <summary>
        /// Store the state of connected client.
        /// </summary>
        private UserToken _userToken;

        private SocketAsyncEventArgs _socketAsyncEventArgsForConnect = new();
        /// <summary>
        /// The token used to cancel the asynchronous operations and loops.
        /// </summary>
        /// <returns></returns>
        private readonly CancellationTokenSource _sourceToken = new();
        /// <summary>
        /// Store the connected clients, used to dispose the unmanaged resources.
        /// </summary>
        private List<NeutronSocket> _sockets;

        /// <summary>
        /// Returns the state of the socket.
        /// </summary>
        public bool IsConnected => !_sourceToken.IsCancellationRequested;

        /// <summary>
        /// Initialize the socket on the remote host.
        /// </summary>
        public void Init(SocketMode socketMode, Protocol protocol, EndPoint endPoint, int backLog = 0)
        {
            if (endPoint == null)
                throw new Exception("Endpoint is null!"); // If the endpoint is null, throw an exception.

            if (protocol == Protocol.Tcp)
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); // Initialize the tcp socket.
            else if (protocol == Protocol.Udp)
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp); // Initialize the udp socket.

            _socket.Bind(endPoint); // Bind the socket to the endpoint.

            if (socketMode == SocketMode.Server && protocol == Protocol.Tcp)
            {
                // If the socket is a server and the protocol is tcp, listen for incoming connections.

                if (backLog <= 0)
                    backLog = 10; // If the back log is less than or equal to zero, set it to 10.

                _socket.Listen(backLog); // Listen for incoming connections.
                _sockets = new(); // Initialize the list of connected clients.
                _isListen = true; // Set the socket to listen for incoming connections.
            }

            _socketMode = socketMode; // Set the socket mode.
            _protocol = protocol; // Set the protocol.
        }

        /// <summary>
        /// Connect to the remote host.
        /// </summary>
        /// <param name="endPoint"></param>
        public bool ConnectAsync(EndPoint endPoint)
        {
            CheckConnectIsValidForThisSocket(endPoint); // Check if the socket is valid for this operation.
            SocketAsyncEventArgs socketAsyncEventArgs = _socketAsyncEventArgsForConnect; // Get the socket event args.
            socketAsyncEventArgs.Completed += OnIOCompleted; // Set the completed event.
            socketAsyncEventArgs.RemoteEndPoint = endPoint; // Set the remote endpoint.
            bool value = _socket.ConnectAsync(socketAsyncEventArgs); // Connect to the remote host.
            if (!value)
                ProcessConnectedAsync(socketAsyncEventArgs); // If the connect is completed synchronously, process the connected event.
            return value; // Return the connect result.
        }

        /// <summary>
        /// This event is triggered when the socket is connected.
        /// </summary>
        /// <param name="socketAsyncEventArgs"></param>
        private void ProcessConnectedAsync(SocketAsyncEventArgs socketAsyncEventArgs)
        {
            if (_sourceToken.IsCancellationRequested)
                return; // If the token is cancelled, return.

            if (socketAsyncEventArgs.SocketError != SocketError.Success)
                throw new NeutronException($"Failed to connect client! -> {socketAsyncEventArgs.SocketError}"); // If the socket error is not success, throw an exception.

            socketAsyncEventArgs.Completed -= OnIOCompleted; // Remove the completed event.
        }

        /// <summary>
        /// Accept the incoming connection.
        /// </summary>
        /// <returns></returns>
        public bool AcceptAsync()
        {
            CheckAcceptIsValidForThisSocket(); // Check if the socket is valid for this operation.
            SocketAsyncEventArgs socketAsyncEventArgs = PooledSocketAsyncEventArgsForAccept.Pull(); // Get the socket event args.
            socketAsyncEventArgs.Completed += OnIOCompleted; // Set the completed event.
            bool value = _socket.AcceptAsync(socketAsyncEventArgs); // Accept the incoming connection.
            if (!value)
                ProcessAcceptedSocket(socketAsyncEventArgs); // If the accept is completed synchronously, process the accepted socket.
            return value; // Return the accept result.
        }

        /// <summary>
        /// Process the accepted socket.
        /// </summary>
        /// <param name="socketAsyncEventArgs"></param>
        private void ProcessAcceptedSocket(SocketAsyncEventArgs socketAsyncEventArgs)
        {
            if (_sourceToken.IsCancellationRequested)
                return; // If the token is cancelled, return.

            UserToken userToken = new(); // Create a new user token.
            userToken.AsyncEventArgsBlockingQueue = new(); // Initialize the blocking queue, it's used to process de received data.

            NeutronSocket neutronSocket = new()
            {
                _socket = socketAsyncEventArgs.AcceptSocket,
                _userToken = userToken
            }; // Create a new socket to new connected client.

            userToken.SourceToken = neutronSocket._sourceToken; // Set the token to the user token.
            userToken.Socket = neutronSocket; // Set the socket to the user token.

            _sockets.Add(neutronSocket); // Add the new socket to the list of connected clients.

            AcceptAsync(); // Accept the next incoming connection.

            if (socketAsyncEventArgs.SocketError != SocketError.Success)
            {
                if (!LogHelper.Error($"Failed to accept client! -> {socketAsyncEventArgs.SocketError}"))
                {
                    if (_sockets.Remove(neutronSocket))
                        neutronSocket.Close(); // If the socket is removed from the list of connected clients, close the socket.
                    else
                        throw new NeutronException("Accept: Socket not found!"); // If the socket is not found, throw an exception.
                }

                return; // If the socket error is not success, return.
            }

            LogHelper.Error("Client Accepted!");

            userToken.PooledSocketAsyncEventArgsForReceive = new(() => new(), NeutronConstants.MAX_CAPACITY_FOR_EVENT_ARGS_IN_RECEIVE_POOL, false, "IOReceiveEventPool");
            for (int i = 0; i < NeutronConstants.MAX_CAPACITY_FOR_EVENT_ARGS_IN_RECEIVE_POOL; i++)
                userToken.PooledSocketAsyncEventArgsForReceive.Push(new()); // Initialize the pool of socket event args for receive.

            SocketAsyncEventArgs receiveArgs = userToken.PooledSocketAsyncEventArgsForReceive.Pull(); // Get the socket event args.
            receiveArgs.Completed += OnIOCompleted; // Set the completed event.
            receiveArgs.UserToken = userToken; // Set the user token.
            receiveArgs.AcceptSocket = socketAsyncEventArgs.AcceptSocket; // Set the accept socket.

            receiveArgs.SetBuffer(new Memory<byte>(new byte[1024])); // Set the buffer to receive data.

            socketAsyncEventArgs.Completed -= OnIOCompleted; // Remove the completed event.
            socketAsyncEventArgs.AcceptSocket = null; // Set the accept socket to null.
            PooledSocketAsyncEventArgsForAccept.Push(socketAsyncEventArgs); // Push the socket event args to the pool.

            StartReceive(receiveArgs); // Start the receive.
            ProcessAsyncEventArgs(userToken); // Process the async event args/received data.
        }

        /// <summary>
        /// Start the receive data from the remote host.
        /// </summary>
        /// <param name="socketAsyncEventArgs"></param>
        private void StartReceive(SocketAsyncEventArgs socketAsyncEventArgs)
        {
            try
            {
                if (!socketAsyncEventArgs.AcceptSocket.ReceiveAsync(socketAsyncEventArgs))
                    ProcessReceivedData(socketAsyncEventArgs); // If the receive is completed synchronously, process the received data.
            }
            catch (Exception ex)
            {
                LogHelper.Stacktrace(ex);
            }
        }

        /// <summary>
        /// Process the received data and handling disconnection.
        /// </summary>
        /// <param name="socketAsyncEventArgs"></param>
        private void ProcessReceivedData(SocketAsyncEventArgs socketAsyncEventArgs)
        {
            try
            {
                UserToken userToken = (UserToken)socketAsyncEventArgs.UserToken; // Get the user token.

                if (_sourceToken.IsCancellationRequested || userToken.SourceToken.IsCancellationRequested)
                    return; // If the token is cancelled, return.

                if (socketAsyncEventArgs.SocketError != SocketError.Success)
                {
                    if (!LogHelper.Error($"Failed to reiceive data! -> {socketAsyncEventArgs.SocketError}"))
                    {
                        if (_sockets.Remove(userToken.Socket))
                            userToken.Socket.Close(); // If the socket is removed from the list of connected clients, close the socket.
                        else
                            throw new NeutronException("Receive: Socket not found!"); // If the socket is not found, throw an exception.
                    }

                    return;
                }

                if (socketAsyncEventArgs.BytesTransferred > 0)
                {
                    SocketAsyncEventArgs receiveArgs = userToken.PooledSocketAsyncEventArgsForReceive.Pull(); // Get the socket event args.
                    receiveArgs.Completed += OnIOCompleted; // Set the completed event.
                    receiveArgs.UserToken = userToken; // Set the user token.
                    receiveArgs.AcceptSocket = socketAsyncEventArgs.AcceptSocket; // Set the accept socket.
                    receiveArgs.SetBuffer(new Memory<byte>(new byte[1024])); // Set the buffer to receive data.

                    userToken.AsyncEventArgsBlockingQueue.Add(socketAsyncEventArgs, userToken.SourceToken.Token); // Add the socket event args to the blocking queue to process the received data.

                    StartReceive(receiveArgs); // Receive the next data.

                    return;
                }
                // when the client is disconnected.....
                socketAsyncEventArgs.Completed -= OnIOCompleted;
                socketAsyncEventArgs.UserToken = null;
                socketAsyncEventArgs.AcceptSocket = null;
                userToken.PooledSocketAsyncEventArgsForReceive.Push(socketAsyncEventArgs); // Push the socket event args to the pool.
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                LogHelper.Stacktrace(ex);
            }
        }

        /// <summary>
        /// Process the received data with message framing.
        /// Part 1: Message framing: Read the prefix of the message and the length of the message and then read the message.
        /// </summary>
        /// <param name="userToken"></param>
        private void ProcessAsyncEventArgs(UserToken userToken)
        {
            try
            {
                MemoryStream dataStream = new(); // Create a new memory stream to store the received data.
                int totalBytesTransferred = 0; // The total bytes transferred/received.
                int currentOffset = 0; // The current offset in the stream.
                byte[] data = new byte[8192]; // The data to be processed.

                while (!_sourceToken.IsCancellationRequested && !userToken.SourceToken.IsCancellationRequested)
                {
                    // Stop 'while' processing if the cancellation token is requested, to prevent high CPU usage.
                    if (ReadExactly(dataStream, userToken, data, ref currentOffset, 4))
                    {
                        // Read exactly 4 bytes(integer) from the stream, if the read is completed, process the received data.
                        // 4 bytes is the length of the message, the length of the message is the first 4 bytes(prefix) of the message/packet.
                        // 4 bytes = 32 bits = integer(int), the maximum length of the message is 2^32 - 1 bytes.

                        int messageLength = BitConverter.ToInt32(data[..4]); // Slice the first 4 bytes(prefix) to get the length of the message and convert it to integer.
                        messageLength += 4; // Add 4 bytes(prefix) to the length of the message, this is the real length of the packet, including the prefix....

                        int fLength = messageLength - 4;
                        if (fLength > 512 || fLength <= 0)
                        {
                            // If the length of the message is greater than 512 bytes, ignore the message, if size is less or equal to 0 bytes, ignore the message, because the message is empty.
                            // Dos attack, disconnect the client.
                            break;
                        }

                        while ((currentOffset < messageLength) && (!_sourceToken.IsCancellationRequested && !userToken.SourceToken.IsCancellationRequested))
                        {
                            // Stop 'while' processing if the cancellation token is requested, to prevent high CPU usage.
                            if (ReadExactly(dataStream, userToken, data, ref currentOffset, messageLength))
                            {
                                // Read exactly the length of the message from the stream, if the read is completed, process the received data.

                                byte[] messageData = data[4..messageLength]; // Slice the data from the prefix to the length of the message to get the message/packet.
                                int bytesRemaining = totalBytesTransferred - currentOffset; // Get the remaining bytes to be read.

                                LogHelper.Info("Pckt completed: ");

                                if (messageData.Length != fLength)
                                    throw new NeutronException("Header: Invalid range!");

                                byte[] nextData = null;
                                if (bytesRemaining > 0)
                                    nextData = dataStream.ToArray()[currentOffset..(currentOffset + bytesRemaining)]; // Get the previous data in the stream.

                                dataStream.SetLength(0); // Clear the stream.

                                if (bytesRemaining > 0 && nextData.Length > 0)
                                {
                                    if (nextData.Length != bytesRemaining)
                                        throw new NeutronException("Next Data: Invalid range!");

                                    // copy the remaining data to the beginning of the stream.
                                    dataStream.Write(nextData); // If there are remaining bytes to be read, write them to the stream.
                                    dataStream.Position = 0; // Set the position of the stream to the beginning.
                                }

                                currentOffset = 0; // Reset the current offset.
                                totalBytesTransferred = bytesRemaining; // Reset the total bytes transferred.

                                break; // Break the 'while' loop.
                            }
                            else
                                GetRemainingData(dataStream, userToken, ref currentOffset, ref totalBytesTransferred); // Get the left data from the socket and add it to the stream.
                        }
                    }
                    else
                        GetRemainingData(dataStream, userToken, ref currentOffset, ref totalBytesTransferred); // Get the left data from the socket and add it to the stream.
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                LogHelper.Stacktrace(ex);
            }
        }

        /// <summary>
        /// Part 2: Message framing: Get the remaining data from the socket and add it to the stream.
        /// </summary>
        private void GetRemainingData(Stream stream, UserToken userToken, ref int offset, ref int totalBytesTransferred)
        {
            var args = userToken.AsyncEventArgsBlockingQueue.Take(userToken.SourceToken.Token); // Get the socket event args from the queue and block until the data is received.
            int bytesTransferred = args.BytesTransferred; // Get the bytes transferred.

            if (bytesTransferred > 0)
            {
                // if the bytes transferred is greater than 0, add the data to the stream.

                var chunkBuffer = args.MemoryBuffer[..bytesTransferred]; // Slice the data to get the data.
                stream.Write(chunkBuffer.ToArray(), 0, bytesTransferred); // Write the data to the stream.
                stream.Position = offset; // Set the position of the stream to the current offset.

                totalBytesTransferred += bytesTransferred; // Add the bytes transferred to the total bytes transferred.
            }

            args.Completed -= OnIOCompleted;
            args.UserToken = null;
            args.AcceptSocket = null;
            userToken.PooledSocketAsyncEventArgsForReceive.Push(args);
        }

        /// <summary>
        /// Part 3: Message Framing: Read exactly X bytes from the stream.
        /// </summary>
        private bool ReadExactly(Stream stream, UserToken userToken, byte[] buffer, ref int offset, int size)
        {
            while ((offset < size) && (!_sourceToken.IsCancellationRequested && !userToken.SourceToken.IsCancellationRequested))
            {
                // Stop 'while' processing if the cancellation token is requested, to prevent high CPU usage.

                int bytesLeftToRead = size - offset; // Get the bytes left to read.
                int bytesRead = stream.Read(buffer, offset, bytesLeftToRead); // Read the data from the stream.
                if (bytesRead > 0)
                    offset += bytesRead; // Add the bytes read to the offset.
                else
                    return false; // If the bytes read is 0, the read is not completed.
            }

            return offset == size; // Return true if the read is completed.
        }

        public int Send(byte[] buffer)
        {
            return _socket.Send(buffer, SocketFlags.None); // Send the data to the socket.
        }

        public bool SendAsync(byte[] buffer)
        {
            if (_userToken == null)
                throw new NeutronException("This operation is not valid for this socket.");

            SocketAsyncEventArgs socketAsyncEventArgs = _userToken.PooledSocketAsyncEventArgsForSend.Pull(); // Get the socket event args.
            socketAsyncEventArgs.Completed += OnIOCompleted; // Set the completed event.
            bool value = _socket.SendAsync(socketAsyncEventArgs); // Send the data to the socket.
            if (!value)
                ProcessSendedData(socketAsyncEventArgs); // if the send is completed synchronously, process the data.
            return value;
        }

        private void ProcessSendedData(SocketAsyncEventArgs socketAsyncEventArgs)
        {

        }

        /// <summary>
        /// This event is trigerred when the socket finishes a operation.
        /// </summary>
        private void OnIOCompleted(object sender, SocketAsyncEventArgs socketAsyncEventArgs)
        {
            switch (socketAsyncEventArgs.LastOperation)
            {
                case SocketAsyncOperation.Accept:
                    ProcessAcceptedSocket(socketAsyncEventArgs); // Process the accepted socket.
                    break;
                case SocketAsyncOperation.Receive:
                    ProcessReceivedData(socketAsyncEventArgs); // Process the received data.
                    break;
                case SocketAsyncOperation.Connect:
                    ProcessConnectedAsync(socketAsyncEventArgs); // Process the connected socket.
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSendedData(socketAsyncEventArgs); // Process the sent data.
                    break;
            }
        }

        public void Close()
        {
            try
            {
                _sourceToken.Cancel(); // Cancel the source token, this stop all the async operations.
                if (_userToken != null)
                    _userToken.SourceToken.Cancel(); // Stop the user token, this stop all the async operations for the user.

                if (_isListen)
                {
                    // If the socket is listening, is it a server socket? in this case, the server socket must be closed, but before that, the client sockets must be closed.
                    foreach (NeutronSocket socket in _sockets.ToArray())
                    {
                        if (_sockets.Remove(socket))
                        {
                            // If the socket is removed from the list, close the socket.
                            if (socket.IsConnected)
                                socket.Close(); // Close the socket.
                            else { /*continue*/ }
                        }
                    }
                }

                if (_socket.Connected)
                    _socket.Shutdown(SocketShutdown.Both); // Shutdown the socket, wait for the all the data to be sent and received.
            }
            finally
            {
                _socket?.Close(); // Close the socket.
                _sourceToken?.Dispose(); // Dispose the cancellation token, free the resources.
                if (_userToken != null)
                {
                    _userToken.AsyncEventArgsBlockingQueue?.Dispose(); // Dispose the blocking queue.
                    _userToken.SourceToken?.Dispose(); // Dispose the cancellation token, free the resources.
                }
            }
        }

        private void CheckAcceptIsValidForThisSocket()
        {
            if ((_socketMode != SocketMode.Server && _protocol != Protocol.Tcp) || _socket == null)
                throw new Exception("This property is not valid for this socket."); // If the socket is not a server socket and the protocol is not TCP, throw an exception.
        }

        private void CheckConnectIsValidForThisSocket(EndPoint endPoint)
        {
            if (endPoint == null)
                throw new Exception("Endpoint is null!");

            if (((IPEndPoint)endPoint).Address == IPAddress.Any)
                throw new Exception("Invalid IP Address!");

            if ((_socketMode == SocketMode.Server) || _socket == null)
                throw new Exception("This operation is not valid for this socket."); // If the socket is a server socket, throw an exception.
        }
    }
}