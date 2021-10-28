using NeutronNetwork.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

//* Created by: Ruan Cardoso(Brasil)
//* Email: neutron050322@gmail.com
//* License: MIT
namespace NeutronNetwork
{
    /// <summary>
    ///* SyncVarBehaviour is a base class for any class that uses SyncVars attributes.
    /// </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public class SyncVarBehaviour : NeutronBehaviour
    {
        #region Fields
        /// <summary>
        ///* Store the old json value to compare if the value changed.
        /// </summary>
        /// <value></value>
        private string _oldJson = "{\"\":\"\"}";
        /// <summary>
        ///* Store the fields and properties that will be serialized over the network.
        /// </summary>
        /// <returns></returns>
        private readonly JObject _fieldsAndProperties = new JObject();
        /// <summary>
        ///* Store the fields to read are values.
        /// </summary>
        private (SyncVarAttribute, FieldInfo)[] _fields;
        /// <summary>
        ///* Store the properties to read are values.
        /// </summary>
        private (SyncVarAttribute, PropertyInfo)[] _properties;
        protected readonly JsonSerializer JsonSerializer = new JsonSerializer()
        {
            ObjectCreationHandling = ObjectCreationHandling.Replace, //* Replace the object if it already exists.
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore, //* Ignore the reference loop.
            ContractResolver = new SyncVarContractResolver(), //* Use the SyncVarResolver to serialize the SyncVars.

        };
        protected readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings()
        {
            ObjectCreationHandling = ObjectCreationHandling.Replace, //* Replace the object if it already exists.
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore, //* Ignore the reference loop.
            ContractResolver = new SyncVarContractResolver(), //* Use the SyncVarResolver to serialize the SyncVars.
        };
        #endregion

        protected virtual void Start()
        {
            _fields = ReflectionHelper.GetAttributesWithField<SyncVarAttribute>(this); //* Get all the fields with SyncVarAttribute.
            _properties = ReflectionHelper.GetAttributesWithProperty<SyncVarAttribute>(this); //* Get all the properties with SyncVarAttribute.
            SetTokens(); //* Set the tokens to the fields and properties.
            _oldJson = _fieldsAndProperties.ToString(); //* Store the old json value.
        }

        private void SetTokens()
        {
            foreach (var field in _fields)
                SetToken(field.Item2.Name, field.Item2.GetValue(this)); //* Set the token to the field.
            foreach (var property in _properties)
                SetToken(property.Item2.Name, property.Item2.GetValue(this)); //* Set the token to the property.
        }

        private void SetToken(string name, object value)
        {
            if (value == null)
                throw new Exception($"SyncVar: {name} is null!");
            _fieldsAndProperties[name] = JToken.FromObject(value, JsonSerializer); //* Set the token to the value.
        }

        JObject diffFieldsAndProperties = new JObject(); //* Store the difference between the old and new json.
        public override bool OnAutoSynchronization(NeutronStream stream, bool isMine)
        {
            var writer = stream.Writer; //* Get the writer.
            var reader = stream.Reader; //* Get the reader.
            if (isMine)
            {
                SetTokens(); //* Set the tokens to the fields and properties.
                diffFieldsAndProperties.RemoveAll(); //* Remove all the fields and properties.
                JToken oldToken = JToken.Parse(_oldJson); //* Parse the old json value.
                JToken currentToken = JToken.Parse(_fieldsAndProperties.ToString()); //* Parse the current json value.
                if (!JToken.DeepEquals(oldToken, currentToken))
                {
                    //* If the old and current json values are different, send the current json value.
                    var tokens = currentToken.Except(oldToken, JToken.EqualityComparer).ToArray(); //* Get the difference between the old and current json values.
                    if (tokens.Length > 0)
                    {
                        //* If there is a difference, send the difference.
                        foreach (var token in tokens)
                            diffFieldsAndProperties.Add(token); //* Add the token to the JObject.
                        writer.Write(diffFieldsAndProperties.ToString()); //* Send the difference.
                        writer.Write(); //* Send the end of the message.
                        _oldJson = currentToken.ToString(); //* Store the current json value as the old json value.
                    }
                    else
                        return false; //* If there is no difference, return false.
                }
                else
                    return false; //* If the old and current json values are equal, return false.
            }
            else if (DoNotPerformTheOperationOnTheServer)
                JsonConvert.PopulateObject(reader.ReadString(), this, JsonSerializerSettings); //* Read the json value and populate the object.
            return OnValidateAutoSynchronization(isMine); //* Return the result of OnValidateAutoSynchronization.
        }

        protected override bool OnValidateAutoSynchronization(bool isMine) => isMine || OnValidateProperties();

        /// <summary>
        ///* Used to validate the properties if the client has authority.
        /// </summary>
        /// <returns></returns>
        protected virtual bool OnValidateProperties() => true;
    }

    public class SyncVarContractResolver : DefaultContractResolver
    {
        protected override List<MemberInfo> GetSerializableMembers(Type objectType)
        {
            //* Get all the fields and properties with SyncVarAttribute.
            return base.GetSerializableMembers(objectType)
              .Where(mi => mi.GetCustomAttribute<SyncVarAttribute>() != null)
              .ToList(); //* Return the fields and properties.
        }
    }
}