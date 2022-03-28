using NeutronNetwork.Constants;
using NeutronNetwork.Helpers;
using NeutronNetwork.Internal.Components;
using NeutronNetwork.Internal.Wrappers;
using NeutronNetwork.Wrappers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using ThreadPriority = System.Threading.ThreadPriority;

// Created by: Ruan Cardoso(Brasil)
// Email: neutron050322@gmail.com
// License: MIT
// Mano???? Isso aqui deu trabalho demais sfd (:
namespace NeutronNetwork.Internal
{
    public enum ChannelMode : byte { Unreliable = 0, Reliable = 1, ReliableSequenced = 2 }
    public enum OperationMode : byte { Sequence = 0, Data = 1, Acknowledgement = 2 }
    public enum TargetMode : byte { All, Others, Single, Server }
    public enum PacketType : byte { Connect = 1, Test = 2 } // Zero is reserved for the packet type(Default)..
    public class TransmissionPacket
    {
        /// <summary>
        /// The sequence(Ack) number, this number is used to re-transmit the packet if the packet is lost.
        /// When the response is received, the sequence number is compared to the sequence number of the response.
        /// If they are the same, the packet is considered to be received and will be removed from the transmission queue, otherwise it will be re-transmitted.
        /// </summary>
        public int SeqAck { get; } // Available for these modes: Reliable, ReliableSequenced
        /// <summary>
        /// Packet attempts to re-transmit, if packets are lost, this number will be increased.
        /// If the number is greater than the maximum number of attempts, the packet will be discarded, because the EndPoint is not responding, disconnection will be detected?
        /// Thread Safe: Because this value is incremented on both the main thread and the sending thread.
        /// </summary>
        public int Attempts; /*{ get; set; }*/ // Available for these modes: Reliable, ReliableSequenced
        /// <summary>
        /// This date is used to calculate the time to wait before re-transmitting the packet.
        /// If the packet is not received, the time will be increased by the time between each attempt.
        /// If the time is greater or equal to the maximum time, the packet will be re-transmitted and the time will be reset to UtcNow.
        /// </summary>
        public DateTime LastSent { get; set; } // Available for these modes: Reliable, ReliableSequenced
        /// <summary>
        /// The data that will be re-transmitted.
        /// This data keeps the same sequence number, so the packet will be re-transmitted.
        /// </summary>
        public UdpPacket Data { get; } // Available for these modes: Reliable, ReliableSequenced

        public TransmissionPacket(int sequence, DateTime lastSent, UdpPacket data)
        {
            SeqAck = sequence;
            LastSent = lastSent;
            Data = data;
        }

        /// <summary>
        /// Is attempt is greater than the maximum number of attempts, the packet will be discarded.
        /// As we are working with reliable packets, this should never happen.
        /// If it happens, it means that the EndPoint is not responding, a possible disconnection will be detected? or that client is really, really, really laggy?
        /// With these assumptions, i think it's better to disconnect the client(KICK) and free the resources.
        /// </summary>
        public bool IsDisconnected()
        {
            // Thread safe: Simultaneous access to this value is not a problem.
            int value = Interlocked.Increment(ref Attempts);
            if (value >= 150)
                LogHelper.Info($"Packet with sequence: {SeqAck} was lost and was not re-transmitted ):");
            return value >= 150;
        }
    }

    public class UdpPacket
    {
        /// <summary>
        /// The id used to identify the owner of the packet.
        /// If the id is 0, the packet is a default packet, and it will be handled by the server.
        /// Maximum id is ushort.MaxValue.
        /// </summary>
        public ushort PlayerId { get; } // Available for these modes: Reliable, ReliableSequenced, Unreliable
        /// <summary>
        /// The sequence(Ack) number, this number is used to re-transmit the packet if the packet is lost.
        /// When the response is received, the sequence number is compared to the sequence number of the response.
        /// If they are the same, the packet is considered to be received and will be removed from the transmission queue, otherwise it will be re-transmitted.
        /// </summary>
        public int SeqAck { get; } // Available for these modes: Reliable, ReliableSequenced
        /// <summary>
        /// Packet attempts to re-transmit, if packets are lost, this number will be increased.
        /// If the number is greater than the maximum number of attempts, the packet will be discarded, because the EndPoint is not responding, disconnection will be detected?
        /// Thread Safe: Because this value is incremented on both the main thread and the sending thread.
        /// </summary>
        public int Attempts = 0; /*{ get; set; }*/ // Available for these modes: Reliable, ReliableSequenced
        /// <summary>
        /// This date is used to calculate the time to wait before re-transmitting the packet.
        /// If the packet is not received, the time will be increased by the time between each attempt.
        /// If the time is greater or equal to the maximum time, the packet will be re-transmitted and the time will be reset to UtcNow.
        /// </summary>
        public DateTime LastSent { get; set; } // Available for these modes: Reliable, ReliableSequenced
        /// <summary>
        /// This EndPoint is the EndPoint of the owner of this packet.
        /// This is commonly used to ignore the packet to the owner.
        /// </summary>
        public EndPoint EndPoint { get; } // Available for these modes: Unreliable, Reliable, ReliableSequenced
        /// <summary>
        /// Set the packet is a sequence packet, data packet or an acknowledgement packet.
        /// sequence packet: The packet is a sequence packet, it is used to re-transmit the packet if the packet is lost.
        /// data packet: The packet is a data packet, it is used to send data to the EndPoint.
        /// acknowledgement packet: The packet is an acknowledgement packet, it is used to re-transmit the packet if the packet is lost.
        /// </summary>
        public OperationMode OperationMode { get; } // Available for these modes: Reliable, ReliableSequenced
        /// <summary>
        /// Define how be able to receive the packet.
        /// All: The packet will be received by all clients.
        /// Others: The packet will be received by all clients except the owner.
        /// Single: The packet will be received by the owner.
        /// Server: The packet will be received by the server.
        /// </summary>
        public TargetMode TargetMode { get; } // Available for these modes: Unreliable, Reliable, ReliableSequenced
        /// <summary>
        /// The channel used to send the packet.
        /// Reliable: The packet will be sent on the reliable channel.
        /// Unreliable: The packet will be sent on the unreliable channel.
        /// ReliableSequenced: The packet will be sent on the reliable sequenced channel.
        /// </summary>
        public ChannelMode ChannelMode { get; } // Available for these modes: Reliable, ReliableSequenced
        /// <summary>
        /// The channel that the packet will be sent on.
        /// Reliable or ReliableSequenced: The packet will be sent on the channel that is defined by the channel number.
        /// Each player has a channel number that is defined by the player's ID.
        /// </summary>
        public ChannelData ChannelData { get; }  // Available for these modes: Reliable, ReliableSequenced
        /// <summary>
        /// The data that will be sent.
        /// </summary>
        public byte[] Data { get; } // Available for these modes: Unreliable, Reliable, ReliableSequenced

