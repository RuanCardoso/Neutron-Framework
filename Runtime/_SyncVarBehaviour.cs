using NeutronNetwork.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

/// <summary>
///* Criado por: Ruan Cardoso(Brasil)
///* Os br também são pica.
///* Email: cardoso.ruan050322@gmail.com
///* Licença: GNU AFFERO GENERAL PUBLIC LICENSE
/// </summary>
namespace NeutronNetwork
{
    /// <summary>
    ///* Herde para serializar um campo via rede com os attributos: [Sync].<br/>
    /// </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public class SyncVarBehaviour : NeutronBehaviour
    {
        #region Fields
        //*Armazena o json anterior para comparar com o novo.
        private string _oldJson = "{\"\":\"\"}";
        //* Armazena os campos e propriedades que serão enviados via rede.
        private readonly JObject _fieldsAndProperties = new JObject();
        //* Armazena os campos com o atributo syncvar.
        private (SyncVarAttribute, FieldInfo)[] _fields;
        //* Armazena as propriedades com o atributo syncvar.
        private (SyncVarAttribute, PropertyInfo)[] _properties;
        /// <summary>
        ///* Configurações de serialização e deserialização.
        /// </summary>
        protected readonly JsonSerializer JsonSerializer = new JsonSerializer()
        {
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            ContractResolver = new SyncVarResolver(),

        };
        /// <summary>
        ///* Configurações de serialização e deserialização.
        /// </summary>
        protected readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings()
        {
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            ContractResolver = new SyncVarResolver(),
        };
        #endregion

        protected virtual void Start()
        {
            _fields = ReflectionHelper.GetAttributesWithField<SyncVarAttribute>(this);
            _properties = ReflectionHelper.GetAttributesWithProperty<SyncVarAttribute>(this);
            SetTokens();
            _oldJson = _fieldsAndProperties.ToString();
        }

        private void SetTokens()
        {
            foreach (var field in _fields)
                SetToken(field.Item2.Name, field.Item2.GetValue(this));
            foreach (var property in _properties)
                SetToken(property.Item2.Name, property.Item2.GetValue(this));
        }

        private void SetToken(string name, object value)
        {
            if (value == null)
                throw new Exception($"SyncVar: {name} is null!");
            _fieldsAndProperties[name] = JToken.FromObject(value, JsonSerializer);
        }

        public override bool OnAutoSynchronization(NeutronStream stream, bool isMine)
        {
            var writer = stream.Writer;
            var reader = stream.Reader;
            if (isMine)
            {
                SetTokens();
                JObject fieldsAndProperties = new JObject();
                JToken oldToken = JToken.Parse(_oldJson);
                JToken currentToken = JToken.Parse(_fieldsAndProperties.ToString());
                if (!JToken.DeepEquals(oldToken, currentToken))
                {
                    var tokens = currentToken.Except(oldToken, JToken.EqualityComparer).ToArray();
                    if (tokens.Length > 0)
                    {
                        foreach (var token in tokens)
                            fieldsAndProperties.Add(token);
                        writer.Write(fieldsAndProperties.ToString());
                        writer.Write();
                        _oldJson = currentToken.ToString();
                    }
                    else
                        return false;
                }
                else
                    return false;
            }
            else if (DoNotPerformTheOperationOnTheServer)
                JsonConvert.PopulateObject(reader.ReadString(), this, JsonSerializerSettings);
            return OnValidateAutoSynchronization(isMine);
        }

        protected override bool OnValidateAutoSynchronization(bool isMine) => isMine || OnValidateProperties();

        /// <summary>
        ///* Usado para validar as propriedades ao lado do servidor, disponível somente se a autoridade é do cliente.
        /// </summary>
        /// <returns></returns>
        protected virtual bool OnValidateProperties() => true;
    }

    public class SyncVarResolver : DefaultContractResolver
    {
        protected override List<MemberInfo> GetSerializableMembers(Type objectType)
        {
            return base.GetSerializableMembers(objectType)
              .Where(mi => mi.GetCustomAttribute<SyncVarAttribute>() != null)
              .ToList();
        }
    }
}