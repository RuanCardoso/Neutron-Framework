using NeutronNetwork;
using NeutronNetwork.Internal.Extesions;
using NeutronNetwork.Internal.Server;
using NeutronNetwork.Internal.Wrappers;
using NeutronNetwork.Wrappers;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace NeutronNetwork
{
    public class NeutronSyncBehaviour : NeutronBehaviour
    {
        private bool signalEvent = false; //* signal to unlock thread.
        private NeutronQueue<Action> syncQueue = new NeutronQueue<Action>();
        private Dictionary<string, object> observerDict = new Dictionary<string, object>();
        [SerializeField] protected bool lowLevelSync = false;
        [SerializeField] [Range(0, 1000)] protected float updateFrequency = 1f; //* frequency in seconds
        private HashSet<Type> supportedTypes = new HashSet<Type>()
        {
            typeof(int),
            typeof(bool),
            typeof(float),
            typeof(string),
            typeof(double),
            typeof(SerializableColor),
            typeof(SerializableVector3),
            typeof(SerializableQuaternion),
            typeof(ObservableList<int>),
            typeof(ObservableList<bool>),
            typeof(ObservableList<float>),
            typeof(ObservableList<string>),
            typeof(ObservableList<double>),
            typeof(ObservableList<SerializableColor>),
            typeof(ObservableList<SerializableVector3>),
            typeof(ObservableList<SerializableQuaternion>),
            typeof(ObservableDictionary<string, int>),
            typeof(ObservableDictionary<string, bool>),
            typeof(ObservableDictionary<string, float>),
            typeof(ObservableDictionary<string, string>),
            typeof(ObservableDictionary<string, double>),
            typeof(ObservableDictionary<string, SerializableColor>),
            typeof(ObservableDictionary<string, SerializableVector3>),
            typeof(ObservableDictionary<string, SerializableQuaternion>),
            typeof(ObservableDictionary<int, string>),
            typeof(ObservableDictionary<int, bool>),
            typeof(ObservableDictionary<int, float>),
            typeof(ObservableDictionary<int, int>),
            typeof(ObservableDictionary<int, double>),
            typeof(ObservableDictionary<int, SerializableColor>),
            typeof(ObservableDictionary<int, SerializableVector3>),
            typeof(ObservableDictionary<int, SerializableQuaternion>),
        };

        protected void Start()
        {
#if UNITY_SERVER || UNITY_EDITOR
            if (IsServer)
                StartCoroutine(InitFieldsSync());
#endif
        }

        protected void Update()
        {
#if UNITY_SERVER || UNITY_EDITOR
            if (IsServer && syncQueue.Count > 0)
                syncQueue.Dequeue().Invoke();
#endif
        }

        protected void Set()
        {
#if UNITY_SERVER || UNITY_EDITOR
            if (IsServer)
            {
                if (lowLevelSync)
                    signalEvent = true;
            }
#endif
        }

        protected void Set(string fieldName)
        {
#if UNITY_SERVER || UNITY_EDITOR
            if (IsServer)
            {
                if (lowLevelSync)
                    DirectSync(fieldName);
            }
#endif
        }

        private IEnumerator InitFieldsSync()
        {
            FieldInfo[] fields = this.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            bool CreateObserver()
            {
                for (int i = 0; i < fields.Length; i++)
                {
                    SyncAttribute syncAttribute = fields[i].GetCustomAttribute<SyncAttribute>();
                    if (syncAttribute != null)
                    {
                        object value = fields[i].GetValue(this);
                        Type valueType = value.GetType();
                        if (supportedTypes.Contains(valueType))
                        {
                            if (fields[i].FieldType.IsSerializable)
                            {
                                if (fields[i].FieldType.IsGenericType)
                                {
                                    var fieldDelegate = valueType.GetField("onChanged", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
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
                                value = Cloneable(value);
                                observerDict.Add(fields[i].Name, value);
                            }
                            else { NeutronUtils.LoggerError($"[{fields[i].Name}] Is not serializable!"); return false; }
                        }
                        else { NeutronUtils.LoggerError($"[SyncVar] unsupported type -> [{valueType.Name}]"); return false; }
                    }
                    else continue;
                }
                return true;
            }

            if (CreateObserver())
            {
                while (true)
                {
                    if (lowLevelSync) { yield return new WaitUntil(() => signalEvent); signalEvent = false; }

                    for (int i = 0; i < fields.Length; i++)
                    {
                        FieldInfo field = fields[i];
                        SyncAttribute syncAttribute = field.GetCustomAttribute<SyncAttribute>();
                        if (syncAttribute != null)
                        {
                            object value = field.GetValue(this);
                            if (ChangesObserver(field.Name, value))
                            {
                                Synchronize(field, syncAttribute);
                            }
                        }
                        else continue;
                    }
                    yield return new WaitForSeconds(updateFrequency);
                }
            }
        }

        protected virtual void OnObservableListChanged(string fieldName)
        {
            if (!lowLevelSync)
                DirectSync(fieldName);
        }

        private void Synchronize(FieldInfo field, SyncAttribute attribute) => SetOptions(attribute.onChanged, attribute.sendTo, attribute.broadcast, attribute.protocolType, field.Name);

        private void DirectSync(string fieldName)
        {
            if (CheckKeyExists(fieldName, out object oldValue))
            {
                FieldInfo fieldInfo = this.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                SyncAttribute syncAttribute = fieldInfo.GetCustomAttribute<SyncAttribute>();
                if (fieldInfo.FieldType.IsGenericType)
                    Synchronize(fieldInfo, syncAttribute);
                else NeutronUtils.LoggerError($"this method can only be used in observable collections. [{fieldInfo.Name}] use \"Set()\" instead of \"Set(string fieldName)\"");
            }
            else NeutronUtils.LoggerError("Invalid fieldName on Observable Collection");
        }

        private void SetOptions(string functionName, SendTo sendTo, Broadcast broadcast, Protocol protocolType, string field)
        {
            syncQueue.Enqueue(() =>
            {
                if (!string.IsNullOrEmpty(functionName))
                    this.GetType().GetMethod(functionName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(this, null);
                Synchronize(sendTo, broadcast, protocolType, field);
            });
        }

        private void Synchronize(SendTo sendTo, Broadcast broadcast, Protocol protocolType, string field)
        {
            string props = JsonConvert.SerializeObject(new Dictionary<string, object>() { { field, observerDict[field] } });
            using (NeutronWriter writer = new NeutronWriter())
            {
                writer.WritePacket(Packet.SyncBehaviour);
                writer.Write(NeutronView.owner.ID);
                writer.Write(props);
                NeutronView.owner.Send(sendTo, writer.ToArray(), broadcast, protocolType);
            }
        }

        private object Cloneable<T>(T newValue)
        {
            if (newValue is INeutronCloneable)
            {
                if (newValue.GetType().IsClass)
                {
                    INeutronCloneable sync = (INeutronCloneable)newValue;
                    return sync.Clone();
                }
                else NeutronUtils.LoggerError($"only classes can implement INeutronCloneable");
            }
            return newValue;
        }

        protected void AddType(Type item)
        {
            if (item.IsSerializable)
                supportedTypes.Add(item);
            else NeutronUtils.LoggerError($"[{item.Name}] Is not serializable!");
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
                    newValue = (T)Cloneable(newValue);
                    observerDict[fieldName] = newValue;
                    return true;
                }
                else return false;
            }
            else return false;
        }
    }
}