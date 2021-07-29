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

        public NeutronRoom(int id, string roomName, int maxPlayers, bool hasPassword, bool isVisible, string options)
        {
            ID = id;
            Name = roomName;
            MaxPlayers = maxPlayers;
            HasPassword = hasPassword;
            IsVisible = isVisible;
            Properties = options;
        }

        public NeutronRoom(SerializationInfo info, StreamingContext context)
        {
            ID = info.GetInt32("ID");
            Name = info.GetString("NM");
            PlayerCount = info.GetInt32("CP");
            MaxPlayers = info.GetInt32("MP");
            HasPassword = info.GetBoolean("HP");
            IsVisible = info.GetBoolean("IV");
            Properties = info.GetString("_");
            //////////////////////////////////////// Instantiate ////////////////////////////////////
            Get = JsonConvert.DeserializeObject<Dictionary<string, object>>(Properties);
            SceneView = new SceneView();
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("ID", ID);
            info.AddValue("NM", Name);
            info.AddValue("CP", PlayerCount);
            info.AddValue("MP", MaxPlayers);
            info.AddValue("HP", HasPassword);
            info.AddValue("IV", IsVisible);
            info.AddValue("_", Properties);
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