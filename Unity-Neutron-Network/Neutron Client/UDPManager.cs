//using NeutronNetwork.Internal.Comms;
//using NeutronNetwork.Internal.Extesions;
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.IO;
//using System.Net;
//using System.Net.Sockets;
//using System.Text;
//using UnityEngine;

//namespace NeutronNetwork.Internal.Client
//{
//    public class UDPManager : NeutronCFunc
//    {
//        public void OnUDPReceive(IAsyncResult ia)
//        {
//            try
//            {
//                byte[] data = _UDPSocket.EndReceive(ia, ref _IEPRef);
//                //==============================================================================================\\
//                _UDPSocket.BeginReceive(OnUDPReceive, null);
//                //==============================================================================================\\
//                if (data.Length > 0)
//                {
//                    byte[] decompressedBuffer = data.Decompress(COMPRESSION_MODE);
//                    //==================================================================================\\
//                    using (NeutronReader mReader = new NeutronReader(decompressedBuffer))
//                    {
//                        Packet mCommand = mReader.ReadPacket<Packet>();
//                        switch (mCommand)
//                        {
//                            case Packet.RPC:
//                                HandleRPC(mReader.ReadInt32(), mReader.ReadBytes(8192));
//                                break;
//                            case Packet.APC:
//                                int SRPCID = mReader.ReadInt32();
//                                int playerID = mReader.ReadInt32();
//                                byte[] SRPCParameters = mReader.ReadBytes(2048);
//                                HandleAPC(SRPCID, SRPCParameters, playerID);
//                                break;
//                            case Packet.VoiceChat:
//                                //HandleVoiceChat (mReader.ReadInt32 (), mReader.ReadBytes (Communication.BYTES_READ));
//                                break;
//                            case Packet.SyncBehaviour:
//                                HandleJsonProperties(mReader.ReadInt32(), mReader.ReadString());
//                                break;
//                        }
//                    }
//                }
//                else
//                {
//                    Utils.LoggerError("UDP Error");
//                }
//            }
//            catch (SocketException ex) { Utils.LoggerError(ex.Message + ":" + ex.ErrorCode); }
//        }
//    }
//}