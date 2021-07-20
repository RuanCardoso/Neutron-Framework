using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Linq;

public class MeshInfo : EditorWindow
{
    private int m_VertexCount;
    private int m_SubmeshCount;
    private int m_TriangleCount;
    private int m_EdgeCount;
    private int m_FaceCount;

    [MenuItem("Neutron/Addons/Mesh Info")]
    static void Init()
    {
        var Window = GetWindow(typeof(MeshInfo), true, "Mesh Info");
        Window.maxSize = new Vector2(240, 210);
        Window.minSize = new Vector2(240, 210);
    }

    private void OnGUI()
    {
        BeginWindows();
        GUILayout.Window(3, new Rect(5, 5, 230, 195), Draw, "Mesh Info");
        EndWindows();
    }

    private void Draw(int unusedWindowID)
    {
        #region Disabled
        GUI.FocusControl(null);
        #endregion

        #region Logic
        MeshFilter[][] gameObjects = Selection.gameObjects.Select(x => x.GetComponentsInChildren<MeshFilter>()).ToArray();
        if (gameObjects != null)
        {
            Reset();
            foreach (var gO in gameObjects)
            {
                if (gO != null)
                {
                    foreach (var meshFilter in gO)
                    {
                        if (meshFilter != null)
                        {
                            int l_Vertex = meshFilter.sharedMesh.vertexCount;
                            int l_Triangles = meshFilter.sharedMesh.triangles.Length;

                            m_VertexCount += l_Vertex;
                            m_TriangleCount += l_Triangles / 3;
                            m_FaceCount = m_TriangleCount;
                            m_EdgeCount = m_TriangleCount * 3 - (m_FaceCount / 2);
                        }
                        else continue;
                    }
                }
                else continue;
            }
            #endregion

            #region Layout
            EditorGUILayout.LabelField($"Selected Objects: {Selection.gameObjects.Length}", GUI.skin.box);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.BeginVertical();
            EditorGUILayout.LabelField($"Vertices: {m_VertexCount} | Only:  {m_VertexCount / 3}", GUI.skin.textField);
            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField($"Triangles: {m_TriangleCount}", GUI.skin.textField);
            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField($"Edges: {m_EdgeCount} | Only:  {(m_TriangleCount * 3) / 3}", GUI.skin.textField);
            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField($"Faces(Tris): {m_FaceCount} | Quads: {m_FaceCount / 2}", GUI.skin.textField);
            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField($"SubMeshes: {m_SubmeshCount}", GUI.skin.textField);
            GUILayout.EndVertical();
            #endregion
        }
    }

    private void OnSelectionChange() => Repaint();

    private void Reset()
    {
        m_VertexCount = 0;
        m_TriangleCount = 0;
        m_SubmeshCount = 0;
        m_EdgeCount = 0;
        m_FaceCount = 0;
    }
}