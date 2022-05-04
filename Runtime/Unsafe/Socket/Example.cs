// using NeutronNetwork.Internal.Packets;
// using NeutronNetwork.Packets;
// using System;
// using System.Collections.Generic;
// using System.IO;
// using System.Net;
// using System.Net.Sockets;
// using System.Threading;

// // Created by: Ruan Cardoso(Brasil)
// // Email: neutron050322@gmail.com
// // License: MIT
// namespace NeutronNetwork.Internal
// {
//     /// <summary>
//     /// This class is responsible for handling the network connection.
//     /// </summary>
//     internal class NeutronSocket
//     {
//         #region Fields/Properties
//         /// <summary>
//         /// The total operations supported by the socket.
//         /// </summary>
//         private const int SOCK_OPS = 2;

//         /// <summary>
//         /// Store the connected clients, used to close and dispose the unmanaged resources.
//         /// </summary>
//         private List<NeutronSocket> _sockets; // Initialized on Server Only.

//         /// <summary>
//         /// This is a pool of socket events, used for best performance.
//         /// Used to accept new connections.
//         /// </summary>
//         internal static NeutronPool<SocketAsyncEventArgs> PooledSocketAsyncEventArgsForAccept
//         {
//             get;
//             set;
//         }

//         /// <summary>
//         /// The token used to cancel the asynchronous operations and loops.
//         /// </summary>
//         /// <returns></returns>
//         private readonly CancellationTokenSource _sourceToken = new();

//         /// <summary>
//         /// Returns the state of the socket.
//         /// </summary>
//         private bool IsConnected => !_sourceToken.IsCancellationRequested;

//         /// <summary>
//         /// This class creates a single large buffer which can be divided up.
//         /// and assigned to SocketAsyncEventArgs objects for use with each.
//         /// socket I/O operation.
//         /// This enables buffers to be easily reused and guards against.
//         /// fragmenting heap memory.
//         /// The operations exposed on the NeutronBuffer class are not thread safe.
//         /// </summary>
//         private NeutronBuffer _bufferManager;

//         /// <summary>
//         /// Store the state of connected client.
//         /// And assigned to SocketAsyncEventArgs objects for use with each.
//         /// </summary>
//         private UserToken _userToken;

//         /// <summary>
//         /// This is a socket, it's used to receive and send between two endpoints.
//         /// </summary>
//         private Socket _socket;

//         /// <summary>
//         /// The mode of the socket, used to set if a socket is a server or a client.
//         /// </summary>
//         private SocketMode _socketMode;

//         /// <summary>
//         /// The protocol of the socket, UDP and TCP are supported.
//         /// </summary>
//         private Protocol _protocol;

//         /// <summary>
//         /// The max buffer size of the socket.
//         /// </summary>
//         private int _receiveSize, _sendSize;

//         /// <summary>
//         /// The max message size to be received from the clients.
//         /// </summary>
//         private int _maxMessageSize;
//         #endregion

//         #region Events
//         /// <summary>
//         /// This event is triggered when a new Socket is accepted or this Socket is connected.
//         /// </summary>
//         internal NeutronEventWithReturn<bool> OnSocketConnected;
//         /// <summary>
//         /// This event is triggered when a complete message is received.
//         /// </summary>
//         internal NeutronEventWithReturn<bool> OnMessageCompleted;
//         #endregion

//         /// <summary>
//         /// Initialize the socket on the remote host.
//         /// </summary>
//         public void Init(SocketMode socketMode, Protocol protocol, EndPoint endPoint, int backLog = 0)
//         {
//             if (endPoint == null)
//                 throw new Exception("Endpoint is null!"); // If the endpoint is null, throw an exception.

//             if (protocol == Protocol.Tcp)
//                 _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); // Create a new TCP Socket.
//             else if (protocol == Protocol.Udp)
//                 _socket = new Socket(endPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp); // Create a new UDP Socket.

//             //ConfigureSocket(_socket, _receiveSize, _sendSize, NeutronModule.Settings._NoDelay); // Configure the tcp socket.

//             // If true, sets the receive and send buffer size to zero.
//             // Prevents a new buffer to be allocated each time a new message is received.
//             // Prevent a new copy of the buffer.
//             // This a good practice to reduce the memory usage and CPU usage, increase the performance.
//             // This options is not available to UDP sockets.
//             bool zeroStack = NeutronModule.Settings._ZeroStack;
//             // Sets the max players supported by the server.
//             // When the max players is reached, the server will not accept new connections.
//             // This option is not available to UDP sockets.
//             int maxPlayers = NeutronModule.Settings.MaxPlayers;
//             // Sets the max message size to be received from the clients.
//             int maxMessageSize = NeutronModule.Settings._PacketSize;
//             // Multiplier the size of the NeutronBuffer.
//             // Neutron Buffer is used to store the data received from the clients.
//             // The Multiplier not affect the internal buffer size.
//             int bufferMultiplier = NeutronModule.Settings._MultiplierSize;
//             // Sets the max size of the internal buffer.
//             // The size of NeutronBuffer not affected by this value.
//             int bufferRecSize = !zeroStack ? NeutronModule.Settings._RecBufferSize : 0;
//             // Sets the max size of the internal buffer.
//             // The size of NeutronBuffer not affected by this value.
//             int bufferSendSize = !zeroStack ? NeutronModule.Settings._SendBufferSize : 0;

