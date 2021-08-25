using NeutronNetwork.Attributes;
using Newtonsoft.Json;
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

        public override bool OnAutoSynchronization(NeutronStream stream, NeutronReader reader, bool isWriting)
        {
            if (isWriting)
            {
                var data = JsonConvert.SerializeObject(this, _jsonSerializerSettings);
                if (data.Length > 2)
                {
                    if (!_serializeOnChange)
                    {
                        stream.Writer.Write(data);
                        if (!stream.IsFixedSize)
                            stream.Writer.EndWrite();
                        else
                            stream.Writer.EndWriteWithFixedCapacity();
                    }
                    else
                    {
                        if (_json != data)
                        {
                            stream.Writer.Write(data);
                            if (!stream.IsFixedSize)
                                stream.Writer.EndWrite();
                            else
                                stream.Writer.EndWriteWithFixedCapacity();
                            _json = data;
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

        protected override bool OnValidateAutoSynchronization(bool isMine) => isMine || OnValidateProperties();
        /// <summary>
        ///* Usado para validar as propriedades ao lado do servidor, disponível somente se a autoridade é do cliente.
        /// </summary>
        /// <returns></returns>
        protected virtual bool OnValidateProperties() => true;
    }
}