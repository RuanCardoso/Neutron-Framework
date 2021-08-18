using NeutronNetwork.Internal.Packets;
using NeutronNetwork.Internal.Wrappers;
using System;
using UnityEngine;

/// <summary>
///* Criado por: Ruan Cardoso(Brasil)
///* Os br também são pica.
///* Email: cardoso.ruan050322@gmail.com
///* Licença: GNU AFFERO GENERAL PUBLIC LICENSE
/// </summary>
namespace NeutronNetwork
{
    [Serializable]
    public class SceneView
    {
        #region Fields
        [SerializeField] private GameObject[] _gameObjects;
        [SerializeField] private bool _hasPhysics;
        #endregion

        #region Properties
        public NeutronSafeDictionary<(int, int, RegisterMode), NeutronView> Views { get; set; } = new NeutronSafeDictionary<(int, int, RegisterMode), NeutronView>();
        public bool HasPhysics { get => _hasPhysics; set => _hasPhysics = value; }
        public GameObject[] GameObjects { get => _gameObjects; set => _gameObjects = value; }
        #endregion
    }
}