        public UdpPacket(int seqAck, ushort playerId, DateTime lastSent, EndPoint endPoint, OperationMode operationMode, TargetMode targetMode, ChannelMode channelMode, ChannelData channelData, byte[] data)
        {
            // Reliable and ReliableSequenced constructor.
            PlayerId = playerId;
            SeqAck = seqAck;
            LastSent = lastSent;
            EndPoint = endPoint;
            OperationMode = operationMode;
            TargetMode = targetMode;
            ChannelMode = channelMode;
            ChannelData = channelData;
            Data = data;
        }

        public UdpPacket(ushort playerId, EndPoint endPoint, OperationMode operationMode, TargetMode targetMode, ChannelMode channelMode, byte[] data)
        {
            // Unreliable constructor.
            PlayerId = playerId;
            EndPoint = endPoint;
            OperationMode = operationMode;
            TargetMode = targetMode;
            ChannelMode = channelMode;
            Data = data;
        }

        /// <summary>
        /// Is attempt is greater than the maximum number of attempts, the packet will be discarded.
        /// As we are working with reliable packets, this should never happen.
        /// If it happens, it means that the EndPoint is not responding, a possible disconnection will be detected? or that client is really, really, really laggy?
        /// With these assumptions, i think it's better to disconnect the client(KICK) and free the resources.
        /// </summary>
        public bool IsDisconnected()
        {
            // Thread safe: Simultaneous access to this value is not a problem.
            int value = Interlocked.Increment(ref Attempts);
            if (value >= 150)
                LogHelper.Info($"Packet with sequence: {SeqAck} was lost and was not re-transmitted ):");
            return value >= 150;
        }
    }

