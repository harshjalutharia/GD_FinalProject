using System.Collections;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(NoiseMap), true)]
public class NoiseMapEditor : Editor
{
    public override void OnInspectorGUI() {
        NoiseMap nm = (NoiseMap)target;

        if (DrawDefaultInspector()) {
            if (nm.autoUpdate) {
                nm.GenerateMap();
            }
        }

        if (GUILayout.Button("Generate")) {
            nm.GenerateMap();
        }
    }
}
