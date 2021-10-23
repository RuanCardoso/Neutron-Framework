using NeutronNetwork.Interfaces;
using NeutronNetwork.Internal;
using System;

namespace NeutronNetwork
{
    [Serializable]
    public class NeutronServerMatchmaking : MatchmakingBehaviour, INeutronIdentify
    {
        #region Properties
        /// <summary>
        ///* Retorna o identificador do canal.
        /// </summary>
        public int Id {
            get => _id;
            set => _id = value;
        }
        #endregion
    }
}