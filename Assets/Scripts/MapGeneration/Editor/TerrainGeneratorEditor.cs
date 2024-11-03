using System.Collections;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TerrainGenerator))]
public class TerrainGeneratorEditor : Editor
{
    public override void OnInspectorGUI() {
        TerrainGenerator terrainGen = (TerrainGenerator)target;

        if (DrawDefaultInspector()) {
            if (terrainGen.autoUpdate) {
                terrainGen.GenerateMap();
            }
        }

        if (GUILayout.Button("Generate")) {
            terrainGen.GenerateMap();
        }
    }
}
