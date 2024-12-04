using System.Collections;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GenerateBillboardAsset), true)]
public class GenerateBillboardAssetEditor : Editor
{
    public override void OnInspectorGUI() {
        GenerateBillboardAsset nm = (GenerateBillboardAsset)target;

        DrawDefaultInspector();

        if (GUILayout.Button("Generate")) {
            nm.GeneratePNG();
        }
    }
}
