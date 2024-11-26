using System.Collections;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TestFinalTerrainGenerator), true)]
public class TestFinalTerrainGeneratorEditor : Editor
{
    public override void OnInspectorGUI() {
        TestFinalTerrainGenerator nm = (TestFinalTerrainGenerator)target;

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
