using NeutronNetwork;
using NeutronNetwork.Extensions;
using NeutronNetwork.Internal;
using NeutronNetwork.Internal.Components;
using System;

namespace NeutronNetwork.Helpers
{
    public static class NeutronHelper
    {
        private static readonly string[] SizeSuffixes = { "B/s", "kB/s", "mB/s", "gB/s" };

        public const int BUFFER_SIZE = 1024;
        public static bool iRPC(byte[] parameters, bool isMine, RemoteProceduralCall remoteProceduralCall, Player sender, NeutronMessageInfo infor, NeutronView neutronView)
        {
            #region Pool
            var pool = Neutron.PooledNetworkReaders.Pull();
            pool.SetBuffer(parameters);
            #endregion

            #region Reflection
            object obj = remoteProceduralCall.Invoke(pool, isMine, sender, infor);
            if (obj != null)
            {
                Type objType = obj.GetType();
                if (objType == typeof(bool))
                    return (bool)obj;
            }
            return true;
            #endregion
        }

        public static bool sRPC(int sRPCId, Player sender, byte[] parameters, RemoteProceduralCall remoteProceduralCall, bool isServer, bool isMine, Neutron localInstance = null)
        {
            #region Pool
            var pool = Neutron.PooledNetworkReaders.Pull();
            pool.SetLength(0);
            pool.SetBuffer(parameters);
            pool.SetPosition(0);
            #endregion

            object obj = remoteProceduralCall.Invoke(pool, isServer, isMine, sender, localInstance);
            if (obj != null)
            {
                Type objType = obj.GetType();
                if (objType == typeof(NeutronView))
                {
                    NeutronView objectToInst = (NeutronView)obj;
                    if (!isServer)
                        SceneHelper.MoveToContainer(objectToInst.gameObject, "[Container] -> Player[Main]");
                    else
                    {
                        if (!sender.IsInRoom())
                            SceneHelper.MoveToContainer(objectToInst.gameObject, $"[Container] -> Channel[{sender.CurrentChannel}]");
                        else if (sender.IsInChannel()) SceneHelper.MoveToContainer(objectToInst.gameObject, $"[Container] -> Room[{sender.CurrentRoom}]");
                    }
                    if (sRPCId == 1001)
                        NeutronRegister.RegisterPlayer(sender, objectToInst, isServer, localInstance);
                    else if (sRPCId == 1002)
                    {
                        using (NeutronReader defaultOptions = Neutron.PooledNetworkReaders.Pull())
                        {
                            defaultOptions.SetBuffer(parameters);

                            defaultOptions.SetPosition((sizeof(float) * 3) + (sizeof(float) * 4));
                            NeutronRegister.RegisterObject(sender, objectToInst, defaultOptions.ReadInt32(), isServer, localInstance);
                        }
                    }
                    else return true;
                }
                else if (objType == typeof(bool))
                    return (bool)obj;
                else NeutronLogger.LoggerError("invalid type hehehe");
            }
            //else NeutronLogger.LoggerError("Invalid sRPC ID, there is no attribute with this ID.");
            return true;
        }

        public static string SizeSuffix(long value, int mag = 0, int decimalPlaces = 2)
        {
            if (decimalPlaces < 0) { throw new ArgumentOutOfRangeException("decimalPlaces"); }
            if (value < 0) { return "-" + SizeSuffix(-value, decimalPlaces); }
            if (value == 0) { return string.Format("{0:n" + decimalPlaces + "} B/s", 0); }

            // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
            if (mag <= 0)
                mag = (int)Math.Log(value, 1024);

            // 1L << (mag * 10) == 2 ^ (10 * mag) 
            // [i.e. the number of bytes in the unit corresponding to mag]
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            // make adjustment when the value is large enough that
            // it would round up to 1000 or more
            if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
            {
                mag += 1;
                adjustedSize /= 1024;
            }

            return string.Format("{0:n" + decimalPlaces + "} {1}",
                adjustedSize,
                SizeSuffixes[mag]);
        }

        public static int GetMaxPacketsPerSecond(float sInterval)
        {
#if UNITY_SERVER || UNITY_EDITOR
            int currentFPS = NeutronConfig.Settings.ServerSettings.FPS;
            if (sInterval == 0)
                return currentFPS;
            float interval = (sInterval * currentFPS);
            float MPPS = currentFPS / interval;
            return (int)MPPS;
#else
            return 0;
#endif
        }

#if !UNITY_2019_2_OR_NEWER
        public static bool TryGetComponent<T>(this GameObject monoBehaviour, out T component)
        {
            component = monoBehaviour.GetComponent<T>();
            if (component != null)
                return (component.ToString() != null && component.ToString() != "null");
            else return false;
        }

        public static bool TryGetComponent<T>(this Transform monoBehaviour, out T component)
        {
            component = monoBehaviour.GetComponent<T>();
            if (component != null)
                return (component.ToString() != null && component.ToString() != "null");
            else return false;
        }
#endif
    }
}