//             // Create a new NeutronBuffer.
//             // The NeutronBuffer is used to store the data received from the clients.
//             // This buffer is independent of the size of the internal buffer(Send/Recv).
//             // The size is maxMessageSize * maxPlayers * SOCK_OPS * bufferMultiplier, maxMessageSize is the size of the message.
//             _bufferManager = new NeutronBuffer(maxMessageSize * maxPlayers * SOCK_OPS * bufferMultiplier, maxMessageSize);
//             _bufferManager.Init();

//             // Bind the socket to the endpoint.
//             // This is the address and port of the server, used to listen for incoming connections.
//             _socket.Bind(endPoint); // Bind the socket to the endpoint.

//             if (socketMode == SocketMode.Server && protocol == Protocol.Tcp)
//             {
//                 // If the socket is a server and the protocol is tcp, listen for incoming connections.

//                 if (backLog <= 0)
//                     backLog = 10; // Backlog is the maximum length of the pending connections queue.

//                 _socket.Listen(backLog); // Start listening for incoming connections.
//                 _sockets = new(); // Initialize the list of connected clients.
//             }

//             _socketMode = socketMode;
//             _protocol = protocol;

//             _receiveSize = bufferRecSize;
//             _sendSize = bufferSendSize;
//             _maxMessageSize = maxMessageSize;

//             //ConfigureSocket(_socket, _receiveSize, _sendSize, NeutronModule.Settings._NoDelay); // Configure the tcp socket.
//         }

//         private void ConfigureSocket(Socket tcpSocket, int recvSize, int sentSize, bool noDelay)
//         {
//             // Disable the Nagle Algorithm for this tcp socket.
//             // When NoDelay is false, a TcpClient does not send a packet over the network until it has collected a significant amount of outgoing data.
//             // Because of the amount of overhead in a TCP segment, sending small amounts of data is inefficient.
//             // However, situations do exist where you need to send very small amounts of data or expect immediate responses from each packet you send.
//             // Your decision should weigh the relative importance of network efficiency versus application requirements.
//             // If you are sending small amounts of data, you should set NoDelay to true.
//             // If you are sending large amounts of data, you should set NoDelay to false.
//             // Used to improve the performance of the TCP protocol for specific situations.
//             tcpSocket.NoDelay = noDelay;
//             // The ReceiveBufferSize property gets or sets the number of bytes that you are expecting to store in the receive buffer for each read operation.
//             // This property actually manipulates the network buffer space allocated for receiving incoming data.
//             tcpSocket.ReceiveBufferSize = recvSize;
//             // The SendBufferSize property gets or sets the number of bytes that you are expecting to send in each call to the Write method. 
//             // This property actually manipulates the network buffer space allocated for send operation.
//             tcpSocket.SendBufferSize = sentSize;
//         }

//         /// <summary>
//         /// Connect to the remote host.
//         /// </summary>
//         /// <param name="endPoint"></param>
//         public bool ConnectAsync(EndPoint endPoint)
//         {
//             CheckConnectIsValidForThisSocket(endPoint); // Check if the socket is valid for this operation.
//             SocketAsyncEventArgs socketAsyncEventArgs = new SocketAsyncEventArgs();
//             socketAsyncEventArgs.Completed += OnIOCompleted;
//             socketAsyncEventArgs.RemoteEndPoint = endPoint; // Remote endpint used to connect to the remote host.
//             bool value = _socket.ConnectAsync(socketAsyncEventArgs);
//             if (!value)
//                 ProcessConnectedAsync(socketAsyncEventArgs); // If the connect is completed synchronously, process the connected event.
//             return value; // Return the connect result.
//         }

//         public void Connect(string host, int port)
//         {
//             _socket.Connect(host, port);
//         }

//         /// <summary>
//         /// This event is triggered when the socket is connected.
//         /// </summary>
//         /// <param name="socketAsyncEventArgs"></param>
//         private void ProcessConnectedAsync(SocketAsyncEventArgs socketAsyncEventArgs)
//         {
//             if (_sourceToken.IsCancellationRequested)
//                 return; // If the token is cancelled, return.

//             if (socketAsyncEventArgs.SocketError != SocketError.Success)
//             {
//                 // Dont need this anymore, dispose it.
//                 socketAsyncEventArgs.Dispose();
//                 throw new NeutronException($"Failed to connect client! -> {socketAsyncEventArgs.SocketError}");
//             }

//             LogHelper.Error("Conexão bem success!");

//             // Dont need this anymore, dispose it.
//             socketAsyncEventArgs.Dispose();
//         }

//         /// <summary>
//         /// Start accepting incoming connections or receiving data.
//         /// </summary>
//         /// <returns></returns>
//         public void Start()
//         {
//             if (_socketMode == SocketMode.Server)
//             {
//                 SocketAsyncEventArgs socketAsyncEventArgs = PooledSocketAsyncEventArgsForAccept.Pull();
//                 socketAsyncEventArgs.Completed += OnIOCompleted;
//                 if (!_socket.AcceptAsync(socketAsyncEventArgs))
//                     ProcessAcceptedSocket(socketAsyncEventArgs); // If the accept is completed synchronously, process the accepted socket.
//             }
//             else
//                 CreatePool(GenerateUserTokenAndCreateSocket(null), _socket); // Create the pool of sockets and user token.
//         }

