/* using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

public class ServerUDP : NeutronSDatabase {
    public void OnUDPVoiceReceive (IAsyncResult ia) {
        try {
            byte[] data = _UDPVoiceSocket.EndReceive (ia, ref _IEPRefVoice);
            //-----------------------------------------------------------------------------\\
            _UDPVoiceSocket.BeginReceive (OnUDPVoiceReceive, null);
            //-----------------------------------------------------------------------------\\
            if (data.Length > 0) {
                lock (lockerUDPEndPointsVoices) {
                    if (!udpEndPointsVoices.Contains (_IEPRefVoice)) udpEndPointsVoices.Add (_IEPRefVoice);
                }
                //-----------------------------------------------------------------------------\\
                using (NeutronReader reader = new NeutronReader (data)) {
                    int port = reader.ReadInt32 ();
                    byte[] buffer = reader.ReadBytes (4092);
                    //-----------------------------------------------------------------------------\\
                    if (GetPlayer (new IPEndPoint (_IEPRefVoice.Address, port), out Player Sender)) {
                        SendVoice (SendTo.Others, buffer, Sender.tcpClient.RemoteEndPoint (), _IEPRefVoice, tcpPlayers.Values.ToArray ());
                    }
                }
            } else { }
        } catch (Exception ex) {
            Utils.LoggerError (ex.Message);
            //----------------------------------------------\\
            RenitializeVoiceWhenException (_IEPRefVoice);
        }
    }

    void RenitializeVoiceWhenException (IPEndPoint EndPointException) {
        lock (lockerUDPEndPointsVoices) {
            udpEndPointsVoices.Clear ();
        }
        _UDPVoiceSocket.BeginReceive (OnUDPVoiceReceive, null);
    }

    void SendVoice (SendTo sendTo, byte[] buffer, IPEndPoint comparer, IPEndPoint onlyEndPoint, Player[] ToSend = null) {
        switch (sendTo) {
            case SendTo.All:
                lock (lockerUDPEndPointsVoices) {
                    if (ToSend.Any (x => x.tcpClient.RemoteEndPoint ().Equals (comparer))) {
                        foreach (var _ip in udpEndPointsVoices) {
                            _UDPVoiceSocket.BeginSend (buffer, buffer.Length, _ip, (e) => {
                                _UDPVoiceSocket.EndSend (e);
                            }, null);
                        }
                    }
                }
                break;
            case SendTo.Only:
                _UDPVoiceSocket.BeginSend (buffer, buffer.Length, onlyEndPoint, (e) => {
                    _UDPVoiceSocket.EndSend (e);
                }, null);
                break;
            case SendTo.Others:
                lock (lockerUDPEndPointsVoices) {
                    if (ToSend.Any (x => x.tcpClient.RemoteEndPoint ().Equals (comparer))) {
                        foreach (var _ip in udpEndPointsVoices) {
                            if (_ip.Equals (onlyEndPoint)) continue;

                            _UDPVoiceSocket.BeginSend (buffer, buffer.Length, _ip, (e) => {
                                _UDPVoiceSocket.EndSend (e);
                            }, null);
                        }
                    }
                }
                break;
        }
    }
} */