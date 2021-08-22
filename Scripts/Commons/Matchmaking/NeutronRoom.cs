using NeutronNetwork.Interfaces;
using NeutronNetwork.Internal;
using NeutronNetwork.Internal.Attributes;
using NeutronNetwork.Internal.Interfaces;
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
        [SerializeField] private string _password = string.Empty;
        #endregion

        #region Properties
        /// <summary>
        ///* Retorna o identificador da sala.
        /// </summary>
        [Network("Serialized")]
        public int ID {
            get => _id;
            set => _id = value;
        }

        /// <summary>
        ///* Retorna se a sala é protegida por senha.
        /// </summary>
        [Network("Serialized")]
        public bool HasPassword {
            get;
        }

        /// <summary>
        ///* Retorna se a sala está visível para outros jogadores.
        /// </summary>
        [Network("Serialized")]
        public bool IsVisible {
            get;
        }

        /// <summary>
        ///* Define a senha da sala, senha armazenada somente ao lado do servidor. 
        /// </summary>
        [Network("Serialized")]
        public string Password {
            get => _password;
            set => _password = value;
        }
        #endregion

        public NeutronRoom() { } //* the default constructor is important for deserialization and serialization.(only if you implement the ISerializable interface or JSON.Net).

        public NeutronRoom(int id, string name, int maxPlayers, string properties) : base(name, maxPlayers, properties)
        {
            ID = id;
            //************************************************
            HasPassword = !string.IsNullOrEmpty(Password);
            IsVisible = !Name.StartsWith(".");
        }

        public NeutronRoom(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            ID = info.GetInt32("id");
            Password = info.GetString("password");
            //************************************************
            HasPassword = !string.IsNullOrEmpty(Password);
            IsVisible = !Name.StartsWith(".");
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            {
                info.AddValue("id", ID);
                info.AddValue("password", !string.IsNullOrEmpty(Password) ? "****" : Password);
            }
        }

        public bool Equals(NeutronRoom room)
        {
            return ID == room.ID;
        }

        public bool Equals(NeutronRoom x, NeutronRoom y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (ReferenceEquals(x, null) || ReferenceEquals(y, null))
                return false;
            return x.ID == y.ID;
        }

        public int GetHashCode(NeutronRoom room)
        {
            return room.ID.GetHashCode();
        }

        public override bool Equals(object room)
        {
            return ID == ((NeutronRoom)room).ID;
        }

        public override int GetHashCode()
        {
            return ID.GetHashCode();
        }

        public override string ToString()
        {
            return $"\n\rId: {ID}\n\rHasPassword: {HasPassword}\n\rIsVisible: {IsVisible}\n\rName: {Name}\r\nPlayerCount: {PlayerCount}\r\nMaxPlayers: {MaxPlayers}\r\nProperties: {Properties}\r\nPassword: {Password}";
        }
    }
}