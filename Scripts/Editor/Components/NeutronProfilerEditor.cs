using NeutronNetwork.Helpers;
using NeutronNetwork.Internal;
using NeutronNetwork.Internal.Components;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class NeutronProfilerEditor : EditorWindow
{
    #region Variables
    float t_Timer = 0;
    #endregion

    [MenuItem("Neutron/Neutron/Profiler")]
    static void Init()
    {
        var Window = GetWindow(typeof(NeutronProfilerEditor), true, "Profiler");
        Window.maxSize = new Vector2(480, 220);
        Window.minSize = new Vector2(480, 220);
    }

    private void OnGUI()
    {
        #region Windows
        BeginWindows();
        GUILayout.Window(1, new Rect(5, 5, 230, 200), DrawClientWindow, "Client");
        GUILayout.Window(2, new Rect(245, 5, 230, 200), DrawServerWindow, "Server");
        EndWindows();
        #endregion
    }

    void DrawClientWindow(int unusedWindowID)
    {
        #region Disabled
        GUI.FocusControl(null);
        #endregion

        #region Header
        EditorGUILayout.LabelField("TCP", GUI.skin.box);
        #endregion
        if (NeutronStatistics.m_ClientTCP.Get(out int TCPOutgoing, out int TCPIncoming))
        {
            EditorGUILayout.LabelField($"Incoming: {NeutronHelper.SizeSuffix(TCPIncoming)} | [{NeutronHelper.SizeSuffix(TCPIncoming, 2, 4)}]");
            EditorGUILayout.LabelField($"Outgoing: {NeutronHelper.SizeSuffix(TCPOutgoing)} | [{NeutronHelper.SizeSuffix(TCPOutgoing, 2, 4)}]");
        }
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        #region Header
        EditorGUILayout.LabelField("UDP", GUI.skin.box);
        #endregion
        if (NeutronStatistics.m_ClientUDP.Get(out int UDPOutgoing, out int UDPIncoming))
        {
            EditorGUILayout.LabelField($"Incoming: {NeutronHelper.SizeSuffix(UDPIncoming)} | [{NeutronHelper.SizeSuffix(UDPIncoming, 2, 4)}]");
            EditorGUILayout.LabelField($"Outgoing: {NeutronHelper.SizeSuffix(UDPOutgoing)} | [{NeutronHelper.SizeSuffix(UDPOutgoing, 2, 4)}]");
        }
    }

    private void Update()
    {
        if (Application.isPlaying)
        {
            t_Timer += Time.deltaTime;
            if (t_Timer >= 1)
            {
                Repaint();
                t_Timer = 0;
            }
        }
    }

    void DrawServerWindow(int unusedWindowID)
    {
        #region Disabled
        GUI.FocusControl(null);
        #endregion

        #region Header
        EditorGUILayout.LabelField("TCP", GUI.skin.box);
        #endregion
        if (NeutronStatistics.m_ServerTCP.Get(out int TCPOutgoing, out int TCPIncoming))
        {
            EditorGUILayout.LabelField($"Incoming: {NeutronHelper.SizeSuffix(TCPIncoming)} | [{NeutronHelper.SizeSuffix(TCPIncoming, 2, 4)}]");
            EditorGUILayout.LabelField($"Outgoing: {NeutronHelper.SizeSuffix(TCPOutgoing)} | [{NeutronHelper.SizeSuffix(TCPOutgoing, 2, 4)}]");
        }
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        #region Header
        EditorGUILayout.LabelField("UDP", GUI.skin.box);
        #endregion
        if (NeutronStatistics.m_ServerUDP.Get(out int UDPOutgoing, out int UDPIncoming))
        {
            EditorGUILayout.LabelField($"Incoming: {NeutronHelper.SizeSuffix(UDPIncoming)} | [{NeutronHelper.SizeSuffix(UDPIncoming, 2, 4)}]");
            EditorGUILayout.LabelField($"Outgoing: {NeutronHelper.SizeSuffix(UDPOutgoing)} | [{NeutronHelper.SizeSuffix(UDPOutgoing, 2, 4)}]");
        }
    }
}