    public class ChannelData
    {
        static int Syn { get; } = 0;// Available for these modes: Reliable, ReliableSequenced
        /// <summary>
        /// The sequence(Ack) number, this number is used to identify the packet, is incremented every time a packet is sent.
        /// Used to make UDP packets reliable and ordered.
        /// </summary>
        public int SentAck = Syn; // Available for these modes: Reliable, ReliableSequenced
        /// <summary>
        /// This value will be used to compare if the first sequence number in the array is the same as the last one + 1.
        /// Formula is: Array[0] == (LastReceivedSequentialAck + 1).
        /// Imagine that: The first packet received is sequence 10, because the packets is out of order, LastReceivedSequentialAck in this case is zero.
        /// Array[0] = 10, so the first packet received is sequence 10.
        /// LastReceivedSequentialAck = 0.
        /// (LastReceivedSequentialAck + 1) = 1.
        /// Now let's calculate: Array[0] == (LastReceivedSequentialAck + 1) | -> 10 equals to (0 + 1), if true, it's ok, if false, the packet is out of order.
        /// If it's Ok, the last LastReceivedSequentialAck is setted to the last sequence number(10).
        /// Obs: The calculation is valid only if the length of the string array is 1....
        /// </summary>
        public int LastReceivedSequentialAck { get; set; } = Syn; // Available for these modes: ReliableSequenced
        /// <summary>
        /// This value returns the last sequence number processed.
        /// Let's remove from the list the packets that are already processed, that is, the packets that are less or equal to the last sequence number processed.
        /// This is only used to clean the list of packets that are already processed and free the memory.
        /// </summary>
        public int LastProcessedSequentialAck { get; set; } = Syn; // Available for these modes: ReliableSequenced
        /// <summary>
        /// List of the packets that are waiting to be re-sent.
        /// This list is used to re-send the packets() that are lost.
        /// Every time a reliable packet is sent, it is added to this list.
        /// When the packet is received, it is removed from this list.
        /// </summary>
        public NeutronSafeDictionary<int, TransmissionPacket> PacketsToReTransmit = new NeutronSafeDictionary<int, TransmissionPacket>(); // Available for these modes: Reliable, ReliableSequenced
        /// <summary>
        /// Any sequence is received is added to this list.
        /// It's only used to check if the sequence is already received.
        /// Duplicate packets are rarely received, but if they are, they will be ignored.
        /// Can RAM usage be an issue? No and Yes, but it's not a big problem.
        /// If the client is playing for weeks or months without ever disconnecting, so we have a big problem.
        /// </summary>
        public Dictionary<int, int> ReceivedSequences = new Dictionary<int, int>(); // Available for these modes: Reliable
        /// <summary>
        /// All packets are added to this list is automatically sorted by sequence number.
        /// The data from the list is only processed when the all data are in sequence.
        /// 1, 2, 3, 4, 5 It's Ok, the data is processed.
        /// 1, 3, 4, 5, 6 Not Ok, the data is not processed, because it's not in sequence, 2 is missing.
        /// If a sequence is missing, let's wait for it to arrive.
        /// </summary>
        public SortedDictionary<int, byte[]> SequentialData = new SortedDictionary<int, byte[]>(); // Available for these modes: ReliableSequenced
        /// <summary>
        /// This method is used to check if the all data is in sequence.
        /// Returns true if the all data is in sequence, otherwise returns false.
        /// Every time a packet is received, this method is called to check if the data is in sequence.
        /// </summary>
        public bool IsSequential()
        {
            // The next sequence number is the last sequence number + 1.
            // If the array length is 1, formula is: Array[0] == (LastReceivedSequentialAck + 1) == Ok, the first received packet is in sequence.
            int nextSequence = LastReceivedSequentialAck + 1;
            // Convert the dictionary keys to an array.
            // The dictionary keys are the sequence numbers.
            // Let's compare if it is in sequence using the following formula: array[index + 1] == array[index] + 1.
            int[] keys = SequentialData.Keys.ToArray();
            // If the length is zero, the data is not in sequence.
            // If the length is one, check: Array[0] == (LastReceivedSequentialAck + 1) == Ok, the first received packet is in sequence.
            // If the length is more than one, check: array[index + 1] == array[index] + 1.
            if (keys.Length == 0)
                return false;
            else if (keys.Length == 1)
                return keys[0] == nextSequence;
            else if (keys.Length > 1)
            {
                for (int i = 0; i < keys.Length; i++)
                {
                    // If the index is the last index, check: array[^1] == array[^2] + 1 == Ok, the last received packet is in sequence.
                    if (i + 1 == keys.Length)
                        return keys[^1] == keys[^2] + 1;
                    else
                    {
                        // If the index is not the last index, check: array[index + 1] == array[index] + 1 == Ok, the received packets are in sequence.
                        // If the last index, an exception will be thrown, because the last index + 1 is out of range.
                        if (keys[i + 1] != keys[i] + 1)
                            return false;
                        else
                            continue;
                    }
                }
            }
            return true;
        }
    }

    public class BandwidthCounter
    {
        /// <summary>
        /// Lets create this counter to know how many bytes we have received per second.
        /// Note that: values are not exact, but it's a good approximation.
        /// Note: The protocol overhead is optional, but it's a good idea to use it.
        /// Every X seconds, the calc is done and the value is reset, to mantaing a good approximation.
        /// </summary>
        private Stopwatch Stopwatch { get; } = new Stopwatch();
        /// <summary>
        /// Increment this counter every time we receive a packet.
        /// The formula is: (total packets received)/(time elapsed in seconds).
        /// </summary>
        private long TotalPackets;
        /// <summary>
        /// Calc the last time we received a packet.
        /// If is equal or greater tha 1 second, the value is printed.
        /// </summary>
        private double LastSec;
        /// <summary>
        /// Increment this counter + the total bytes received every time we receive a packet.
        /// The formula is: (total bytes received)/(time elapsed in seconds).
        /// </summary>
        private long TotalBytes;

        public void Start()
        {
            // Before we receive the data, let's start the stopwatch.
            // This is used to calculate the bytes received per second.
            if (!Stopwatch.IsRunning)
                Stopwatch.Start();
        }

        public void Stop()
        {
            // After we receive the data, stop the stopwatch.
            // This is used to calculate the bytes received per second.
            if (Stopwatch.IsRunning)
                Stopwatch.Stop();
        }

        public void Add(int bytesTransferred)
        {
            TotalPackets++;
            TotalBytes += bytesTransferred;
        }

        public void Get()
        {
            double sec = Stopwatch.Elapsed.TotalSeconds;
            if (sec > 0)
            {
                // Let's calculate the bytes received per second and packets received per second.
                double bytesTransferRate = TotalBytes / sec;
                double packetsTransferRate = Math.Round(TotalPackets / sec);
                // If one second has passed, let's print the values.
                if (sec >= LastSec + 1)
                {
                    NeutronStatistics.ServerUDP.SetIncoming((int)bytesTransferRate, (int)packetsTransferRate);
                    NeutronStatistics.OnChangedStatistics?.Invoke(NeutronStatistics._inOutDatas);
                    // Set the last second to the current second.
                    LastSec = sec;
                }
                // If 10 seconds has passed, let's reset the counters, to keep the good approximation.
                if (sec >= 10)
                {
                    LastSec = TotalBytes = TotalPackets = 0;
                    Stopwatch.Reset();
                }
            }
        }
    }

    public class SocketClient
    {
        public SocketClient(ushort id, EndPoint endPoint, long address, int port)
        {
            Id = id;
            EndPoint = endPoint;
            Address = address;
            Port = port;
        }

        public ushort Id { get; set; }
        public EndPoint EndPoint { get; set; }
        public long Address { get; set; }
        public int Port { get; set; }
    }

