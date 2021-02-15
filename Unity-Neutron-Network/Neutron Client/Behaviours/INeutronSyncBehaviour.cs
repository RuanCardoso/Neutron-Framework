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
        public bool overwrite = false;
        private ManualResetEvent manualResetEvent = new ManualResetEvent(false);
        private bool _goto = false;
        private Dictionary<string, object> observerDict = new Dictionary<string, object>(); // thread safe.
        [SerializeField] [Range(0, 10000)] private int updateFrequency = 100;
        [SerializeField] private bool highPerfomance = false;

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
            typeof(ObservableDictionary<string, int>),
            typeof(ObservableDictionary<string, bool>),
            typeof(ObservableDictionary<string, float>),
            typeof(ObservableDictionary<string, string>),
            typeof(ObservableDictionary<int, string>),
            typeof(ObservableDictionary<int, bool>),
            typeof(ObservableDictionary<int, float>),
            typeof(ObservableDictionary<int, int>),
        };

        protected void AddType(Type item)
        {
            if (item.IsSerializable)
                supportedTypes.Add(item);
            else Utils.LoggerError($"[{item.Name}] Is not serializable!");
        }

        public void Start()
        {
#if UNITY_SERVER || UNITY_EDITOR
            if (IsServer)
                ThreadPool.QueueUserWorkItem((e) => SyncVar());
#endif
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
                        if (Fields[i].FieldType.IsSerializable)
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
                        else Utils.LoggerError($"[{Fields[i].Name}] Is not serializable!");
                    }
                    else { Utils.LoggerError($"[SyncVar] unsupported type -> [{value.GetType().Name}]"); return false; }
                }
            }
            return true;
        }

        private void InvokeOptions(string functionName, SendTo sendTo, Broadcast broadcast, Protocol protocolType, string field)
        {
            if (!string.IsNullOrEmpty(functionName))
            {
                MethodInfo method = this.GetType().GetMethod(functionName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                method?.Invoke(this, null);
            }
            Send(sendTo, broadcast, protocolType, field);
        }

        private void Send(SendTo sendTo, Broadcast broadcast, Protocol protocolType, string field)
        {
            try
            {
                string props = JsonConvert.SerializeObject((!_goto) ? new Dictionary<string, object>() { { field, observerDict[field] } } : observerDict);
                using (NeutronWriter writer = new NeutronWriter())
                {
                    writer.WritePacket(Packet.SyncBehaviour);
                    writer.Write(NeutronView.owner.ID);
                    writer.Write(props);
                    NeutronView?.owner.Send(sendTo, writer.ToArray(), broadcast, protocolType);
                }
                _goto = false;
                Utils.LoggerError(props);
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
                    if (highPerfomance) manualResetEvent.Reset();
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
                                            if (ChangesObserver(Field.Name, value) || _goto)
                                            {
                                                InvokeOptions(fieldAttribute.onChanged, fieldAttribute.sendTo, fieldAttribute.broadcast, fieldAttribute.protocolType, Field.Name);
                                            }
                                        }
                                        break;
                                }
                            }
                            else { Utils.LoggerError($"[SyncVar] unsupported type -> [{type.GetType().Name}], Use the serializable class -> [Serializable{type.GetType().Name}]"); break; }
                        }
                        else continue;
                    }
                    await Task.Delay(updateFrequency);
                    if (highPerfomance) manualResetEvent.WaitOne();
                }
            }
        }

        protected void Set()
        {
            if (highPerfomance)
                manualResetEvent.Set();
        }
        
        public virtual void OnObservableListChanged()
        {
            _goto = true;
            Set();
        }
    }
}