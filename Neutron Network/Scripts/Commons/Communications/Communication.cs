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
        public static bool Dynamic(int dynamicID, byte[] parameters, RemoteProceduralCall remoteProceduralCall, Player sender, NeutronMessageInfo infor, NeutronView neutronView)
        {
            object obj = remoteProceduralCall.Invoke(new NeutronReader(parameters), sender, infor);
            if (obj != null)
            {
                Type objType = obj.GetType();
                if (objType == typeof(bool))
                    return (bool)obj;
                else NeutronUtils.LoggerError("Type not supported");
            }
            return true;
        }

        public static bool NonDynamic(int nonDynamicID, Player sender, byte[] parameters, RemoteProceduralCall remoteProceduralCall, bool isServer, Neutron localInstance = null)
        {
            object obj = remoteProceduralCall.Invoke(new NeutronReader(parameters), isServer, sender, localInstance);
            if (obj != null)
            {
                Type objType = obj.GetType();
                if (objType == typeof(NeutronView))
                {
                    NeutronView objectToInst = (NeutronView)obj;
                    if (!isServer)
                        InternalUtils.MoveToContainer(objectToInst.gameObject, "[Container] -> Player[Main]");
                    else
                    {
                        if (!sender.IsInRoom())
                            InternalUtils.MoveToContainer(objectToInst.gameObject, $"[Container] -> Channel[{sender.CurrentChannel}]");
                        else if (sender.IsInChannel()) InternalUtils.MoveToContainer(objectToInst.gameObject, $"[Container] -> Room[{sender.CurrentRoom}]");
                    }
                    if (nonDynamicID == 1001)
                        NeutronRegister.RegisterPlayer(sender, objectToInst, isServer, localInstance);
                    else if (nonDynamicID == 1002)
                    {
                        using (NeutronReader defaultOptions = new NeutronReader(parameters))
                        {
                            defaultOptions.SetPosition((sizeof(float) * 3) + (sizeof(float) * 4));
                            NeutronRegister.RegisterObject(sender, objectToInst, defaultOptions.ReadInt32(), isServer, localInstance);
                        }
                    }
                    else return true;
                }
                else if (objType == typeof(bool))
                    return (bool)obj;
                else NeutronUtils.LoggerError("Type not supported");
            }
            //else NeutronUtils.LoggerError("Invalid NonDynamic ID, there is no attribute with this ID.");
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