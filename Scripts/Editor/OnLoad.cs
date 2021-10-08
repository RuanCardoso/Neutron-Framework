using NeutronNetwork;
using UnityEditor;

// ensure class initializer is called whenever scripts recompile
[InitializeOnLoad]
public static class PlayModeStateChangedLoad
{
    // register an event handler when the class is initialized
    static PlayModeStateChangedLoad()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
            NeutronModule.EditorLoadSettings().GlobalSettings.PerfomanceMode = false;
    }
}