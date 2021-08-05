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
        public NeutronSafeDictionary<(int, int, RegisterType), NeutronView> Views = new NeutronSafeDictionary<(int, int, RegisterType), NeutronView>();
        [SerializeField] private GameObject[] _gameObjects;
        [SerializeField] private bool _hasPhysics;
        #endregion

        #region Properties
        public bool HasPhysics { get => _hasPhysics; set => _hasPhysics = value; }
        public GameObject[] GameObjects { get => _gameObjects; set => _gameObjects = value; }
        #endregion
    }
}