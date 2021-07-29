using NeutronNetwork.Internal.Wrappers;
using System;
using System.Collections.Generic;
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
        public NeutronSafeDictionary<(int, int), NeutronView> Views = new NeutronSafeDictionary<(int, int), NeutronView>();
        [SerializeField] private GameObject[] _gameObjects;
        #endregion

        #region Properties
        public bool HasPhysics { get; set; }
        public GameObject[] GameObjects { get => _gameObjects; set => _gameObjects = value; }
        #endregion
    }
}