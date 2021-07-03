using NeutronNetwork;
using NeutronNetwork.Attributes;
using NeutronNetwork.Constants;
using NeutronNetwork.Json;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace NeutronNetwork
{
    public class NeutronSynchronizeBehaviour : NeutronBehaviour
    {
        #region Primitives
        //* Armazena o antigo estados dos campos para comparação.
        private string oldJson;
        [SerializeField] [Range(0, 10)] private float m_SynchronizeInterval = 1f;
        #endregion

        #region Collections
        //* Armazena os campos a serem sincronizados.
        private List<FieldInfo> listOfFields = new List<FieldInfo>();
        #endregion

        // #region Neutron
        // [SerializeField] private CacheMode m_CacheMode = CacheMode.Overwrite;
        // [SerializeField] private SendTo m_SendTo = SendTo.All;
        // [SerializeField] private Broadcast m_Broadcast = global::Broadcast.Room;
        // [SerializeField] private Protocol m_RecProtocol = Protocol.Udp;
        // [SerializeField] [Separator] private Protocol m_SendProtocol = Protocol.Tcp;
        // #endregion
        JsonSerializerSettings m_JSS = new JsonSerializerSettings() { ObjectCreationHandling = ObjectCreationHandling.Replace };

        public override void OnNeutronStart()
        {
            base.OnNeutronStart();
            if (HasAuthority)
                GetSynchronizedFields();
        }

        private void GetSynchronizedFields()
        {
            var l_Fields = this.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance); //* Pega todos os campos da classe derivada.
            if (l_Fields != null)
            {
                foreach (var l_Field in l_Fields) //* percore a parada
                {
                    if (l_Field.GetCustomAttribute<SyncAttribute>() != null) //* verifica se está marcado com o atributo Sync.
                        if (l_Field.IsPublic) //* Verifica se o campo é publico, para ser possível a serialização via JSON.
                        {
                            if (!l_Field.IsNotSerialized) //* Verifica se o campo é serializabel para evitar problemas com classes Internas da Unity, ex: Vector3.
                                listOfFields.Add(l_Field); //* Tudo certo?, adiciona este campo na lista.
                            else Debug.LogError($"NonSerialized field: [{l_Field.Name}] cannot be synchronized.");
                        }
                        else
                            Debug.LogError($"Private field: [{l_Field.Name}] cannot be synchronized.");
                    else continue;
                }
                StartCoroutine(FieldsToJson());
            }
        }

        private IEnumerator FieldsToJson()
        {
            while (true)
            {
                var ToJsonDict = listOfFields.ToDictionary(x => x.Name, y => y.GetValue(this)); //* Transforma a lista em dict para serializar via rede.
                if (ToJsonDict != null)
                {
                    var cJson = JsonConvert.SerializeObject(ToJsonDict);
                    if (!string.IsNullOrEmpty(cJson))
                    {
                        if (oldJson != cJson) //* compara se o valores novos diferem dos antigos, necessário para enviar os dados somente quando os valores mudarem.
                        {
                            Broadcast(cJson); //* envia os novos dados para a rede.
                            {
                                oldJson = cJson; //* Atualiza os valores antigos com os novos, evitando envios desnecessários.
                            }
                        }
                    }
                }
                yield return new WaitForSeconds(m_SynchronizeInterval);
            }
        }

        private void Broadcast(string cJson) //* Metódo de de envio para a rede.
        {
            using (NeutronWriter nWriter = Neutron.PooledNetworkWriters.Pull())
            {
                nWriter.SetLength(0);
                nWriter.Write(cJson);
                iRPC(NeutronConstants.NEUTRON_SYNCHRONIZE_BEHAVIOUR, nWriter, m_CacheMode, m_SendTo, m_BroadcastTo, m_ReceivingProtocol, m_SendingProtocol);
            }
        }

        [iRPC(NeutronConstants.NEUTRON_SYNCHRONIZE_BEHAVIOUR)]
        public void RPC(NeutronReader nOptions, bool nIsMine, Player nSender)
        {
            using (nOptions)
            {
                if (IsClient || Authority != AuthorityMode.Server)
                    JsonConvert.PopulateObject(nOptions.ReadString(), this, m_JSS); //* Recebe os dados novos e deserializa neste objeto que recebeu.
            }
        }
    }
}