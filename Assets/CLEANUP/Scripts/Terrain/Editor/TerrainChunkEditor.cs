using System.Collections;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TerrainChunk), true)]
public class TerrainChunkEditor : Editor
{
    public override void OnInspectorGUI() {
        TerrainChunk chunk = (TerrainChunk)target;

        if (DrawDefaultInspector()) {
            if (chunk.autoUpdate) {
                chunk.GenerateMap();
            }
        }

        if (GUILayout.Button("Generate")) {
            chunk.GenerateMap();
        }
    }
}
