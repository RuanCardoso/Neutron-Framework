using NeutronNetwork.Interfaces;
using NeutronNetwork.Internal;
using NeutronNetwork.Internal.Interfaces;
using NeutronNetwork.Json;
using NeutronNetwork.Naughty.Attributes;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

namespace NeutronNetwork
{
    [Serializable]
    public class NeutronRoom : MatchmakingBehaviour, INeutronSerializable, INeutron, IEquatable<NeutronRoom>, IEqualityComparer<NeutronRoom>
    {
        #region Fields
        [SerializeField] [HorizontalLine] private bool _hasPassword;
        [SerializeField] [AllowNesting] [ShowIf("_hasPassword")] private string _password;
        [SerializeField] private bool _isVisible;
        #endregion

        #region Properties
        /// <summary>
        ///* Retorna o identificador da sala.
        /// </summary>
        public int ID { get => _id; set => _id = value; }
        /// <summary>
        ///* Retorna se a sala é protegida por senha.
        /// </summary>
        public bool HasPassword { get => _hasPassword; set => _hasPassword = value; }
        /// <summary>
        ///* Retorna se a sala está visível para outros jogadores.
        /// </summary>
        public bool IsVisible { get => _isVisible; set => _isVisible = value; }
        /// <summary>
        ///* Define a senha da sala, disponível somente ao lado do servidor. 
        /// </summary>
        public string Password { get => _password; set => _password = value; }
        #endregion

        public NeutronRoom() { } //* the default constructor is important for deserialization and serialization.(only if you implement the ISerializable interface or JSON.Net).

        public NeutronRoom(int id, string name, int maxPlayers, bool hasPassword, bool isVisible, string properties) : base(name, maxPlayers, properties)
        {
            ID = id;
            HasPassword = hasPassword;
            IsVisible = isVisible;
        }

        public NeutronRoom(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            ID = info.GetInt32("id");
            HasPassword = info.GetBoolean("hasPassword");
            IsVisible = info.GetBoolean("isVisible");
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            {
                info.AddValue("id", ID);
                info.AddValue("hasPassword", HasPassword);
                info.AddValue("isVisible", IsVisible);
            }
        }

        public Boolean Equals(NeutronRoom room)
        {
            return this.ID == room.ID;
        }

        public Boolean Equals(NeutronRoom x, NeutronRoom y)
        {
            if (object.ReferenceEquals(x, y))
            {
                return true;
            }
            if (object.ReferenceEquals(x, null) ||
                object.ReferenceEquals(y, null))
            {
                return false;
            }
            return x.ID == y.ID;
        }

        public Int32 GetHashCode(NeutronRoom obj)
        {
            return obj.ID.GetHashCode();
        }
    }
}