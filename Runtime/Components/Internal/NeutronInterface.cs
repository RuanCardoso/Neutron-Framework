using NeutronNetwork.Constants;
using NeutronNetwork.Extensions;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeutronNetwork.UI
{
    [DefaultExecutionOrder(ExecutionOrder.NEUTRON_CONFIG)]
    public class NeutronInterface : MonoBehaviour
    {
        private static readonly Dictionary<(string, string, string), Component> _components = new Dictionary<(string, string, string), Component>();

        /// <summary>
        ///* Não chame esta função com frequência, obtenha o resultado e guarde-o em cache.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="rootName">Nome da raiz da hierarquia.</param>
        /// <returns></returns>
        public static T GetUIComponent<T>(string rootName)
        {
            return (T)Convert.ChangeType(_components[(rootName, rootName, rootName)].GetComponent(typeof(T)), typeof(T));
        }

        /// <summary>
        ///* Não chame esta função com frequência, obtenha o resultado e guarde-o em cache.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="rootName">Nome da raiz da hierarquia.</param>
        /// <param name="parentName">Nome do pai do objeto de destino.</param>
        /// <returns></returns>
        public static T GetUIComponent<T>(string rootName, string parentName)
        {
            return (T)Convert.ChangeType(_components[(rootName, parentName, parentName)].GetComponent(typeof(T)), typeof(T));
        }

        /// <summary>
        ///* Não chame esta função com frequência, obtenha o resultado e guarde-o em cache.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="rootName">Nome da raiz da hierarquia.</param>
        /// <param name="parentName">Nome do pai do objeto de destino.</param>
        /// <param name="name">Nome do objeto de destino.</param>
        /// <returns></returns>
        public static T GetUIComponent<T>(string rootName, string parentName, string name)
        {
            return (T)Convert.ChangeType(_components[(rootName, parentName, name)].GetComponent(typeof(T)), typeof(T));
        }

        /// <summary>
        ///* Não chame esta função com frequência, obtenha o resultado e guarde-o em cache.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="rootName">Nome da raiz da hierarquia.</param>
        /// <param name="parentName">Nome do pai do objeto de destino.</param>
        /// <param name="name">Nome do objeto de destino.</param>
        /// <returns></returns>
        public static T[] GetUIComponent<T>(string rootName, string parentName, string[] names)
        {
            List<T> components = new List<T>();
            foreach (var name in names)
                components.Add((T)Convert.ChangeType(_components[(rootName, parentName, name)].GetComponent(typeof(T)), typeof(T)));
            return components.ToArray();
        }

        [Obsolete]
        private void GetComponents()
        {
            _components.Clear();
#if UNITY_2020_1_OR_NEWER
            var arrayOfCanvas = FindObjectsOfType<Canvas>(true);
#else
            var arrayOfCanvas = FindObjectsOfTypeAll(typeof(Canvas)) as Canvas[];
#endif
            foreach (var canvas in arrayOfCanvas)
            {
                var components = canvas.GetComponentsInChildren<Component>(true);
                foreach (var component in components)
                {
                    try
                    {
                        if (component.TryGetComponent<RectTransform>(out var tr))
                        {
                            string parentName = tr.root.name;
                            if (tr.parent != null)
                                parentName = tr.parent.name;
                            var keyName = (tr.root.name, parentName, tr.name);
                            if (tr != null)
                            {
                                if (!_components.ContainsKey(keyName))
                                    _components.Add(keyName, component);
                                else
                                    continue;
                            }
                            else
                                continue;
                        }
                        else
                            continue;
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Error($"UI Component -> {component.name} failed to add! [{ex.Message}]");
                    }
                }
            }
        }

        [Obsolete]
#pragma warning disable IDE0051
        private void Start()
#pragma warning restore IDE0051
        {
            GetComponents();
            //* Obtém os componentes da Ui na nova scene carregada.
            SceneManager.sceneLoaded += (scene, mode) => GetComponents();
        }
    }
}