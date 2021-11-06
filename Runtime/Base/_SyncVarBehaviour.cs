using NeutronNetwork.Helpers;
using NeutronNetwork.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
        /// <summary>
        ///* Indicates if this objects is ready to send.
        /// </summary>
        private bool _isReady = true;
        /// <summary>
        ///* Store the old json value to compare if the value changed.
        /// </summary>
        /// <value></value>
        private string _oldSerializedJson = "{\"\":\"\"}";
        /// <summary>
        ///* Store the fields and properties that will be serialized over the network.
        /// </summary>
        /// <returns></returns>
        private readonly JObject _memberInfos = new JObject();
        /// <summary>
        ///* Store the fields to read are values.
        /// </summary>
        private (SyncVarAttribute, FieldInfo)[] _fields;
        /// <summary>
        ///* Store the properties to read are values.
        /// </summary>
        private (SyncVarAttribute, PropertyInfo)[] _properties;
        /// <summary>
        ///* Store the all hooks...
        /// </summary>
        private readonly Dictionary<string, MethodInfo> _hooks = new Dictionary<string, MethodInfo>();

        protected virtual void Start()
        {
            var method = ReflectionHelper.GetMethod("OnAutoSynchronization", this); //* Get the virtual auto-sync method.
            if (method.DeclaringType != typeof(SyncVarBehaviour))
                _isReady = LogHelper.Error($"The Method \"{nameof(OnAutoSynchronization)}\" cannot be overridden because it is being used by base class \"{nameof(SyncVarBehaviour)}\"");
            else
            {
                _fields = ReflectionHelper.GetAttributesWithField<SyncVarAttribute>(this); //* Get all the fields with SyncVarAttribute.
                _properties = ReflectionHelper.GetAttributesWithProperty<SyncVarAttribute>(this); //* Get all the properties with SyncVarAttribute.
                MakeHooks();
                SetTokens();
                _oldSerializedJson = _memberInfos.ToString(); //* Store the old json value.
            }
        }

        private void SetTokens()
        {
            foreach (var field in _fields)
            {
                FieldInfo fieldInfo = field.Item2;
                if (fieldInfo.IsPrivate)
                    LogHelper.Warn($"The field {fieldInfo.Name} it will not be serialized because it is private.");
                else
                    SetToken(fieldInfo.Name, fieldInfo.GetValue(this)); //* Set the token to the field.
            }

            foreach (var property in _properties)
            {
                PropertyInfo propertyInfo = property.Item2;
                SetToken(propertyInfo.Name, propertyInfo.GetValue(this)); //* Set the token to the property.
            }
        }

        private void MakeHooks()
        {
            void Hook(string hook, string name)
            {
                if (!string.IsNullOrEmpty(hook))
                {
                    if (!_hooks.ContainsKey(name))
                    {
                        MethodInfo info = ReflectionHelper.GetMethod(hook, this);
                        if (info != null)
                            _hooks.Add(name, info);
                        else
                            LogHelper.Error($"The method {hook} does not exist!");
                    }
                }
            }

            foreach (var field in _fields)
            {
                SyncVarAttribute attr = field.Item1;
                FieldInfo fieldInfo = field.Item2;
                Hook(attr.Hook, fieldInfo.Name);
            }

            foreach (var property in _properties)
            {
                SyncVarAttribute attr = property.Item1;
                PropertyInfo propertyInfo = property.Item2;
                Hook(attr.Hook, propertyInfo.Name);
            }
        }

        private void SetToken(string name, object value)
        {
            if (value == null)
                throw new Exception($"SyncVar: {name} is null!");
            _memberInfos[name] = JToken.FromObject(value, JsonContracts.JsonSerializer); //* Set the token to the value.
        }

        private readonly JObject _diffMemberInfos = new JObject(); //* Store the difference between the old and new json.
        public override bool OnAutoSynchronization(NeutronStream stream, bool isMine)
        {
            var writer = stream.Writer; //* Get the writer.
            var reader = stream.Reader; //* Get the reader.
            if (!_isReady)
                return false;
            if (isMine)
            {
                SetTokens(); //* Set the tokens to the fields and properties.
                _diffMemberInfos.RemoveAll(); //* Remove all the fields and properties.
                JToken oldToken = JToken.Parse(_oldSerializedJson); //* Parse the old json value.
                JToken currentToken = JToken.Parse(_memberInfos.ToString(Formatting.Indented)); //* Parse the current json value.
                if (!JToken.DeepEquals(oldToken, currentToken))
                {
                    //* If the old and current json values are different, send the current json value.
                    var tokens = currentToken.Except(oldToken, JToken.EqualityComparer).ToArray(); //* Get the difference between the old and current json values.
                    if (tokens.Length > 0)
                    {
                        //* If there is a difference, send the difference.
                        foreach (var token in tokens)
                            _diffMemberInfos.Add(token); //* Add the token to the JObject.
                        writer.Write(_diffMemberInfos.ToString(Formatting.Indented)); //* Send the difference.
                        writer.Write(); //* Send the end of the message.
                        _oldSerializedJson = currentToken.ToString(Formatting.Indented); //* Store the current json value as the old json value.
                    }
                    else
                        return false; //* If there is no difference, return false.
                }
                else
                    return false; //* If the old and current json values are equal, return false.
            }
            else if (DoNotPerformTheOperationOnTheServer)
            {
                string json = reader.ReadString();
                if (_hooks.Count > 0)
                {
                    JObject keyValuePairs = JObject.Parse(json);
                    foreach (var pair in keyValuePairs)
                    {
                        try
                        {
                            if (_hooks.TryGetValue(pair.Key, out MethodInfo info))
                            {
                                LogHelper.Error(pair.Value.Type);
                                Type parameterType = info.GetParameters()[0].ParameterType;
                                string jsonValue = pair.Value.ToString(Formatting.Indented);
                                object jsonOject = JsonConvert.DeserializeObject(jsonValue, parameterType, JsonContracts.JsonSerializerSettings);
                                info.Invoke(this, new object[] { jsonOject });
                            }
                        }
                        catch (Exception ex)
                        {
                            LogHelper.Stacktrace(ex);
                        }
                    }
                }
                JsonConvert.PopulateObject(json, this, JsonContracts.JsonSerializerSettings); //* Read the json value and populate the object.
            }
            return OnValidateAutoSynchronization(isMine); //* Return the result of OnValidateAutoSynchronization.
        }

        protected override bool OnValidateAutoSynchronization(bool isMine) => isMine || OnValidateProperties();

        /// <summary>
        ///* Used to validate the properties if the client has authority.
        /// </summary>
        /// <returns></returns>
        protected virtual bool OnValidateProperties() => true;
    }

    public class SyncVarMethodHook
    {
        public MethodInfo MethodInfo { get; }
        public Type ParameterType { get; }
        //public Action<>

        public SyncVarMethodHook(MethodInfo methodInfo, Type parameterType)
        {
            MethodInfo = methodInfo;
            ParameterType = parameterType;
        }
    }
}