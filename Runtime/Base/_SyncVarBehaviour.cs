using NeutronNetwork.Attributes;
using NeutronNetwork.Helpers;
using NeutronNetwork.Internal;
using NeutronNetwork.Naughty.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

//* Created by: Ruan Cardoso(Brasil)
//* Email: neutron050322@gmail.com
//* License: MIT
namespace NeutronNetwork
{
    /// <summary>
    ///* SyncVarBehaviour is a base class for any class that uses SyncVars attributes.
    ///* SyncVar supports any type, but not all types are internally optimized.
    /// </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public class SyncVarBehaviour : NeutronBehaviour
    {
        private bool _hasHooks;
        /// <summary>
        ///* Store the old json value to compare if the value changed.
        /// </summary>
        /// <value></value>
        [SerializeField] [Label("Debug Json")] [ReadOnly] [ResizableTextArea] private string _oldSerializedJson = "{\"\":\"\"}";
#pragma warning disable IDE0044
        [SerializeField] [HorizontalLineDown] [ReadOnly] private bool _sendOnlyDiff = true;
#pragma warning restore IDE0044
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
        ///* Stores the types of fields marked with the SyncVar attribute.
        /// </summary>
        private readonly Dictionary<string, string> _memberTypes = new Dictionary<string, string>();

        #region Dict Hooks
        /// <summary>
        ///* Stores known types for internal optimization, is there a better way to do this? ):, a lot of dictionaries....
        /// </summary>
        private readonly Dictionary<string, SyncVarMethodHook> _hooksUnknowTypes = new Dictionary<string, SyncVarMethodHook>();
        private readonly Dictionary<string, SyncVarMethodHook<int>> _hooksInt32 = new Dictionary<string, SyncVarMethodHook<int>>();
        private readonly Dictionary<string, SyncVarMethodHook<uint>> _hooksUInt32 = new Dictionary<string, SyncVarMethodHook<uint>>();
        private readonly Dictionary<string, SyncVarMethodHook<long>> _hooksInt64 = new Dictionary<string, SyncVarMethodHook<long>>();
        private readonly Dictionary<string, SyncVarMethodHook<ulong>> _hooksUInt64 = new Dictionary<string, SyncVarMethodHook<ulong>>();
        private readonly Dictionary<string, SyncVarMethodHook<float>> _hooksSingle = new Dictionary<string, SyncVarMethodHook<float>>();
        private readonly Dictionary<string, SyncVarMethodHook<double>> _hooksDouble = new Dictionary<string, SyncVarMethodHook<double>>();
        private readonly Dictionary<string, SyncVarMethodHook<bool>> _hooksBoolean = new Dictionary<string, SyncVarMethodHook<bool>>();
        private readonly Dictionary<string, SyncVarMethodHook<byte>> _hooksByte = new Dictionary<string, SyncVarMethodHook<byte>>();
        private readonly Dictionary<string, SyncVarMethodHook<sbyte>> _hooksSByte = new Dictionary<string, SyncVarMethodHook<sbyte>>();
        private readonly Dictionary<string, SyncVarMethodHook<char>> _hooksChar = new Dictionary<string, SyncVarMethodHook<char>>();
        private readonly Dictionary<string, SyncVarMethodHook<decimal>> _hooksDecimal = new Dictionary<string, SyncVarMethodHook<decimal>>();
        private readonly Dictionary<string, SyncVarMethodHook<string>> _hooksString = new Dictionary<string, SyncVarMethodHook<string>>();
        private readonly Dictionary<string, SyncVarMethodHook<short>> _hooksInt16 = new Dictionary<string, SyncVarMethodHook<short>>();
        private readonly Dictionary<string, SyncVarMethodHook<ushort>> _hooksUInt16 = new Dictionary<string, SyncVarMethodHook<ushort>>();
        #endregion

        protected virtual void Start()
        {
            var method = ReflectionHelper.GetMethod("OnAutoSynchronization", this); //* Get the virtual auto-sync method.
            if (method.DeclaringType != typeof(SyncVarBehaviour))
                throw new NeutronException($"The Method \"{nameof(OnAutoSynchronization)}\" cannot be overridden because it is being used by base class \"{nameof(SyncVarBehaviour)}\"");
            else
            {
                _fields = ReflectionHelper.GetAttributesWithField<SyncVarAttribute>(this); //* Get all the fields with SyncVarAttribute.
                _properties = ReflectionHelper.GetAttributesWithProperty<SyncVarAttribute>(this); //* Get all the properties with SyncVarAttribute.
                //**************************************************************************
                MakeHooks(); //* Get all methods assigned to syncvar
                SetTokens(); //* Gets all fields and properties and creates a JsonToken.
                //**************************************************************************
                _oldSerializedJson = _memberInfos.ToString(); //* Store the old json value.
            }

            _hasHooks = _hooksUnknowTypes.Count > 0 || _hooksInt32.Count > 0 || _hooksInt64.Count > 0
                || _hooksSingle.Count > 0 || _hooksDouble.Count > 0 || _hooksBoolean.Count > 0 || _hooksByte.Count > 0
                || _hooksSByte.Count > 0 || _hooksChar.Count > 0 || _hooksDecimal.Count > 0 || _hooksString.Count > 0
                || _hooksUInt32.Count > 0 || _hooksUInt64.Count > 0 || _hooksInt16.Count > 0 || _hooksUInt16.Count > 0;
        }

        private void SetTokens()
        {
            foreach (var field in _fields)
            {
                FieldInfo fieldInfo = field.Item2;
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
            void Hook(string hook, string name, string fieldType)
            {
                _memberTypes[name] = fieldType;
                if (!string.IsNullOrEmpty(hook))
                {
                    MethodInfo info = ReflectionHelper.GetMethod(hook, this);
                    if (info != null)
                    {
                        ParameterInfo[] parameterInfos = info.GetParameters();
                        if (parameterInfos.Length == 0)
                            throw new NeutronException("SyncVar: Did you forget to add the default parameter?");
                        else
                        {
                            string type = parameterInfos[0].ParameterType.Name;
                            switch (type)
                            {
                                case nameof(Int32):
                                    {
                                        if (!_hooksInt32.ContainsKey(name))
                                            _hooksInt32.Add(name, new SyncVarMethodHook<int>(info, this));
                                    }
                                    break;
                                case nameof(UInt32):
                                    {
                                        if (!_hooksUInt32.ContainsKey(name))
                                            _hooksUInt32.Add(name, new SyncVarMethodHook<uint>(info, this));
                                    }
                                    break;
                                case nameof(Int16):
                                    {
                                        if (!_hooksInt16.ContainsKey(name))
                                            _hooksInt16.Add(name, new SyncVarMethodHook<short>(info, this));
                                    }
                                    break;
                                case nameof(UInt16):
                                    {
                                        if (!_hooksUInt16.ContainsKey(name))
                                            _hooksUInt16.Add(name, new SyncVarMethodHook<ushort>(info, this));
                                    }
                                    break;
                                case nameof(Int64):
                                    {
                                        if (!_hooksInt64.ContainsKey(name))
                                            _hooksInt64.Add(name, new SyncVarMethodHook<long>(info, this));
                                    }
                                    break;
                                case nameof(UInt64):
                                    {
                                        if (!_hooksUInt64.ContainsKey(name))
                                            _hooksUInt64.Add(name, new SyncVarMethodHook<ulong>(info, this));
                                    }
                                    break;
                                case nameof(Single):
                                    {
                                        if (!_hooksSingle.ContainsKey(name))
                                            _hooksSingle.Add(name, new SyncVarMethodHook<float>(info, this));
                                    }
                                    break;
                                case nameof(Double):
                                    {
                                        if (!_hooksDouble.ContainsKey(name))
                                            _hooksDouble.Add(name, new SyncVarMethodHook<double>(info, this));
                                    }
                                    break;
                                case nameof(Boolean):
                                    {
                                        if (!_hooksBoolean.ContainsKey(name))
                                            _hooksBoolean.Add(name, new SyncVarMethodHook<bool>(info, this));
                                    }
                                    break;
                                case nameof(Byte):
                                    {
                                        if (!_hooksByte.ContainsKey(name))
                                            _hooksByte.Add(name, new SyncVarMethodHook<byte>(info, this));
                                    }
                                    break;
                                case nameof(SByte):
                                    {
                                        if (!_hooksSByte.ContainsKey(name))
                                            _hooksSByte.Add(name, new SyncVarMethodHook<sbyte>(info, this));
                                    }
                                    break;
                                case nameof(Char):
                                    {
                                        if (!_hooksChar.ContainsKey(name))
                                            _hooksChar.Add(name, new SyncVarMethodHook<char>(info, this));
                                    }
                                    break;
                                case nameof(Decimal):
                                    {
                                        if (!_hooksDecimal.ContainsKey(name))
                                            _hooksDecimal.Add(name, new SyncVarMethodHook<decimal>(info, this));
                                    }
                                    break;
                                case nameof(String):
                                    {
                                        if (!_hooksString.ContainsKey(name))
                                            _hooksString.Add(name, new SyncVarMethodHook<string>(info, this));
                                    }
                                    break;
                                default:
                                    {
                                        if (!_hooksUnknowTypes.ContainsKey(name))
                                            _hooksUnknowTypes.Add(name, new SyncVarMethodHook(info));
                                    }
                                    break;
                            }
                        }
                    }
                    else
                        throw new NeutronException($"The method {hook} does not exist!");
                }
            }

            foreach (var field in _fields)
            {
                SyncVarAttribute attr = field.Item1;
                FieldInfo fieldInfo = field.Item2;
                if (fieldInfo.IsPrivate)
                    LogHelper.Warn($"The field {fieldInfo.Name} it will not be serialized because it is private.");
                Hook(attr.Hook, fieldInfo.Name, fieldInfo.FieldType.Name);
            }

            foreach (var property in _properties)
            {
                SyncVarAttribute attr = property.Item1;
                PropertyInfo propertyInfo = property.Item2;
                Hook(attr.Hook, propertyInfo.Name, propertyInfo.PropertyType.Name);
            }
        }

        private void SetToken(string name, object value)
        {
            if (value == null)
                throw new NeutronException($"SyncVar: {name} is null!");
            else
                _memberInfos[name] = JToken.FromObject(value, JsonContracts.JsonSerializer); //* Set the token to the value.
        }

        private void CallHook<T>(JToken token, SyncVarMethodHook<T> syncVarMethodHook)
        {
            T value = token.Value<T>();
            syncVarMethodHook.Invoke(value);
        }

        private readonly JObject _diffMemberInfos = new JObject(); //* Store the difference between the old and new json.
        public override bool OnAutoSynchronization(NeutronStream stream, bool isMine)
        {
            var writer = stream.Writer; //* Get the writer.
            var reader = stream.Reader; //* Get the reader.
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
                _oldSerializedJson = json;
                if (_hasHooks)
                {
                    JObject keyValuePairs = JObject.Parse(json);
                    foreach (var pair in keyValuePairs)
                    {
                        try
                        {
                            if (_memberTypes.TryGetValue(pair.Key, out var type))
                            {
                                switch (type)
                                {
                                    case nameof(Int32):
                                        {
                                            if (_hooksInt32.TryGetValue(pair.Key, out SyncVarMethodHook<int> syncVarMethodHook))
                                                CallHook(pair.Value, syncVarMethodHook);
                                        }
                                        break;
                                    case nameof(UInt32):
                                        {
                                            if (_hooksUInt32.TryGetValue(pair.Key, out SyncVarMethodHook<uint> syncVarMethodHook))
                                                CallHook(pair.Value, syncVarMethodHook);
                                        }
                                        break;
                                    case nameof(Int16):
                                        {
                                            if (_hooksInt16.TryGetValue(pair.Key, out SyncVarMethodHook<short> syncVarMethodHook))
                                                CallHook(pair.Value, syncVarMethodHook);
                                        }
                                        break;
                                    case nameof(UInt16):
                                        {
                                            if (_hooksUInt16.TryGetValue(pair.Key, out SyncVarMethodHook<ushort> syncVarMethodHook))
                                                CallHook(pair.Value, syncVarMethodHook);
                                        }
                                        break;
                                    case nameof(Int64):
                                        {
                                            if (_hooksInt64.TryGetValue(pair.Key, out SyncVarMethodHook<long> syncVarMethodHook))
                                                CallHook(pair.Value, syncVarMethodHook);
                                        }
                                        break;
                                    case nameof(UInt64):
                                        {
                                            if (_hooksUInt64.TryGetValue(pair.Key, out SyncVarMethodHook<ulong> syncVarMethodHook))
                                                CallHook(pair.Value, syncVarMethodHook);
                                        }
                                        break;
                                    case nameof(Single):
                                        {
                                            if (_hooksSingle.TryGetValue(pair.Key, out SyncVarMethodHook<float> syncVarMethodHook))
                                                CallHook(pair.Value, syncVarMethodHook);
                                        }
                                        break;
                                    case nameof(Double):
                                        {
                                            if (_hooksDouble.TryGetValue(pair.Key, out SyncVarMethodHook<double> syncVarMethodHook))
                                                CallHook(pair.Value, syncVarMethodHook);
                                        }
                                        break;
                                    case nameof(Boolean):
                                        {
                                            if (_hooksBoolean.TryGetValue(pair.Key, out SyncVarMethodHook<bool> syncVarMethodHook))
                                                CallHook(pair.Value, syncVarMethodHook);
                                        }
                                        break;
                                    case nameof(Byte):
                                        {
                                            if (_hooksByte.TryGetValue(pair.Key, out SyncVarMethodHook<byte> syncVarMethodHook))
                                                CallHook(pair.Value, syncVarMethodHook);
                                        }
                                        break;
                                    case nameof(SByte):
                                        {
                                            if (_hooksSByte.TryGetValue(pair.Key, out SyncVarMethodHook<sbyte> syncVarMethodHook))
                                                CallHook(pair.Value, syncVarMethodHook);
                                        }
                                        break;
                                    case nameof(Char):
                                        {
                                            if (_hooksChar.TryGetValue(pair.Key, out SyncVarMethodHook<char> syncVarMethodHook))
                                                CallHook(pair.Value, syncVarMethodHook);
                                        }
                                        break;
                                    case nameof(Decimal):
                                        {
                                            if (_hooksDecimal.TryGetValue(pair.Key, out SyncVarMethodHook<decimal> syncVarMethodHook))
                                                CallHook(pair.Value, syncVarMethodHook);
                                        }
                                        break;
                                    case nameof(String):
                                        {
                                            if (_hooksString.TryGetValue(pair.Key, out SyncVarMethodHook<string> syncVarMethodHook))
                                                CallHook(pair.Value, syncVarMethodHook);
                                        }
                                        break;
                                    default:
                                        {
                                            if (_hooksUnknowTypes.TryGetValue(pair.Key, out SyncVarMethodHook syncVarMethodHook))
                                            {
                                                MethodInfo methodInfo = syncVarMethodHook.MethodInfo;
                                                object jsonOject = pair.Value.ToObject(syncVarMethodHook.ParameterType);
                                                methodInfo.Invoke(this, new object[] { jsonOject });
                                            }
                                        }
                                        break;
                                }
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

        public SyncVarMethodHook(MethodInfo methodInfo)
        {
            MethodInfo = methodInfo;
            ParameterType = methodInfo.GetParameters()[0].ParameterType;
        }
    }

    public class SyncVarMethodHook<T>
    {
        public Action<T> Invoke { get; }

        public SyncVarMethodHook(MethodInfo methodInfo, object target)
        {
            Invoke = (Action<T>)methodInfo.CreateDelegate(typeof(Action<T>), target);
        }
    }
}