    public class NeutronUdp
    {
        private ushort _id = 0;
        /// <summary>
        /// The Connected property gets the connection state of the Socket as of the last I/O operation. 
        /// When it returns false, the Socket was either never connected, or is no longer connected
        /// </summary>
        private bool _isConnected;
        /// <summary>
        /// The socket used to receive and send the data, all data are received and sent simultaneously.
        /// Synchronous receive and send operations are used to avoid the overhead of asynchronous operations, 
        /// Unity doesn't like asynchronous operations, high CPU usage and a lot of garbage collection and low number of packets per second.
        /// And it doesn't matter if it's on a different thread, and because of that TCP is not welcome here...
        /// Only UDP is welcome here, so this socket implements three channels: unreliable, reliable and reliable ordered.
        /// Udp is connectionless, so we don't need async receive and send operations and we don't need extra threads either and we lighten the load of the garbage collector.
        /// This makes it a lot faster than TCP and perfect for Unity.
        /// </summary>
        private Socket _socket;
        /// <summary>
        /// The endpoint used to send data to the remote host, client only.
        /// I made a wrapper for this because a lot of garbage will be created if we use the IPEndPoint directly.
        /// </summary>
        private EndPoint _destEndPoint;
        /// <summary>
        /// The endpoint used to listen for data from the remote host.
        /// </summary>
        private EndPoint _srcEndPoint;
        /// <summary>
        /// Used to cancel the receive and send operations, called when the socket is closed.
        /// Prevents the CPU from spinning, Thread.Abort() is not recommended, because it's not a good way to stop a thread and not work on Linux OS.
        /// The Dispose() method is called when the socket is closed, and the Dispose() method is called when the application is closed.
        /// </summary>
        private CancellationTokenSource _cts;
        /// <summary>
        /// Used to enqueue the received data, the data is enqueued in a queue, and the queue is processed in a thread.
        /// The Dispose() method is called when the socket is closed, and the Dispose() method is called when the application is closed.
        /// </summary>
        private NeutronBlockingQueue<UdpPacket> _dataToSend;
        /// <summary>
        /// This event is fired when the completed message is received.
        /// </summary>
        internal event NeutronEventNoReturn<NeutronStream, ushort, EndPoint, ChannelMode, TargetMode, OperationMode, NeutronUdp> OnMessageCompleted;
        /// <summary>
        /// The list to store the connected clients.
        /// When a client connects, it's added to this list.
        /// When a client disconnects, it's removed from this list.
        /// Thread Safe: This list is used by the thread that is processing the received data, and the thread that is sending data.
        /// </summary>
        internal NeutronSafeDictionary<EndPoint, SocketClient> ClientsByEndPoint = new NeutronSafeDictionary<EndPoint, SocketClient>();
        /// <summary>
        /// The list to store the connected clients.
        /// When a client connects, it's added to this list.
        /// When a client disconnects, it's removed from this list.
        /// Thread Safe: This list is used by the thread that is processing the received data, and the thread that is sending data.
        /// </summary>
        //internal NeutronSafeDictionary<ushort, SocketClient> ClientsById = new NeutronSafeDictionary<ushort, SocketClient>();
        /// <summary>
        /// Store the information of the channels.
        /// Ex: SentSequence, RecvSequence, Acknowledge....etc
        /// UDP sequence(seq) and acknowledgment(ack) numbers are used to detect lost packets and to detect packet reordering.
        /// The sequence number is incremented every time a packet is sent, and the acknowledgment number is incremented every time a packet is received.
        /// The acknowledgment number is used to confirm that the packet has been received, if not, the packet is resent.
        /// The sequence number is used to reorder packets, if the packet is out of order, the packet is reordered.
        /// Disponible only for reliable channels.
        /// ValueTuple(Address, Port, ChannelMode)
        /// </summary>
        internal NeutronSafeDictionary<(ushort, byte), ChannelData> ChannelsData = new NeutronSafeDictionary<(ushort, byte), ChannelData>() { };
        /// <summary>
        /// List of exlusive id's, used to prevent the same id to be used twice.
        /// When a player connects, a unique id is removed from this list.
        /// When a player disconnects, the id is added back to this list.
        /// </summary>
        private NeutronSafeQueueNonAlloc<ushort> _ids = new NeutronSafeQueueNonAlloc<ushort>();

        /// <summary>
        /// Associates a Socket with a local endpoint.
        /// </summary>
        public void Bind(EndPoint endPoint)
        {
            _cts = new();
            _dataToSend = new();
            _socket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
            {
                // The ReceiveBufferSize property gets or sets the number of bytes that you are expecting to store in the receive buffer for each read operation. 
                // This property actually manipulates the network buffer space allocated for receiving incoming data.
                ReceiveBufferSize = 8192,
                // The SendBufferSize property gets or sets the number of bytes that you are expecting to store in the send buffer for each send operation.
                // This property actually manipulates the network buffer space allocated for sending outgoing data.
                SendBufferSize = 8192,
            };
            // Bind the socket to the endpoint.
            // This address and port will be used to receive data from the remote host.
            _socket.Bind(endPoint);
            // The LocalEndPoint property gets the local endpoint of the socket.
            _srcEndPoint = endPoint;
            // Add the availables id's to the list.
            // This list is used to prevent the same id to be used twice.
            for (ushort i = 1; i < ushort.MaxValue; i++)
                _ids.Push(i);
        }

