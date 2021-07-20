using NeutronNetwork.Attributes;
using NeutronNetwork.Json;
using UnityEngine;

namespace NeutronNetwork
{
    [JsonObject(MemberSerialization.OptIn)]
    public class SynchronizationBehaviour : NeutronBehaviour
    {
        #region Fields -> Inspector
        [SerializeField] [HorizontalLineDown] private bool _serializeOnChange = true;
        #endregion

        #region Fields
        private string _json;
        private JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings()
        {
            ObjectCreationHandling = ObjectCreationHandling.Replace
        };
        #endregion

        public override bool OnAutoSynchronization(NeutronWriter writer, NeutronReader reader, bool isWriting)
        {
            if (isWriting)
            {
                var json = JsonConvert.SerializeObject(this, _jsonSerializerSettings);
                if (!_serializeOnChange)
                    writer.Write(json);
                else
                {
                    if (_json != json)
                    {
                        writer.Write(json);
                        {
                            _json = json;
                        }
                    }
                    else
                        return false;
                }
            }
            else
            {
                if (DoNotPerformTheOperationOnTheServer)
                    JsonConvert.PopulateObject(reader.ReadString(), this, _jsonSerializerSettings);
            }
            return OnValidateAutoSynchronization(isWriting);
        }

        protected override bool OnValidateAutoSynchronization(bool isWriting)
        {
            if (isWriting)
                return true;
            else return OnValidateProperties();
        }
        /// <summary>
        ///* Usado para validar as propriedades ao lado do servidor.
        /// </summary>
        /// <returns></returns>
        protected virtual bool OnValidateProperties() => true;
    }
}