using NeutronNetwork.Internal.Extesions;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace NeutronNetwork.Internal.Comms
{
    public class Communication
    {
        public const int BUFFER_SIZE = 1024;
        public static bool Dynamic(int dynamicID, byte[] parameters, Player sender, NeutronMessageInfo infor, NeutronView neutronView)
        {
            RemoteProceduralCall remoteProceduralCall = neutronView.Dynamics[dynamicID];
            if (remoteProceduralCall != null)
            {
                object obj = remoteProceduralCall.Invoke(new NeutronReader(parameters), sender, infor);
                if (obj != null)
                {
                    Type objType = obj.GetType();
                    if (objType == typeof(bool))
                        return (bool)obj;
                    else return true;
                }
                else return true;
            }
            else return true;
        }

        public static bool NonDynamic(int nonDynamicID, Player sender, byte[] parameters, bool isServer, Neutron localInstance)
        {
            RemoteProceduralCall remoteProceduralCall = NeutronNonDynamicBehaviour.NonDynamics[nonDynamicID];
            if (remoteProceduralCall != null)
            {
                object obj = remoteProceduralCall.Invoke(new NeutronReader(parameters), isServer, sender, localInstance);
                if (obj != null)
                {
                    Type objType = obj.GetType();
                    if (objType == typeof(GameObject))
                    {
                        GameObject objectToInst = (GameObject)obj;
                        NeutronRegister.RegisterPlayer(sender, objectToInst, isServer, localInstance);
                        if (!isServer)
                        {
                            InternalUtils.MoveToContainer(objectToInst, "[Container] -> Player[Main]");
                        }
                        else
                        {
                            if (!sender.IsInRoom())
                                InternalUtils.MoveToContainer(objectToInst, $"[Container] -> Channel[{sender.CurrentChannel}]");
                            else InternalUtils.MoveToContainer(objectToInst, $"[Container] -> Room[{sender.CurrentRoom}]");
                        }
                    }
                    else if (objType == typeof(bool))
                        return (bool)obj;
                    else return true;
                }
                else return true;
            }
            else return true;

            return true;
        }

        public static async Task<bool> ReadAsyncBytes(Stream stream, byte[] buffer, int offset, int count, CancellationToken token)
        {
            return await Task.Run(async () =>
            {
                int bytesRead = 0;
                try
                {
                    while (count > 0)
                    {
                        if ((bytesRead = await stream.ReadAsync(buffer, offset, count, token)) > 0)
                        {
                            offset += bytesRead;
                            count -= bytesRead;
                        }
                        else return false;
                    }
                    return count <= 0;
                }
                catch { return false; }
            });
        }
    }
}