        /// <summary>
        /// Initialize the send and receive channels.
        /// </summary>
        public void Init()
        {
            // Start the receive thread.
            // The Unity API doesn't allow to be called from a thread other than the main thread.
            // The Unity API wil be dispatched to the main thread.
            // Why don't receive the data in the main thread?
            // Because the ReceiveFrom() method is blocking, FPS will be affected.
            // The Unity will be frozen until the data is received, but's not a good idead, right?
            StartReceiveThread();
            // Start the send thread.
            // This thread is used to send data to the remote host.
            // Why don't we send the data directly from the receive thread or Unity's main thread?
            // Because the send method is blocking, and we don't want to block Unity's main thread, FPS will be affected.
            // Let's the data to a queue, and the queue is processed in a thread.
            StartSendThread();
        }

        /// <summary>
        /// Connect to the remote host.
        /// </summary>
        public IEnumerator Connect(EndPoint endPoint)
        {
            while (true)
            {
                if (_isConnected)
                    break; // if the socket is connected, break the loop.
                // The endpoint used to send data to the remote host, client only.
                _destEndPoint = endPoint;
                using (NeutronStream packet = Neutron.PooledNetworkStreams.Pull())
                {
                    var writer = packet.Writer;
                    writer.WritePacket((byte)PacketType.Connect);
                    // The first packet is used to establish the connection.
                    // We are using unrealible channel, because we don't have and exclusive Id for the connection.
                    // We need an id to identify the connection, and the id is the "symbolic link" for the EndPoint...
                    // As we are using an unrealible channel, we need to send connection packets until we get a response.
                    SendToServer(packet, ChannelMode.Unreliable, TargetMode.Single);
                }
                // Wait for the response.
                yield return new WaitForSeconds(0.3f);
                if (_isConnected)
                    break; // if the socket is connected, break the loop.
            }
        }

        /// <summary>
        /// Process the internal packet queue.
        /// </summary>
        public PacketType OnServerMessageCompleted(NeutronStream stream, ushort playerId, EndPoint endPoint, ChannelMode channelMode, TargetMode targetMode, OperationMode opMode, NeutronUdp udp)
        {
            var reader = stream.Reader;
            var writer = stream.Writer;
            // Convert byte to packet type.
            var packetType = (PacketType)reader.ReadByte();
            switch (packetType)
            {
                // Let's process the packet.
                case PacketType.Connect:
                    {
                        if (playerId == 0)
                        {
                            if (GetAvailableID(out ushort id))
                            {
                                AddChannel(id);
                                writer.WritePacket((byte)PacketType.Connect);
                                writer.Write(id);
                                // Create the local channels to send and receive data.
                                // IPEndPoint iPEndPoint = (IPEndPoint)endPoint;               
                                udp.SendToClient(stream, channelMode, targetMode, opMode, playerId, endPoint);
                            }
                            else
                                LogHelper.Error("[Neutron] -> No available id's.");
                        }
                        else
                            LogHelper.Error($"[Neutron] -> Player {playerId} Already connected.");
                    }
                    break;
                default:
                    return packetType; // If not an internal packet, return the packet type to process it.
            }
            // If is a internal packet, return null to ignore it.
            return default;
        }

        /// <summary>
        /// Process the internal packet queue.
        /// </summary>
        public PacketType OnClientMessageCompleted(NeutronStream stream, ushort playerId, EndPoint endPoint, ChannelMode channelMode, TargetMode targetMode, OperationMode opMode, NeutronUdp udp)
        {
            var reader = stream.Reader;
            var writer = stream.Writer;
            // Convert byte to packet type.
            var packetType = (PacketType)reader.ReadByte();
            switch (packetType)
            {
                case PacketType.Connect:
                    AddChannel(_id = reader.ReadUShort());
                    _isConnected = true;
                    break;
                default:
                    return packetType; // If not an internal packet, return the packet type to process it.
            }
            // If is a internal packet, return null to ignore it.
            return default;
        }

        private void AddChannel(ushort playerId)
        {
            ChannelsData.TryAdd((playerId, (byte)ChannelMode.Reliable), new ChannelData());
            ChannelsData.TryAdd((playerId, (byte)ChannelMode.ReliableSequenced), new ChannelData());
        }

        float _reTime = 0f;
        public void ReTransmit(float deltaTime)
        {
            _reTime -= deltaTime;
            if (_reTime <= 0)
            {
                //Re-try to send the data that was not received.
                // This send is called when the data is lost.
                if (ChannelsData.Count > 0)
                {
                    foreach (var pKvP in ChannelsData.ToList())
                    {
                        // Packets that are not received.
                        var packets = pKvP.Value.PacketsToReTransmit.ToList();
                        foreach (var packet in packets)
                        {
                            LogHelper.Error($"[Neutron] -> Re-try to send packet {packet.Key}.");
                            TransmissionPacket transmissionPacket = packet.Value;
                            // Calc the last time we sent the packet.
                            TimeSpan currentTime = DateTime.UtcNow.Subtract(transmissionPacket.LastSent);
                            // If the time elapsed is greater than X second, the packet is re-sent if the packet is not acknowledged.
                            if (currentTime.TotalSeconds >= 0.120d)
                            {
                                LogHelper.Error($"Re-transmit packet {pKvP.Key} : {transmissionPacket.SeqAck}");
                                if (transmissionPacket.IsDisconnected())
                                    pKvP.Value.PacketsToReTransmit.Remove(transmissionPacket.SeqAck, out _);
                                else
                                    Enqueue(transmissionPacket.Data);
                                // Set the last time to current time when the packet is sent.
                                transmissionPacket.LastSent = DateTime.UtcNow;
                            }
                        }
                    }
                }
                else
                    LogHelper.Error("[Neutron] -> No channels.");
                _reTime = 0f;
            }
        }

