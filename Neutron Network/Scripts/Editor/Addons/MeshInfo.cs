using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Linq;

public class MeshInfo : EditorWindow
{

    private int vertexCount;
    private int submeshCount;
    private int triangleCount;
    private int edgeCount;
    private int faceCount;

    [MenuItem("Neutron/Tools/Mesh Info")]
    static void Init()
    {
        var Window = GetWindow(typeof(MeshInfo), true, "Mesh Info");
        Window.maxSize = new Vector2(200, 150);
        Window.minSize = new Vector2(200, 150);
    }

    void OnSelectionChange() => Repaint();

    void OnGUI()
    {
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
                            #region Geometry
                            int vertex = meshFilter.sharedMesh.vertexCount;
                            int triangles = meshFilter.sharedMesh.triangles.Length;
                            #endregion

                            #region Calcs
                            triangleCount += triangles / 3;
                            vertexCount += vertex;
                            #region Debug
                            faceCount = triangleCount;
                            edgeCount = (faceCount / 2) * 5;
                            #endregion
                            #endregion
                        }
                        else continue;
                    }
                }
                else continue;
            }
            EditorGUILayout.LabelField($"Selected Objects: {Selection.gameObjects.Length}");
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.BeginVertical("", GUI.skin.button);
            EditorGUILayout.LabelField($"Vertices: {vertexCount}");
            EditorGUILayout.LabelField($"Triangles: {triangleCount}");
            EditorGUILayout.LabelField($"Edges: {edgeCount}");
            EditorGUILayout.LabelField($"Faces: {faceCount}");
            EditorGUILayout.LabelField($"SubMeshes: {submeshCount}");
            GUILayout.EndVertical();
        }
    }

    void Reset()
    {
        vertexCount = 0;
        triangleCount = 0;
        submeshCount = 0;
        edgeCount = 0;
        faceCount = 0;
    }
}