using NeutronNetwork;
using NeutronNetwork.Internal.Extesions;
using NeutronNetwork.Internal.Server;
using NeutronNetwork.Internal.Wrappers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
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
    public class NeutronSynchronizeBehaviour : NeutronBehaviour
    {
        #region Primitives
        private string oldJsonValues;
        [SerializeField] [Range(0, 10)] private float synchronizeInterval = 1f;
        #endregion

        #region Collections
        private List<FieldInfo> listOfFields = new List<FieldInfo>();
        #endregion

        #region Neutron
        [SerializeField] private SendTo sendTo = SendTo.All;
        [SerializeField] private Broadcast broadcast = global::Broadcast.Room;
        [SerializeField] [Separator] private Protocol protocol = Protocol.Tcp;
        #endregion
        private bool Synchronizing = false;

        protected override void OnNeutronUpdate()
        {
            base.OnNeutronUpdate();
            if (HasAuthority && !Synchronizing)
                GetFields();
        }

        private void GetFields()
        {
            Synchronizing = true;
            FieldInfo[] fieldInfos = this.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var fieldInfo in fieldInfos)
            {
                if (fieldInfo.GetCustomAttribute<SyncAttribute>() != null)
                    if (fieldInfo.IsPublic)
                        listOfFields.Add(fieldInfo);
                    else Debug.LogError($"Private fields({fieldInfo.Name}) cannot be synchronized.");
                else continue;
            }
            StartCoroutine(FieldsToJson());
        }

        private IEnumerator FieldsToJson()
        {
            while (true)
            {
                var newDictValues = listOfFields.ToDictionary(x => x.Name, y => y.GetValue(this));
                if (newDictValues != null)
                {
                    string newJsonValues = JsonConvert.SerializeObject(newDictValues);
                    if (!string.IsNullOrEmpty(newJsonValues))
                    {
                        if (oldJsonValues != newJsonValues)
                        {
                            Broadcast(newJsonValues);
                            oldJsonValues = newJsonValues;
                        }
                    }
                }
                yield return new WaitForSeconds(synchronizeInterval);
            }
        }

        private void Broadcast(string jsonString)
        {
            using (NeutronWriter writer = new NeutronWriter())
            {
                writer.Write(jsonString);
                Dynamic(24, writer, CacheMode.Overwrite, sendTo, broadcast, protocol);
            }
        }

        [Dynamic(24)]
        public void SyncBehaviour(NeutronReader options, Player sender, NeutronMessageInfo infor)
        {
            using (options)
            {
                if (IsClient || Authority != AuthorityMode.Server)
                {
                    string jsonString = options.ReadString();
                    JsonConvert.PopulateObject(jsonString, this, new JsonSerializerSettings() { ObjectCreationHandling = ObjectCreationHandling.Replace });
                }
            }
        }
    }
}