        private void CreateTransmissionPacket(UdpPacket udpPacket)
        {
            if (udpPacket.ChannelMode == ChannelMode.Reliable || udpPacket.ChannelMode == ChannelMode.ReliableSequenced)
            {
                // Don't create the transmission packet in Ack packets. 
                // because we are retransmitting the packet to the owner of the packet, in this case,
                // who takes care of the retransmission is the owner of the packet, 
                // so we can't re-transmit the packet because it already does that, otherwise it's in a retransmission loop.
                // Only clients who do not own(owner) the packet can re-transmit the packet.
                if (udpPacket.OperationMode == OperationMode.Acknowledgement)
                    return;

                // If channelData contains the packet, it means that the packet was lost, and we need to re-transmit it.
                if (!udpPacket.ChannelData.PacketsToReTransmit.ContainsKey(udpPacket.SeqAck))
                    udpPacket.ChannelData.PacketsToReTransmit.TryAdd(udpPacket.SeqAck, new TransmissionPacket(udpPacket.SeqAck, DateTime.UtcNow, udpPacket));
                else
                {
                    if (udpPacket.ChannelData.PacketsToReTransmit[udpPacket.SeqAck].IsDisconnected())
                        udpPacket.ChannelData.PacketsToReTransmit.Remove(udpPacket.SeqAck, out _);
                }
            }
        }

        /// <summary>
        /// Enqueue the data to the send queue.
        /// </summary>
        private void Enqueue(UdpPacket udpPacket)
        {
            // Enqueue data to send.
            // This operation is thread safe, it's necessary to lock the queue? Yes, because data can be enqueued from different threads,
            // example: Unity's main thread and the receive thread.
            _dataToSend.Push(udpPacket);
        }

        /// <summary>
        /// Send the data to the remote host. Server->Client.
        /// </summary>
        public void SendToClient(NeutronStream dataStream, ChannelMode channel, TargetMode targetMode, OperationMode opMode, ushort playerId, EndPoint endPoint)
        {
            if (opMode == OperationMode.Sequence)
                SendToClient(dataStream, channel, targetMode, playerId, endPoint);
        }

        /// <summary>
        /// Send the data to the remote host. Client->Server.
        /// </summary>
        public void SendToServer(NeutronStream dataStream, ChannelMode channel, TargetMode targetMode)
        {
            if ((channel == ChannelMode.ReliableSequenced || channel == ChannelMode.Reliable) && _id == 0)
                throw new Exception("[Neutron] -> You must connect to the server before sending data.");
            Send(dataStream, channel, targetMode, OperationMode.Sequence, _id, _destEndPoint, 0);
        }

        /// <summary>
        /// Send the data to the remote host. Server->Client.
        /// </summary>
        private void SendToClient(NeutronStream dataStream, ChannelMode channel, TargetMode targetMode, ushort playerId, EndPoint endPoint)
        {
            Send(dataStream, channel, targetMode, OperationMode.Data, playerId, endPoint, 0);
        }

        private void Send(NeutronStream dataStream, ChannelMode channelMode, TargetMode targetMode, OperationMode opMode, ushort playerId, EndPoint endPoint, int seqAck = 0)
        {
            // Get the data from the stream.
            // This data will be sent to the remote host.
            var data = dataStream.Writer.GetBufferAsReadOnlySpan();
            // Let's to create the packet.
            // The header includes the channel, the sequence number and the acknowledgment number for reliable channels.
            using (NeutronStream packet = Neutron.PooledNetworkStreams.Pull())
            {
                var writer = packet.Writer;
                writer.Write((byte)channelMode);
                writer.Write((byte)targetMode);
                writer.Write((byte)opMode);
                writer.Write(playerId);
                if (channelMode == ChannelMode.Reliable || channelMode == ChannelMode.ReliableSequenced)
                {
                    (ushort, byte) chKey = (playerId, (byte)channelMode);
                    ChannelData channelData = ChannelsData[chKey];
                    // If sequence is 0, the packet is sent as a new packet, otherwise it is a re-transmission.
                    if (seqAck == 0)
                        seqAck = Interlocked.Increment(ref channelData.SentAck);
                    // The client sends the sequence number(4 bytes(int)) and the data.
                    writer.Write(seqAck);
                    writer.Write(data);
                    // Create a copy of the packet.
                    byte[] pData = writer.GetBufferAsCopy();
                    // Create UdpPacket to send.
                    // The packet includes the data and the endpoint.
                    // The packet is sent to the remote host.
                    UdpPacket udpPacket = new UdpPacket(seqAck, playerId, DateTime.UtcNow, endPoint, opMode, targetMode, channelMode, channelData, pData);
                    // Send the reliable packet.
                    Enqueue(udpPacket);
                }
                else if (channelMode == ChannelMode.Unreliable)
                {
                    // Send the unreliable packet.
                    writer.Write(data);
                    UdpPacket udpPacket = new UdpPacket(playerId, endPoint, opMode, targetMode, channelMode, writer.GetBufferAsCopy());
                    Enqueue(udpPacket);
                }
            }
        }