//         private UserToken GenerateUserTokenAndCreateSocket(SocketAsyncEventArgs socketAsyncEventArgs)
//         {
//             // If a socketAsyncEventArgs is null, use this Socket, otherwise, create a new Socket.
//             // If a socketAsyncEventArgs is null, is a client socket, otherwise, is a server socket.
//             NeutronSocket clientSocket = (socketAsyncEventArgs != null) ? new() : this;
//             if (socketAsyncEventArgs != null)
//                 clientSocket._socket = socketAsyncEventArgs.AcceptSocket; // If a server socket, set the socket to the accepted socket.

//             // This is the user token, used to store the state of the connection between socket operations.
//             clientSocket._userToken = new();
//             // If a new socket is created, set the socket mode to client, if not, not important.
//             clientSocket._socketMode = SocketMode.Client;
//             // If a new socket is created, set the protocol to tcp, if not, not important.
//             clientSocket._protocol = Protocol.Tcp;

//             UserToken userToken = clientSocket._userToken;
//             userToken.SourceToken = clientSocket._sourceToken;
//             userToken.Socket = clientSocket;
//             return userToken;
//         }

//         private void CreatePool(UserToken userToken, Socket acceptSocket)
//         {
//             // Let's pre-allocate the pool of sockets.
//             int socketReceiveArgsCount = NeutronModule.Settings.SocketReceive;
//             userToken.PooledSocketAsyncEventArgsForReceive = new(() => new SocketAsyncEventArgs(), socketReceiveArgsCount, false, "Receive Pool");
//             for (int i = 0; i < socketReceiveArgsCount; i++)
//             {
//                 SocketAsyncEventArgs eventArgs = new();
//                 eventArgs.Completed += OnIOCompleted;
//                 eventArgs.UserToken = userToken;
//                 eventArgs.AcceptSocket = acceptSocket;

//                 // Now let's pre-allocate a reserved space for the receive buffer.
//                 if (_bufferManager.Set(eventArgs))
//                     userToken.PooledSocketAsyncEventArgsForReceive.Push(eventArgs);
//                 else
//                     eventArgs.Dispose(); // If the buffer is full, dispose the event args to free the memory.
//             }

//             int socketSendArgsCount = NeutronModule.Settings.SocketSend;
//             userToken.PooledSocketAsyncEventArgsForSend = new(() => new SocketAsyncEventArgs(), socketSendArgsCount, false, "Send Pool");
//             for (int i = 0; i < socketSendArgsCount; i++)
//             {
//                 SocketAsyncEventArgs eventArgs = new();
//                 eventArgs.Completed += OnIOCompleted;
//                 eventArgs.UserToken = userToken;
//                 eventArgs.AcceptSocket = acceptSocket;
//                 userToken.PooledSocketAsyncEventArgsForSend.Push(eventArgs);
//             }
//         }

//         /// <summary>
//         /// Process the accepted socket.
//         /// </summary>
//         /// <param name="socketAsyncEventArgs"></param>
//         private void ProcessAcceptedSocket(SocketAsyncEventArgs socketAsyncEventArgs)
//         {
//             if (_sourceToken.IsCancellationRequested)
//                 return; // If the token is cancelled, return.

//             if (socketAsyncEventArgs.SocketError != SocketError.Success)
//             {
//                 if (!LogHelper.Error($"Failed to accept client! -> {socketAsyncEventArgs.SocketError}"))
//                     return;
//             }

//             UserToken userToken = GenerateUserTokenAndCreateSocket(socketAsyncEventArgs);
//             userToken.socket = socketAsyncEventArgs.AcceptSocket;
//             _sockets.Add(userToken.Socket); // Add the new socket to the list of connected clients.

//             CreatePool(userToken, socketAsyncEventArgs.AcceptSocket);

//             SocketAsyncEventArgs receiveArgs = userToken.PooledSocketAsyncEventArgsForReceive.Pull();
//             ConfigureSocket(receiveArgs.AcceptSocket, _receiveSize, _sendSize, NeutronModule.Settings._NoDelay); // Configure the socket.

//             receiveArgs.SetBuffer(/*userToken.Offset*/0, 256);
//             if (!receiveArgs.AcceptSocket.ReceiveAsync(receiveArgs))
//                 ProcessReceivedData(receiveArgs, userToken, _protocol, _socketMode); // If the receive is completed synchronously, process the received data.

//             //ReceiveAsync(receiveArgs, userToken); // If the receive is completed synchronously, process the received data.

//             // Recycle the Accept Args, so it can be used again.
//             socketAsyncEventArgs.Completed -= OnIOCompleted;
//             socketAsyncEventArgs.AcceptSocket = null;
//             PooledSocketAsyncEventArgsForAccept.Push(socketAsyncEventArgs);

//             // Accept the next connection.
//             Start();


//             //ReceiveAsync(receiveArgs, userToken); // Receive data from the client.
//         }

//         /// <summary>
//         /// Start the receive data from the remote host.
//         /// </summary>
//         /// <param name="socketAsyncEventArgs"></param>
//         private void ReceiveAsync(SocketAsyncEventArgs socketAsyncEventArgs, UserToken userToken)
//         {
//             // Part 1: Message Framing -> Let's read the exact number of bytes we expect.
//             try
//             {
//                 if (userToken.Offset > userToken.Count)
//                     throw new NeutronException("Offset is greater than count!");

