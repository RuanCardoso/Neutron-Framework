using NeutronNetwork.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NeutronNetwork.Internal
{
    public static class JsonContracts
    {
        static readonly SyncVarContractResolver _contractResolver = new SyncVarContractResolver();
        public static readonly JsonSerializer JsonSerializer = new JsonSerializer()
        {
            ObjectCreationHandling = ObjectCreationHandling.Replace, //* Replace the object if it already exists.
            ContractResolver = _contractResolver, //* Use the SyncVarResolver to serialize the SyncVars.
            Formatting = Formatting.Indented,

        };

        public static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings()
        {
            ObjectCreationHandling = ObjectCreationHandling.Replace, //* Replace the object if it already exists.
            ContractResolver = _contractResolver, //* Use the SyncVarResolver to serialize the SyncVars.
            Formatting = Formatting.Indented,
        };

        public static readonly JsonLoadSettings jsonLoadSettings = new JsonLoadSettings()
        {

        };
    }

    public class SyncVarContractResolver : DefaultContractResolver
    {
        protected override List<MemberInfo> GetSerializableMembers(Type objectType)
        {
            //* Get all the fields and properties with SyncVarAttribute.
            return ReflectionHelper.GetMembers(objectType)
                .Where(mi => mi.GetCustomAttribute<SyncVarAttribute>() != null).ToList();  //* Return the fields and properties with the SyncVarAttr.
        }
    }
}