using NeutronNetwork.Helpers;
using UnityEditor;

namespace NeutronNetwork.Editor
{
    // ensure class initializer is called whenever scripts recompile
    [InitializeOnLoad]
    public static class PlayModeStateChangedLoad
    {
        // register an event handler when the class is initialized
        static PlayModeStateChangedLoad()
        {

        }

        [InitializeOnLoadMethod]
        static void DefineSymbols()
        {
            Helper.SetDefines(false, "NEUTRON_NETWORK");
        }

        [InitializeOnLoadMethod]
        static void CheckDomainIsActive()
        {
            if (!EditorSettings.enterPlayModeOptions.HasFlag(EnterPlayModeOptions.DisableDomainReload))
                UnityEngine.Debug.LogError("Domain reload is disabled. The Neutron does not support! Maybe in the future?");
        }
    }
}