//                 if (userToken.Count > 0)
//                 {
//                     // This is the left bytes to read to complete the expected data.
//                     int bytesLeftToRead = userToken.Count - userToken.Offset;
//                     socketAsyncEventArgs.SetBuffer(/*userToken.Offset*/0, bytesLeftToRead);
//                     if (!socketAsyncEventArgs.AcceptSocket.ReceiveAsync(socketAsyncEventArgs))
//                         ProcessReceivedData(socketAsyncEventArgs, userToken, _protocol, _socketMode); // If the receive is completed synchronously, process the received data.
//                 }
//                 else
//                     throw new Exception("Count must be greater than zero!");
//             }
//             catch (Exception ex)
//             {
//                 LogHelper.Stacktrace(ex);
//             }
//         }

//         private void SendAsync(SocketAsyncEventArgs socketAsyncEventArgs, byte[] sData = null)
//         {
//             try
//             {
//                 var data = sData == null ? _userToken.DataToSendQueue.Pull() : sData;
//                 if (data != null)
//                 {
//                     socketAsyncEventArgs.SetBuffer(data, 0, data.Length);
//                     if (!socketAsyncEventArgs.AcceptSocket.SendAsync(socketAsyncEventArgs))
//                         ProcessSendedData(socketAsyncEventArgs); // If the receive is completed synchronously, process the received data.
//                 }
//                 else
//                 {
//                     if (_socketMode == SocketMode.Client)
//                     {
//                         Interlocked.Exchange(ref _userToken.IsSending, 0);
//                         _userToken.PooledSocketAsyncEventArgsForSend.Push(socketAsyncEventArgs);
//                     }
//                     else
//                         throw new NeutronException("This is a server socket, there is no data to send!");
//                 }
//             }
//             catch (Exception ex)
//             {
//                 LogHelper.Stacktrace(ex);
//             }
//         }

//         private void ProcessSendedData(SocketAsyncEventArgs socketAsyncEventArgs)
//         {
//             try
//             {
//                 UserToken userToken = (UserToken)socketAsyncEventArgs.UserToken; // Get the user token.

//                 if (userToken.SourceToken.IsCancellationRequested)
//                     return; // If the token is cancelled, return.

//                 if (socketAsyncEventArgs.SocketError != SocketError.Success)
//                 {
//                     if (!LogHelper.Error($"Failed to send data! -> {socketAsyncEventArgs.SocketError}"))
//                         userToken.PooledSocketAsyncEventArgsForSend.Push(socketAsyncEventArgs);
//                     return;
//                 }

//                 if (_socketMode == SocketMode.Client)
//                 {
//                     // LogHelper.Error("Send: Client To Server");

//                     // Receive the response.
//                     //ReceiveAsync(userToken.PooledSocketAsyncEventArgsForReceive.Pull(), userToken);
//                 }
//                 else if (_socketMode == SocketMode.Server)
//                 {
//                     // LogHelper.Error("Send: Server To Client");

//                     // Receive again, all over again.
//                     //ReceiveAsync(userToken.PooledSocketAsyncEventArgsForReceive.Pull(), userToken);
//                 }

//                 userToken.PooledSocketAsyncEventArgsForSend.Push(socketAsyncEventArgs);
//             }
//             catch (Exception ex)
//             {
//                 LogHelper.Stacktrace(ex);
//             }
//         }

//         /// <summary>
//         /// Process the received data and handling disconnection.
//         /// </summary>
//         /// <param name="socketAsyncEventArgs"></param>
//         private void ProcessReceivedData(SocketAsyncEventArgs socketAsyncEventArgs, UserToken userToken, Protocol protocol, SocketMode socketMode)
//         {
//             if (socketAsyncEventArgs.BytesTransferred > 0)
//             {
//                 userToken.Pps++;
//                 userToken.BytesTransferred += socketAsyncEventArgs.BytesTransferred;

//                 double sec = DateTime.Now.Subtract(userToken.OldTime).Seconds;
//                 if (sec > 0)
//                 {
//                     LogHelper.Error($"{userToken.Pps / sec}:{userToken.BytesTransferred / sec} Bytes/s");
//                     userToken.OldTime = DateTime.Now;
//                     userToken.Pps = 0;
//                     userToken.BytesTransferred = 0;
//                 }


//                 socketAsyncEventArgs.SetBuffer(socketAsyncEventArgs.Offset, socketAsyncEventArgs.BytesTransferred);
//                 if (!userToken.socket.ReceiveAsync(socketAsyncEventArgs))
//                     ProcessReceivedData(socketAsyncEventArgs, userToken, protocol, socketMode);
//                 return;
//             }
//             return;
//             //  userToken.PooledSocketAsyncEventArgsForReceive.Push(socketAsyncEventArgs);
//             // Part 2: Message Framing -> Read the length of the message and then read the message.
//             try
//             {
//                 if (_sourceToken.IsCancellationRequested || userToken.SourceToken.IsCancellationRequested)
//                     return; // If the token is cancelled, return.

//                 if (socketAsyncEventArgs.SocketError != SocketError.Success)
//                 {
//                     if (!LogHelper.Error($"Failed to reiceive data! -> {socketAsyncEventArgs.SocketError}"))
//                     {
//                         // when the client is disconnected.....
//                         userToken.PooledSocketAsyncEventArgsForReceive.Push(socketAsyncEventArgs); // Push the socket event args to the pool.

