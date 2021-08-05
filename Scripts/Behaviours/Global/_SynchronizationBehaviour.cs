using NeutronNetwork.Attributes;
using NeutronNetwork.Json;
using UnityEngine;

/// <summary>
///* Criado por: Ruan Cardoso(Brasil)
///* Os br também são pica.
///* Email: cardoso.ruan050322@gmail.com
///* Licença: GNU AFFERO GENERAL PUBLIC LICENSE
/// </summary>
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
        private readonly JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings()
        {
            ObjectCreationHandling = ObjectCreationHandling.Replace
        };
        #endregion

        public override bool OnAutoSynchronization(NeutronWriter writer, NeutronReader reader, bool isWriting)
        {
            if (isWriting)
            {
                var data = JsonConvert.SerializeObject(this, _jsonSerializerSettings);
                if (data.Length > 2)
                {
                    if (!_serializeOnChange)
                        writer.Write(data);
                    else
                    {
                        if (_json != data)
                        {
                            writer.Write(data);
                            {
                                _json = data;
                            }
                        }
                        else
                            return false;
                    }
                }
                else
                    return false;
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
            else
                return OnValidateProperties();
        }
        /// <summary>
        ///* Usado para validar as propriedades ao lado do servidor.
        /// </summary>
        /// <returns></returns>
        protected virtual bool OnValidateProperties() => true;
    }
}