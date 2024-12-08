using System.Collections;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(VegetationGenerator), true)]
public class VegetationGeneratorEditor : Editor
{
    public override void OnInspectorGUI() {
        VegetationGenerator generator = (VegetationGenerator)target;

        DrawDefaultInspector();

        if (GUILayout.Button("Generate Vegetation")) {
            generator.GenerateVegetation();
        }
    }
}