        private void StartSendThread()
        {
            new Thread(() =>
            {
                // Let' send the data in a loop.
                while (!_cts.IsCancellationRequested)
                {
                    // Le't get the data from the queue and send it.
                    // This collection is blocked until the data is available, prevents de CPU from spinning.
                    UdpPacket udpPacket = _dataToSend.Pull();
                    if (!_isConnected)
                    {
                        // isConnected is false, is server, so we need to send the data to the client or clients.
                        switch (udpPacket.TargetMode)
                        {
                            // Send and Create the transmission packet.
                            // This transmission packet is used to re-transmit the packet if the packet is lost.
                            // The packet is not created if the packet is an acknowledgment packet or is a duplicate packet.
                            case TargetMode.All:
                                _socket.SendTo(udpPacket.Data, udpPacket.EndPoint);
                                // Send the data to all the clients.
                                foreach (var KvP in ClientsByEndPoint.ToList())
                                {
                                    if (KvP.Key != udpPacket.EndPoint)
                                    {
                                        _socket.SendTo(udpPacket.Data, KvP.Key);
                                        CreateTransmissionPacket(udpPacket);
                                    }
                                    else
                                        continue;
                                }
                                break;
                            case TargetMode.Others:
                                // If the packet is sent to others, it's necessary to send it to all the clients except the sender.
                                // The sender is the owner of the packet, so we don't need to send it to the sender.
                                foreach (var KvP in ClientsByEndPoint.ToList())
                                {
                                    if (KvP.Key != udpPacket.EndPoint)
                                    {
                                        _socket.SendTo(udpPacket.Data, KvP.Key);
                                        CreateTransmissionPacket(udpPacket);
                                    }
                                    else
                                        continue;
                                }
                                break;
                            case TargetMode.Single:
                                // Send the packet to the remote host.
                                _socket.SendTo(udpPacket.Data, udpPacket.EndPoint);
                                CreateTransmissionPacket(udpPacket);
                                break;
                        }
                    }
                    else
                    {
                        // isConnected is true, is client, so we need to send the data to the server.
                        // Send the packet to the remote host.
                        _socket.SendTo(udpPacket.Data, udpPacket.EndPoint);
                        CreateTransmissionPacket(udpPacket);
                    }
                }
            })
            {
                Name = "Neutron_SentThread",
                Priority = ThreadPriority.Normal,
                IsBackground = true,
            }.Start();
        }