//                         if (socketMode == SocketMode.Server)
//                         {
//                             if (_sockets.Remove(userToken.Socket))
//                                 userToken.Socket.Close(); // If the socket is removed from the list of connected clients, close the socket.
//                             else
//                                 throw new NeutronException("Receive: Socket not found!"); // If the socket is not found, throw an exception.
//                         }
//                         else
//                             userToken.Socket.Close(); // If the socket is a client, close the socket.
//                     }

//                     return;
//                 }

//                 int bytesTransferred = socketAsyncEventArgs.BytesTransferred; // Get the bytes transferred/received.
//                 if (bytesTransferred > 0)
//                 {
//                     if (bytesTransferred > userToken.Count)
//                         throw new NeutronException($"Transferred: Received more data than expected! {bytesTransferred} > {userToken.Count}"); // If the number of bytes received is greater than the expected number of bytes, throw an exception.

//                     ReadOnlySpan<byte> sockBuffer = new(socketAsyncEventArgs.Buffer, 0, bytesTransferred); // Get the exactly transferred data.
//                     // Let's copy the received data to the user token buffer.
//                     for (int i = 0; i < bytesTransferred; i++)
//                         userToken.Buffer[userToken.Offset++] = sockBuffer[i];

//                     if (userToken.Offset > userToken.Count)
//                         throw new NeutronException($"Offset: Received more data than expected! {userToken.Offset} > {userToken.Count}"); // If the number of bytes received is greater than the expected number of bytes, throw an exception.

//                     if (userToken.Offset == userToken.Count)
//                     {
//                         ReadOnlySpan<byte> usrBuffer = userToken.Buffer;
//                         if (userToken.Offset == 4 && userToken.Count != userToken.SkipOffset)
//                         {
//                             // Read the size of the message.
//                             ReadOnlySpan<byte> lengthOfMessage = usrBuffer[..4];
//                             // Convert the size of the message to an integer(4 Bytes).
//                             int length = BitConverter.ToInt32(lengthOfMessage);
//                             // Prevent the user from sending a message that is too large or too small, causing the server to crash.
//                             // Dos attack prevention.
//                             if (length > 8192 || length <= 0)
//                                 throw new Exception($"Length: Invalid length! -> [{length}]: {BitConverter.ToString(lengthOfMessage.ToArray())}");
//                             // When the length is 4 bytes, Skip the 4 bytes and read the message.
//                             if (length == 4)
//                                 userToken.SkipOffset = length;

//                             // Set the next message to be received.
//                             userToken.Offset = 0;
//                             userToken.Count = length;

//                             // Receive the message.
//                             ReceiveAsync(userToken.PooledSocketAsyncEventArgsForReceive.Pull(), userToken);
//                         }
//                         else
//                         {
//                             // When the skipped 4 bytes, set to zero.
//                             if (userToken.SkipOffset > 0)
//                                 userToken.SkipOffset = 0;

//                             // Let's process the received data.
//                             if (userToken.Offset > 0)
//                             {
//                                 ReadOnlySpan<byte> message = usrBuffer[..userToken.Offset];
//                                 if (message.Length != userToken.Count)
//                                     throw new Exception($"Message: Invalid message length! -> {message.Length} != {userToken.Count}");

//                                 // Set the next message to be received.
//                                 userToken.Offset = 0;
//                                 userToken.Count = 4;

//                                 ReceiveAsync(userToken.PooledSocketAsyncEventArgsForReceive.Pull(), userToken);

//                                 // // Process the received data.
//                                 // if (_socketMode == SocketMode.Server)
//                                 // {
//                                 //     // LogHelper.Error("Recv: Server From Client");
//                                 //     using (NeutronStream nsStream = Neutron.PooledNetworkStreams.Pull())
//                                 //     {
//                                 //         var nsWriter = nsStream.Writer;
//                                 //         nsWriter.Write(message.Length);
//                                 //         nsWriter.Write(message);
//                                 //         SendAsync(userToken.PooledSocketAsyncEventArgsForSend.Pull(), nsWriter.GetBufferAsReadOnlySpan().ToArray());
//                                 //     }
//                                 // }
//                                 // else
//                                 // {
//                                 //     // LogHelper.Error("Recv: Client From Server");
//                                 //     SendAsync(userToken.PooledSocketAsyncEventArgsForSend.Pull());
//                                 // }
//                             }
//                             else
//                                 throw new Exception("Offset: Invalid length! -> " + userToken.Offset);
//                         }
//                     }
//                     else
//                         ReceiveAsync(userToken.PooledSocketAsyncEventArgsForReceive.Pull(), userToken); // Receive more data.
//                 }
//                 else
//                     LogHelper.Error($"Disconnected! -> Status: {socketAsyncEventArgs.SocketError} -> Mode: {_socketMode} -> {userToken.Offset}:{userToken.Count}");

//                 // Recycle, we don't need anymore.
//                 userToken.PooledSocketAsyncEventArgsForReceive.Push(socketAsyncEventArgs);
//             }
//             catch (OperationCanceledException) { }
//             catch (Exception ex)
//             {
//                 LogHelper.Stacktrace(ex);
//             }
//         }

