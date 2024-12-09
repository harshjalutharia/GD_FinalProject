using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RegionAttribute", menuName = "Generation/Region Attribute", order = 1)]
public class RegionAttributes : ScriptableObject
{
    public string name;
    [Header("=== Vegetation Settings ===")]
    public List<VegetationGenerator.VegetationPrefab> vegetationPrefabs;
    public float vegetationSpawnThreshold;
    public float vegetationSteepnessThreshold;
    public TerrainChunk.MinMax vegetationHeightRange;
    [Header("=== Landmarks ===")]
    public LandmarkGenerator.LandmarkGroupCounter landmarkPrefabs;
}