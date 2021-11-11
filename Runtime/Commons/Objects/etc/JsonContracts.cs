using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace NeutronNetwork.Internal
{
    public static class JsonContracts
    {
        public static JsonSerializer JsonSerializer; //* setted in UnityConvertersConfig....
        public static JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings()
        {
            ObjectCreationHandling = ObjectCreationHandling.Replace, //* Replace the object if it already exists.
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Formatting = Formatting.Indented,
            Converters = { }, //* added in UnityConvertersConfig....
            ContractResolver = _syncVarContractResolver,
        };

        public static readonly JsonLoadSettings JsonLoadSettings = new JsonLoadSettings()
        {

        };

        private static readonly SyncVarContractResolver _syncVarContractResolver = new SyncVarContractResolver();
    }

    public class SyncVarContractResolver : DefaultContractResolver
    {
        protected override List<MemberInfo> GetSerializableMembers(Type objectType)
        {
            return objectType.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                 .Cast<MemberInfo>()
                 .Concat(objectType.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
                 .Where(o => o.GetCustomAttribute<SyncVarAttribute>() != null).ToList();
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty jsonProperty = base.CreateProperty(member, memberSerialization);

            if (member.GetCustomAttribute<SyncVarAttribute>() != null)
            {
                jsonProperty.Ignored = false;
                jsonProperty.Writable = CanWriteMemberWithSerializeField(member);
                jsonProperty.Readable = CanReadMemberWithSerializeField(member);
                jsonProperty.HasMemberAttribute = true;
            }

            return jsonProperty;
        }

        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            IList<JsonProperty> lists = base.CreateProperties(type, memberSerialization);

            return lists;
        }

        protected override JsonObjectContract CreateObjectContract(Type objectType)
        {
            JsonObjectContract jsonObjectContract = base.CreateObjectContract(objectType);

            if (typeof(ScriptableObject).IsAssignableFrom(objectType))
            {
                jsonObjectContract.DefaultCreator = () =>
                {
                    return ScriptableObject.CreateInstance(objectType);
                };
            }

            return jsonObjectContract;
        }

        private static bool CanReadMemberWithSerializeField(MemberInfo member)
        {
            return !(member is PropertyInfo property) || property.CanRead;
        }

        private static bool CanWriteMemberWithSerializeField(MemberInfo member)
        {
            return !(member is PropertyInfo property) || property.CanWrite;
        }
    }
}