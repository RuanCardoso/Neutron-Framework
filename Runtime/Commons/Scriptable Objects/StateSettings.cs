using MarkupAttributes;
using NeutronNetwork.Attributes;
using NeutronNetwork.Constants;
using NeutronNetwork.Internal.Packets;
using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace NeutronNetwork
{
    [CreateAssetMenu(menuName = "Neutron/State Settings", fileName = "State Settings")]
    public class StateSettings : MarkupScriptable
    {
        [Serializable]
        public class NeutronAddress
        {
            public string Address = "localhost";
            [Naughty.Attributes.InfoBox("Do not use exclusive ports from other applications. This can make you vulnerable.", Naughty.Attributes.EInfoBoxType.Warning)]
            public int Port = 1418;
        }

        [ReadOnly]
        [Box("Neutron Connection")]
        public string AppId;
        public NeutronAddress[] Addresses = new NeutronAddress[1] { new NeutronAddress() };

        [Foldout("Pool Capacity")]
        [Naughty.Attributes.InfoBox("These values ​​must be double the amount if client and server are running on the same machine or Editor.")]
        [Naughty.Attributes.InfoBox("The higher the value, the more RAM will be used, depending on the amount of objects in the pool.", Naughty.Attributes.EInfoBoxType.Warning)]
        [Naughty.Attributes.InfoBox("Values that are too high can cause startup delay.", Naughty.Attributes.EInfoBoxType.Warning)]
        public int NeutronStream = 20;
        public int NeutronPacket = 20;
        [Naughty.Attributes.InfoBox("Number of socket objects that can send and receive data.")]
        public int AcceptPool = 3;
        public int SocketReceive = 120;
        public int SocketSend = 120;

        [Foldout("Server Settings")]
        [Range(1, ushort.MaxValue)] public int MaxPlayers = 60;
        public int BackLog = 10;
        public bool FiltersLogging;

        [Foldout("Byte Settings")]
        [Naughty.Attributes.InfoBox("This can have some significant impact on the CPU.", Naughty.Attributes.EInfoBoxType.Warning)]
        [Naughty.Attributes.InfoBox("Serialization will only be used for non-primitive objects.")]
        public SerializationMode Serialization = SerializationMode.Json;
        [Naughty.Attributes.InfoBox("Compression will be applied to all data, although it can be manually applied to each Stream when None.")]
        public CompressionMode Compression = CompressionMode.LZ4;

        [Foldout("Socket Options")]
        [Naughty.Attributes.InfoBox("Multiplies the size of the data send and recv buffer. Beware of RAM memory usage.", Naughty.Attributes.EInfoBoxType.Warning)]
        public int _MultiplierSize = 1;
        [Foldout("Socket Options/Udp")] [Naughty.Attributes.InfoBox("Send the maintenance packets every X seconds, to keep the connection alive.")] public float KeepAlive = 2F; // udp
        [Range(1, 1472)]
        [Naughty.Attributes.InfoBox("Values ​​that are too high can cause large packet loss.", Naughty.Attributes.EInfoBoxType.Warning)]
        public int MTUSize = (int)(0.5 * 1024);
        [Naughty.Attributes.InfoBox("Sets the size of the network stack's internal buffers. The higher the value, the more RAM will be used.", Naughty.Attributes.EInfoBoxType.Warning)]
        public int RecBufferSize = 8 * 1024;
        public int SendBufferSize = 8 * 1024;
        [Foldout("Socket Options/Tcp")] [Naughty.Attributes.InfoBox("Send the maintenance packets every X seconds, to keep the connection alive.")] public float _KeepAlive = 5F; // tcp
        [Range(1, 65535)]
        [Naughty.Attributes.InfoBox("The maximum size of the packet that can be sent/received.")]
        public int _PacketSize = 2 * 1024;
        [Naughty.Attributes.InfoBox("Specifies whether the server disables the delay of sending successive small packets on the network.")]
        public bool _NoDelay = true;
        [Naughty.Attributes.InfoBox("Specifies that TCP should not copy data to a new buffer, but use it directly.")]
        public bool _ZeroStack;
        [Naughty.Attributes.InfoBox("Sets the size of the network stack's internal buffers. The higher the value, the more RAM will be used.", Naughty.Attributes.EInfoBoxType.Warning)]
        [HideIf(nameof(_ZeroStack))] public int _RecBufferSize = 8 * 1024;
        [HideIf(nameof(_ZeroStack))] public int _SendBufferSize = 8 * 1024;













        //[InfoBox("Remember that on the local host the processing is used 4x times more, this is because you are running Client and Server on the same machine. Send(Client/Server), Send(Client/Server).", EInfoBoxType.Warning)]
        //[InfoBox("Performance is drastically reduced in Unity Editor. For performance testing it is recommended to run on a real server.", EInfoBoxType.Warning)]
        [HideInInspector] public NeutronGlobalSettings GlobalSettings = new NeutronGlobalSettings();
        /* [HorizontalLine]*/
        [HideInInspector] public NeutronClientSettings ClientSettings;
        /* [HorizontalLine]*/
        [HideInInspector] public NeutronServerSettings ServerSettings;
        /* [HorizontalLine]*/
        [HideInInspector] public NeutronConstantsSettings NetworkSettings;

        [ContextMenu("Generate AppId")]
        public void NewGuid()
        {
#if UNITY_EDITOR
            GlobalSettings.AppId = Guid.NewGuid().ToString();
#endif
        }

        public void Reset()
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(GlobalSettings.AppId))
                NewGuid();
#endif
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            for (int i = 0; i < GlobalSettings.Addresses.Length; i++)
            {
                GlobalSettings.Addresses[i] = GlobalSettings.Addresses[i].Replace(" ", "");
            }
#endif
        }
    }
}