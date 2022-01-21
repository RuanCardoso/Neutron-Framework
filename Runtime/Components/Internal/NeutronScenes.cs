using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeutronNetwork.Editor
{
    public class NeutronScenes : MonoBehaviour
    {
        /*[HideInInspector] */
        public List<SubScene> _subScenes;

        [System.Obsolete]
        private void Update()
        {
            //Scene[] scenes = SceneManager.GetAllScenes();
            //_subScenes = scene
        }
    }
}