using System.Collections;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(FindShortestPath), true)]
public class FindShortestPathEditor : Editor
{
    public override void OnInspectorGUI() {
        FindShortestPath nm = (FindShortestPath)target;

        if (DrawDefaultInspector()) {
            if (nm.autoUpdate) {
                nm.GenerateShortestPath();
            }
        }

        if (GUILayout.Button("Bake NavMesh")) {
            nm.BuildNavMesh();
        }
        if (GUILayout.Button("Generate")) {
            nm.GenerateShortestPath();
        }
    }
}