//         /// <summary>
//         /// Process the received data with message framing.
//         /// Part 1: Message framing: Read the prefix of the message and the length of the message and then read the message.
//         /// </summary>
//         /// <param name="userToken"></param>
//         // private void ProcessAsyncEventArgs(UserToken userToken)
//         // {
//         //     try
//         //     {
//         //         MemoryStream dataStream = new(); // Create a new memory stream to store the received data.
//         //         Memory<byte> dataMemory = new byte[_maxMessageSize]; // The data to be processed.
//         //         Memory<byte> nextPacketData = new byte[8192];

//         //         int totalBytes = 0; // The total number of bytes received.
//         //         int totalPackets = 0; // The total number of packets completed.
//         //         int totalBytesReceived = 0; // The total bytes received, used to calculate the speed of the connection, that is the number of bytes received per second.
//         //         int totalBytesTransferred = 0; // The total bytes transferred/received, used to calculate the progress of the receive.
//         //         int currentPacketOffset = 0; // The current offset in the stream.
//         //         int totalPacketsCompleted = 0; // The total packets received, used to calculate the speed of the connection, that is the number of packets received per second.
//         //         int nextPacketDataOffset = 0;

//         //         DateTime startTime = DateTime.Now; // Get the current time.

//         //         while (!_sourceToken.IsCancellationRequested && !userToken.SourceToken.IsCancellationRequested)
//         //         {
//         //             // Stop 'while' processing if the cancellation token is requested, to prevent high CPU usage.
//         //             if (ReadExactly(dataStream, userToken, dataMemory, ref currentPacketOffset, 4))
//         //             {
//         //                 // Read exactly 4 bytes(integer) from the stream, if the read is completed, process the received data.
//         //                 // 4 bytes is the length of the message, the length of the message is the first 4 bytes(prefix) of the message/packet.
//         //                 // 4 bytes = 32 bits = integer(int), the maximum length of the message is 2^32 - 1 bytes.

//         //                 int messageLength = BitConverter.ToInt32(dataMemory.Span[..4]); // Slice the first 4 bytes(prefix) to get the length of the message and convert it to integer.
//         //                 messageLength += 4; // Add 4 bytes(prefix) to the length of the message, this is the real length of the packet, including the prefix....

//         //                 int fLength = messageLength - 4;
//         //                 if (fLength > _maxMessageSize || fLength <= 0)
//         //                 {
//         //                     // If the length of the message is greater than 512 bytes, ignore the message, if size is less or equal to 0 bytes, ignore the message, because the message is empty.
//         //                     // Dos attack, disconnect the client.
//         //                     if (!LogHelper.Error($"Invalid message length! -> {fLength}"))
//         //                     {
//         //                         userToken.Socket.Close();
//         //                         return;
//         //                     }
//         //                 }

//         //                 while ((currentPacketOffset < messageLength) && (!_sourceToken.IsCancellationRequested && !userToken.SourceToken.IsCancellationRequested))
//         //                 {
//         //                     // Stop 'while' processing if the cancellation token is requested, to prevent high CPU usage.
//         //                     if (ReadExactly(dataStream, userToken, dataMemory, ref currentPacketOffset, messageLength))
//         //                     {
//         //                         // Read exactly the length of the message from the stream, if the read is completed, process the received data.
//         //                         Memory<byte> messageData = dataMemory[4..messageLength]; // Slice the data from the prefix to the length of the message to get the message/packet.
//         //                         int bytesRemaining = totalBytesTransferred - currentPacketOffset; // Get the remaining bytes to be read.

//         //                         if (messageData.Length != fLength)
//         //                             throw new NeutronException("Header: Invalid range!");

//         //                         // When packet is completed....
//         //                         if (_socketMode == SocketMode.Server)
//         //                         {
//         //                             LogHelper.Error($"Received packet > {messageData.Length} bytes");

//         //                             SocketAsyncEventArgs sendArgs = userToken.PooledSocketAsyncEventArgsForSend.Pull(); // Get the socket event args.
//         //                             sendArgs.Completed += OnIOCompleted; // Set the completed event.
//         //                             sendArgs.UserToken = userToken; // Set the user token.
//         //                             sendArgs.AcceptSocket = userToken.Socket._socket;
//         //                             sendArgs.SetBuffer(messageData);

//         //                             //Interlocked.Exchange(ref userToken.isReceiving, 1); // Stop the receive process.

//         //                             StartSend(sendArgs); // Start sending the data.
//         //                         }

//         //                         totalPacketsCompleted++; // Increase the total packets received.
//         //                         totalPackets++;

//         //                         var endTime = DateTime.Now - startTime; // Get the current time.
//         //                         if (endTime.Seconds >= 1)
//         //                         {
//         //                             // If the time is greater than or equal to 1 second, calculate the speed of the connection.

//         //                             long bytesTransferredPerSecond = ((long)totalBytesReceived / endTime.Seconds); // Calculate the bytes transferred per second.
//         //                             int packetsReceivedPerSecond = totalPacketsCompleted / endTime.Seconds; // Calculate the packets received per second.

//         //                             userToken.BytesTransferredPerSecond = bytesTransferredPerSecond; // Set the bytes transferred per second.
//         //                             userToken.PacketsTransferredPerSecond = packetsReceivedPerSecond; // Set the packets received per second.

//         //                             // if (packetsReceivedPerSecond != 0)
//         //                             //     LogHelper.Error($"Packets received per second: {packetsReceivedPerSecond}");
//         //                             // if (bytesTransferredPerSecond != 0)
//         //                             //     LogHelper.Error($"Bytes transferred per second: {bytesTransferredPerSecond}");

