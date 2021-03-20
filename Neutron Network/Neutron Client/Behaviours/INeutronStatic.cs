using System.Reflection;
using UnityEngine;

namespace NeutronNetwork
{
    public class NeutronStatic : MonoBehaviour
    {
        public static NeutronStatic[] neutronStatics { get; private set; }
        public MethodInfo[] methods { get; private set; }

        private void OnEnable()
        {
            neutronStatics = FindObjectsOfType<NeutronStatic>();
            GetMethods();
        }

        public void GetMethods()
        {
            methods = this.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        }
    }
}