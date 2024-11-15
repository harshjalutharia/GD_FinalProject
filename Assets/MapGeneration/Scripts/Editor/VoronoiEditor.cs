using System.Collections;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Voronoi), true)]
public class VoronoiEditor : Editor
{
    public override void OnInspectorGUI() {
        Voronoi v = (Voronoi)target;

        if (DrawDefaultInspector()) {
            if (v.autoUpdate) {
                v.GenerateTessellation();
            }
        }

        if (GUILayout.Button("Generate Voronoi")) {
            v.GenerateTessellation();
        }
    }
}