//         //                             totalBytesReceived = 0; // Reset the total bytes received.
//         //                             totalPacketsCompleted = 0; // Reset the total packets received.
//         //                             startTime = DateTime.Now; // Set the current time.
//         //                         }

//         //                         if (bytesRemaining > 0)
//         //                         {
//         //                             if (bytesRemaining > nextPacketData.Length)
//         //                                 throw new NeutronException($"nextData: A buffer stack overflow has occurred! -> length: {bytesRemaining}:{nextPacketData.Length}");

//         //                             if (ReadExactly(dataStream, userToken, nextPacketData, ref nextPacketDataOffset, bytesRemaining)) // Read the remaining bytes from the stream.
//         //                                 nextPacketDataOffset = 0;
//         //                         }

//         //                         dataStream.GetBuffer().AsSpan().Clear(); // Clear the buffer.
//         //                         dataStream.Position = 0; // Reset the position.

//         //                         if (bytesRemaining > 0)
//         //                         {
//         //                             // copy the remaining data to the beginning of the stream.
//         //                             var data = nextPacketData[..bytesRemaining];
//         //                             dataStream.Write(data.Span); // If there are remaining bytes to be read, write them to the stream.
//         //                             dataStream.Position = 0; // Set the position of the stream to the beginning.
//         //                         }

//         //                         currentPacketOffset = 0; // Reset the current offset.
//         //                         totalBytesTransferred = bytesRemaining; // Reset the total bytes transferred.

//         //                         break; // Break the 'while' loop.
//         //                     }
//         //                     else
//         //                         GetRemainingData(dataStream, userToken, ref currentPacketOffset, ref totalBytesTransferred, ref totalBytesReceived, ref totalBytes); // Get the left data from the socket and add it to the stream.
//         //                 }
//         //             }
//         //             else
//         //                 GetRemainingData(dataStream, userToken, ref currentPacketOffset, ref totalBytesTransferred, ref totalBytesReceived, ref totalBytes); // Get the left data from the socket and add it to the stream.
//         //         }
//         //     }
//         //     catch (OperationCanceledException) { }
//         //     catch (Exception ex)
//         //     {
//         //         LogHelper.Stacktrace(ex);
//         //     }
//         // }

//         /// <summary>
//         /// Part 2: Message framing: Get the remaining data from the socket and add it to the stream.
//         /// </summary>
//         // private void GetRemainingData(Stream stream, UserToken userToken, ref int offset, ref int totalBytesTransferred, ref int totalBytesReceived, ref int totalBytes)
//         // {
//         //     var args = userToken.AsyncEventArgsBlockingQueue.Take(userToken.SourceToken.Token); // Get the socket event args from the queue and block until the data is received.
//         //     int bytesTransferred = args.BytesTransferred; // Get the bytes transferred.

//         //     if (bytesTransferred > 0)
//         //     {
//         //         // if the bytes transferred is greater than 0, add the data to the stream.

//         //         ReadOnlyMemory<byte> data = args.MemoryBuffer[..bytesTransferred]; // Slice the data to get the data.
//         //         stream.Write(data.Span[..bytesTransferred]); // Write the data to the stream.
//         //         stream.Position = offset; // Set the position of the stream to the current offset.

//         //         totalBytesTransferred += bytesTransferred; // Add the bytes transferred to the total bytes transferred.
//         //         totalBytesReceived += bytesTransferred; // Add the bytes transferred to the total bytes received.
//         //         totalBytes += bytesTransferred; // Add the bytes transferred to the total bytes.
//         //     }

//         //     int isReceiving = Interlocked.CompareExchange(ref userToken.isReceiving, 0, 0);
//         //     if (isReceiving == 0)
//         //     {
//         //         if (userToken.AsyncEventArgsBlockingQueue.Count <= 0)
//         //         {
//         //             SocketAsyncEventArgs receiveArgs = userToken.PooledSocketAsyncEventArgsForReceive.Pull(); // Get the socket event args.
//         //             receiveArgs.Completed += OnIOCompleted; // Set the completed event.
//         //             receiveArgs.UserToken = userToken; // Set the user token.
//         //             receiveArgs.AcceptSocket = userToken.Socket._socket; // Set the accept socket.

//         //             //ReceiveAsync(receiveArgs);
//         //         }
//         //     }

//         //     args.Completed -= OnIOCompleted;
//         //     args.UserToken = null;
//         //     args.AcceptSocket = null;
//         //     userToken.PooledSocketAsyncEventArgsForReceive.Push(args);
//         // }

//         /// <summary>
//         /// Part 3: Message Framing: Read exactly X bytes from the stream.
//         /// </summary>
//         // private bool ReadExactly(Stream stream, UserToken userToken, Memory<byte> buffer, ref int offset, int size)
//         // {
//         //     while ((offset < size) && (!_sourceToken.IsCancellationRequested && !userToken.SourceToken.IsCancellationRequested))
//         //     {
//         //         // Stop 'while' processing if the cancellation token is requested, to prevent high CPU usage.

//         //         int bytesLeftToRead = size - offset; // Get the bytes left to read.
//         //         int bytesRead = stream.Read(buffer.Span[offset..(offset + bytesLeftToRead)]); // Read the data from the stream.
//         //         if (bytesRead > 0)
//         //             offset += bytesRead; // Add the bytes read to the offset.
//         //         else
//         //             return false; // If the bytes read is 0, the read is not completed.
//         //     }

