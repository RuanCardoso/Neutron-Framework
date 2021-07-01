using NeutronNetwork.Components;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NeutronVoiceChat))]
public class NeutronVoiceChatEditor : Editor
{
    private NeutronVoiceChat neutronVoiceChatTarget;

    private void OnEnable()
    {
        neutronVoiceChatTarget = (NeutronVoiceChat)target;
        neutronVoiceChatTarget.devicesName = Microphone.devices;
        if (neutronVoiceChatTarget.audioSource == null)
            neutronVoiceChatTarget.audioSource = neutronVoiceChatTarget.GetComponent<AudioSource>();
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("This component is still in the testing phase.\r\nThis component has high bandwidth usage, the higher the frequency the greater the usage.", MessageType.Warning);
        base.OnInspectorGUI();
    }
}