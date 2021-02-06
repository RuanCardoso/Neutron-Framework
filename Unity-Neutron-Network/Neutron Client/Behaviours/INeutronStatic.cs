using System.Reflection;
using UnityEngine;

namespace NeutronNetwork
{
    public class NeutronStatic : MonoBehaviour
    {
        public static NeutronStatic[] neutronStatics { get; private set; }
        public MethodInfo[] methodInfos { get; private set; }

        private void OnEnable()
        {
            neutronStatics = FindObjectsOfType<NeutronStatic>();
            GetMethods();
        }

        public void GetMethods()
        {
            methodInfos = this.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        }
    }
}