//         //     return offset == size; // Return true if the read is completed.
//         // }

//         public int Send(byte[] buffer)
//         {
//             return _socket.Send(buffer); // Send the data to the socket.
//         }

//         public int Send(ReadOnlySpan<byte> buffer)
//         {
//             return _socket.Send(buffer); // Send the data to the socket.
//         }

//         public void SendAsync(byte[] buffer)
//         {
//             if (_userToken == null)
//                 throw new NeutronException("Call Start() first!");

//             if (_socketMode != SocketMode.Client)
//                 throw new NeutronException("This option is only available for client sockets!");

//             int value = Interlocked.CompareExchange(ref _userToken.IsSending, 0, 0);
//             if (value == 0)
//             {
//                 Interlocked.Exchange(ref _userToken.IsSending, 1);
//                 SendAsync(_userToken.PooledSocketAsyncEventArgsForSend.Pull(), buffer);
//             }
//             else
//                 _userToken.DataToSendQueue.Push(buffer);
//         }

//         /// <summary>
//         /// This event is trigerred when the socket finishes a operation.
//         /// </summary>
//         private void OnIOCompleted(object sender, SocketAsyncEventArgs socketAsyncEventArgs)
//         {
//             switch (socketAsyncEventArgs.LastOperation)
//             {
//                 case SocketAsyncOperation.Accept:
//                     ProcessAcceptedSocket(socketAsyncEventArgs); // Process the accepted socket.
//                     break;
//                 case SocketAsyncOperation.Receive:
//                     ProcessReceivedData(socketAsyncEventArgs, (UserToken)socketAsyncEventArgs.UserToken, _protocol, _socketMode); // Process the received data.
//                     break;
//                 case SocketAsyncOperation.Connect:
//                     ProcessConnectedAsync(socketAsyncEventArgs); // Process the connected socket.
//                     break;
//                 case SocketAsyncOperation.Send:
//                     ProcessSendedData(socketAsyncEventArgs); // Process the sent data.
//                     break;
//                 case SocketAsyncOperation.Disconnect:
//                     break;
//             }
//         }

//         /// <summary>
//         /// Free the unmanged/managed resources to prevent memory leaks.
//         /// </summary>
//         public void Close()
//         {
//             try
//             {
//                 if (_socket == null)
//                     return; // If the socket is null, return.

//                 _sourceToken.Cancel(); // Cancel the source token, this stop all the async operations.
//                 if (_userToken != null)
//                 {
//                     _userToken.AutoResetEvent.Set();
//                     _userToken.SourceToken.Cancel(); // Stop the user token, this stop all the async operations for the user.
//                 }

//                 if (_socketMode == SocketMode.Server)
//                 {
//                     // If the socket is listening, is it a server socket? in this case, the server socket must be closed, but before that, the client sockets must be closed.
//                     foreach (NeutronSocket socket in _sockets.ToArray())
//                     {
//                         if (_sockets.Remove(socket))
//                         {
//                             // If the socket is removed from the list, close the socket.
//                             if (socket.IsConnected)
//                                 socket.Close(); // Close the socket. (:
//                             else { /*continue*/ }
//                         }
//                         else
//                             throw new NeutronException("Receive: Socket not found!"); // If the socket is not found, throw an exception.
//                     }
//                 }

//                 if (_socket.Connected)
//                     _socket.Shutdown(SocketShutdown.Both); // Shutdown the socket, wait for the all the data to be sent and received.
//             }
//             finally
//             {
//                 // free unmanaged resources...
//                 _socket?.Close(); // Close the socket.
//                 _sourceToken?.Dispose(); // Dispose the cancellation token, free the resources.
//                 // free managed resources...
//                 if (_userToken != null)
//                 {
//                     _userToken.AutoResetEvent.Dispose();
//                     _userToken.AsyncEventArgsBlockingQueue?.Dispose(); // Dispose the blocking queue.
//                     _userToken.PooledSocketAsyncEventArgsForReceive = null; // Dispose the pooled socket event args for receive.
//                     _userToken.PooledSocketAsyncEventArgsForSend = null; // Dispose the pooled socket event args for send.
//                     _userToken.SourceToken?.Dispose(); // Dispose the cancellation token, free the resources.
//                     _userToken = null; // Dispose the user token.
//                 }
//                 // free managed resources...
//                 if (_bufferManager != null)
//                     _bufferManager = null; // Dispose the buffer.
//             }
//         }

//         private void CheckAcceptIsValidForThisSocket()
//         {
//             if ((_socketMode != SocketMode.Server && _protocol != Protocol.Tcp) || _socket == null)
//                 throw new Exception("This property is not valid for this socket."); // If the socket is not a server socket and the protocol is not TCP, throw an exception.
//         }

//         private void CheckConnectIsValidForThisSocket(EndPoint endPoint)
//         {
//             if (endPoint == null)
//                 throw new Exception("Endpoint is null!");

//             if (((IPEndPoint)endPoint).Address == IPAddress.Any)
//                 throw new Exception("Invalid IP Address!");

//             if ((_socketMode == SocketMode.Server) || _socket == null)
//                 throw new Exception("This operation is not valid for this socket."); // If the socket is a server socket, throw an exception.
//         }
//     }
// }