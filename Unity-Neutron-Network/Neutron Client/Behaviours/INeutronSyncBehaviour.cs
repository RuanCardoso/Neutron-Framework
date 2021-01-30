using NeutronNetwork;
using NeutronNetwork.Internal.Extesions;
using NeutronNetwork.Internal.Server;
using NeutronNetwork.Wrappers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace NeutronNetwork
{
    public class NeutronSyncBehaviour : NeutronBehaviour
    {
        private Dictionary<string, object> observerDict = new Dictionary<string, object>(); // thread safe.
        [SerializeField] [Range(0, 10000)] private int updateFrequency = 100;

        private HashSet<Type> supportedTypes = new HashSet<Type>()
    {
        typeof(int),
        typeof(bool),
        typeof(float),
        typeof(string),
        typeof(SerializableColor),
        typeof(SerializableVector3),
        typeof(SerializableQuaternion),
        typeof(ObservableList<int>),
        typeof(ObservableList<bool>),
        typeof(ObservableList<float>),
        typeof(ObservableList<string>),
        typeof(ObservableList<SerializableColor>),
        typeof(ObservableList<SerializableVector3>),
        typeof(ObservableList<SerializableQuaternion>),
    };

        public void Init()
        {
            ThreadPool.QueueUserWorkItem((e) => SyncVar());
        }

        private bool CheckKeyExists<T>(string key, out T value)
        {
            value = default;
            bool contains = observerDict.ContainsKey(key);
            if (contains) value = (T)observerDict[key];
            return contains;
        }

        private bool ChangesObserver<T>(string fieldName, T newValue)
        {
            if (CheckKeyExists(fieldName, out T oldValue))
            {
                if (!oldValue.Equals(newValue))
                {
                    observerDict[fieldName] = newValue;
                    return true;
                }
                else return false;
            }
            else return false;
        }

        private bool AddFields(FieldInfo[] Fields)
        {
            for (int i = 0; i < Fields.Length; i++)
            {
                SyncVarAttribute fieldAttribute = Fields[i].GetCustomAttribute<SyncVarAttribute>();
                if (fieldAttribute != null)
                {
                    object value = Fields[i].GetValue(this);
                    if (supportedTypes.Contains(value.GetType()))
                    {
                        if (Fields[i].FieldType.IsGenericType)
                        {
                            var fieldDelegate = value.GetType().GetField("onChanged", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            if (fieldDelegate != null)
                            {
                                try
                                {
                                    object del = Delegate.CreateDelegate(fieldDelegate.FieldType, this, "OnObservableListChanged");
                                    fieldDelegate.SetValue(value, del);
                                }
                                catch (Exception message) { Debug.LogError(message.Message); }
                            }
                        }
                        observerDict.Add(Fields[i].Name, value);
                    }
                }
            }
            return true;
        }

        public virtual void OnObservableListChanged()
        {
            Debug.LogError("criou ae dssdsd");
        }

        private void InvokeOptions(string functionName, SendTo sendTo, Broadcast broadcast, ProtocolType protocolType)
        {
            if (!string.IsNullOrEmpty(functionName))
            {
                MethodInfo method = this.GetType().GetMethod(functionName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                method?.Invoke(this, null);
            }
            Send(sendTo, broadcast, protocolType);
        }

        private void Send(SendTo sendTo, Broadcast broadcast, ProtocolType protocolType)
        {
            try
            {
                string props = JsonConvert.SerializeObject(observerDict);
                using (NeutronWriter writer = new NeutronWriter())
                {
                    writer.WritePacket(Packet.SyncBehaviour);
                    writer.Write(ServerView.player.ID);
                    writer.Write(props);
                    ServerView?.player.Send(sendTo, writer.ToArray(), broadcast, null, protocolType);
                }
            }
            catch (Exception ex) { Debug.LogError(ex.Message); }
        }

        private async void SyncVar()
        {
            FieldInfo[] Fields = this.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (AddFields(Fields))
            {
                while (NeutronServer.Initialized)
                {
                    for (int i = 0; i < Fields.Length; i++)
                    {
                        FieldInfo Field = Fields[i];
                        SyncVarAttribute fieldAttribute = Field.GetCustomAttribute<SyncVarAttribute>();
                        if (fieldAttribute != null)
                        {
                            object type = Field.GetValue(this);
                            if (supportedTypes.Contains(type.GetType()))
                            {
                                switch (type)
                                {
                                    case var value:
                                        {
                                            if (ChangesObserver(Field.Name, value))
                                            {
                                                InvokeOptions(fieldAttribute.function, fieldAttribute.sendTo, fieldAttribute.broadcast, fieldAttribute.protocolType);
                                            }
                                        }
                                        break;
                                }
                            }
                            else { Utils.LoggerError($"[SyncVar] unsupported type -> [{type.GetType().Name}], Use the serializable class -> [Serializable{type.GetType().Name}]"); }
                        }
                    }
                    await Task.Delay(updateFrequency);
                }
            }
        }

        protected void OnNotifyChange(NeutronSyncBehaviour syncBehaviour, string propertiesName, Broadcast broadcast)
        {
            if (ServerView != null) NeutronSFunc.onChanged(ServerView.player, syncBehaviour, propertiesName, broadcast);
        }
    }
}