        private void StartReceiveThread()
        {
            new Thread(() =>
            {
                try
                {
                    BandwidthCounter bandwidthCounter = new BandwidthCounter();
                    // The endpoint used store the address of the remote host.
                    // I made a wrapper for this because a lot of garbage will be created if we use the IPEndPoint directly.
                    // Note that: the client must send something to the server first(establish a connection), otherwise, the directly send from the server to the client will fail, this is called a "Handshake".
                    // To P2P, the client send a packet to the server, and the server send a packet to the client(establish a connection), Now the client's router allows the remote host to send packets to the client and vice versa.
                    // Let's get the address and port of the client and send to the others clients, others clients will send to this address and port, this iw how P2P works.
                    // Remember that we need the server to keep sending packets to the client(Keep Alive) and vice versa, otherwise, the connection will be lost.
                    // This technique is known as "UDP Hole Punching".
                    EndPoint _peerEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    // Create a buffer to receive the data.
                    // The size of the buffer is the maximum size of the data that can be received(MTU Size).
                    byte[] buffer = new byte[1536];
                    // A memory block to store the received data.
                    // Prevent a new allocation every time we receive data and the copy of the data.
                    Memory<byte> memoryBuffer = buffer;
                    // Start receiving data.
                    while (!_cts.IsCancellationRequested)
                    {
                        bandwidthCounter.Start();
                        int bytesTransferred = _socket.ReceiveFrom(buffer, ref _peerEndPoint);
                        if (bytesTransferred >= 0)
                        {
                            bandwidthCounter.Stop();
                            bandwidthCounter.Add(bytesTransferred);
                            bandwidthCounter.Get();
                            using (NeutronStream dataStream = Neutron.PooledNetworkStreams.Pull())
                            {
                                var writer = dataStream.Writer;
                                var reader = dataStream.Reader;
                                reader.SetBuffer(memoryBuffer[..bytesTransferred].Span);
                                // Get the header of Custom Protocol and create the MultiEndPoint
                                // MultiEndPoint is a tuple that contains the address of the remote host, the port and the channel.
                                // MultiEndPoint is used to separate the channels, each player has their own channel separating their data and properties.
                                ChannelMode channel = (ChannelMode)reader.ReadByte();
                                TargetMode targetMode = (TargetMode)reader.ReadByte();
                                OperationMode opMode = (OperationMode)reader.ReadByte();
                                var playerId = reader.ReadUShort();
                                // If the channel is reliable, let's read the sequence number.
                                if (channel == ChannelMode.Reliable || channel == ChannelMode.ReliableSequenced)
                                {
                                    (ushort, byte) chKey = (playerId, (byte)channel);
                                    // The acknowledgement is the first 4 bytes of the packet.
                                    // Let's send it to the remote host to confirm that we received the packet.
                                    int seqAck = reader.ReadInt();
                                    // If the packet was confirmed, let's remove it from the list of packets to re-transmit.
                                    // After that, let's send the data to the remote host.
                                    if (opMode == OperationMode.Acknowledgement)
                                        ChannelsData[chKey].PacketsToReTransmit.Remove(seqAck, out _);
                                    else
                                    {
                                        // Send the acknowledgement to the remote host to confirm that we received the packet.
                                        // If the Ack is dropped, the remote host will resend the packet.
                                        Send(NeutronStream.Empty, channel, TargetMode.Single, OperationMode.Acknowledgement, playerId, _peerEndPoint, seqAck);
                                        // Read the left data in the packet.
                                        // All the data sent by the remote host is stored in the buffer.
                                        byte[] data = reader.ReadNext();
                                        // Let's process the data and send it to the remote host again.
                                        if (channel == ChannelMode.Reliable)
                                        {
                                            // Let's to check if the packet is a duplicate.
                                            // If the packet is a duplicate, let's ignore it.
                                            // It's necessary to check if the packet is a duplicate, because the Ack can be lost, and the packet will be re-transmitted.
                                            if (!ChannelsData[chKey].ReceivedSequences.TryAdd(seqAck, seqAck))
                                                continue;
                                            // Let's send the data to the remote host.
                                            // Don't send the data to the remote host if the packet is a duplicate.
                                            // Don't send the data is opMode is Acknowledgement or Data.
                                            using (NeutronStream stream = Neutron.PooledNetworkStreams.Pull())
                                            {
                                                var sReader = stream.Reader;
                                                sReader.SetBuffer(data);
                                                OnMessageCompleted?.Invoke(stream, playerId, _peerEndPoint, channel, targetMode, opMode, this);
                                            }
                                        }
                                        else if (channel == ChannelMode.ReliableSequenced)
                                        {
                                            // Let's to check if the packet is a duplicate.
                                            // If the packet is a duplicate, let's ignore it.
                                            // It's necessary to check if the packet is a duplicate, because the Ack can be lost, and the packet will be re-transmitted.
                                            // As the data is sequenced, the verification is easy.
                                            // Ex: If the last processed packet is 100, this means that all data between 0 and 100 was received and processed, sequencing assures us of this.
                                            // Then we can safely ignore and remove packets from 0 to 100.
                                            if (seqAck <= ChannelsData[chKey].LastProcessedSequentialAck)
                                                continue;
                                            // Let's sequence the data and process when the sequence is correct and then remove it.
                                            if (ChannelsData[chKey].SequentialData.TryAdd(seqAck, data))
                                            {
                                                if (ChannelsData[chKey].IsSequential())
                                                {
                                                    var KvPSenquentialData = ChannelsData[chKey].SequentialData.ToList();
                                                    for (int i = 0; i < KvPSenquentialData.Count; i++)
                                                    {
                                                        var KvP = KvPSenquentialData[i];
                                                        if (KvP.Key > ChannelsData[chKey].LastProcessedSequentialAck)
                                                        {
                                                            // Let's process the data and send it to the remote host again.
                                                            using (NeutronStream stream = Neutron.PooledNetworkStreams.Pull())
                                                            {
                                                                var sReader = stream.Reader;
                                                                sReader.SetBuffer(KvP.Value);
                                                                OnMessageCompleted?.Invoke(stream, playerId, _peerEndPoint, channel, targetMode, opMode, this);
                                                            }
                                                            // Increment the last processed sequence.
                                                            // Indicates the last processed packet, used to discard the packets that were already processed.
                                                            // Any value lesser than this value means that the packet was already processed.
                                                            ChannelsData[chKey].LastProcessedSequentialAck++;
                                                        }
                                                        else
                                                            ChannelsData[chKey].SequentialData.Remove(KvP.Key);
                                                    }
                                                    ChannelsData[chKey].LastReceivedSequentialAck = KvPSenquentialData.Last().Key;
                                                }
                                                else {/*Waiting for more packets, to create the sequence*/}
                                            }
                                            else {/*Discard duplicate packet....*/}
                                        }
                                    }
                                }
                                else if (channel == ChannelMode.Unreliable)
                                {
                                    // Read the left data in the packet.
                                    // All the data sent by the remote host is stored in the buffer.
                                    byte[] data = reader.ReadNext();
                                    // Let's process the data and send it to the remote host again.
                                    using (NeutronStream stream = Neutron.PooledNetworkStreams.Pull())
                                    {
                                        var sReader = stream.Reader;
                                        sReader.SetBuffer(data);
                                        OnMessageCompleted?.Invoke(stream, playerId, _peerEndPoint, channel, targetMode, opMode, this);
                                    }
                                }
                            }
                        }
                        else
                            LogHelper.Error("\r\nReceiveFrom() failed with error code: " + bytesTransferred);
                    }
                }
                catch (SocketException ex)
                {
                    if (ex.ErrorCode == 10004)
                        return;

                    LogHelper.Stacktrace(ex);
                }
                catch (ThreadAbortException) { }
                catch (Exception ex)
                {
                    LogHelper.Stacktrace(ex);
                }
            })
            {
                Name = "Neutron_RecvThread",
                Priority = ThreadPriority.Highest,
                IsBackground = true,
            }.Start();
        }

        private bool GetAvailableID(out ushort id)
        {
            if (_ids.Count > 0)
            {
                if (!_ids.TryPull(out id))
                    id = 0;
            }
            else
                id = 0;
            return id > NeutronConstants.GENERATE_PLAYER_ID;
        }

        public Socket GetSocket()
        {
            return _socket;
        }

        public void Close()
        {
            try
            {
                _socket.Close();
                _cts.Cancel();
            }
            finally
            {
                _cts.Dispose();
            }
        }
    }
}