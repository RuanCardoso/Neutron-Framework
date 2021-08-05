using NeutronNetwork.Internal.Components;
using UnityEditor;

// ensure class initializer is called whenever scripts recompile
[InitializeOnLoad]
public static class PlayModeStateChangedExample
{
    // register an event handler when the class is initialized
    static PlayModeStateChangedExample()
    {
        EditorApplication.playModeStateChanged += LogPlayModeState;
    }

    private static void LogPlayModeState(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
            NeutronModule.EditorLoadSettings().GlobalSettings.PerfomanceMode = false;
    }
}