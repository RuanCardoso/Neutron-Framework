using System.Reflection;
using UnityEngine;

namespace NeutronNetwork
{
    public class NeutronStaticBehaviour : MonoBehaviour
    {
        public static NeutronStaticBehaviour[] neutronStatics { get; private set; }
        public MethodInfo[] methods { get; private set; }

        private void OnEnable()
        {
            neutronStatics = FindObjectsOfType<NeutronStaticBehaviour>();
            GetMethods();
        }

        public void GetMethods()
        {
            methods = this.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        }
    }
}