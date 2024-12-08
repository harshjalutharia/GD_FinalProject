using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RegionAttribute", menuName = "Generation/Region Attribute", order = 1)]
public class RegionAttributes : ScriptableObject
{
    public string name;
    public List<VegetationGenerator.VegetationPrefab> m_vegetationPrefabs;
    public LandmarkGenerator.LandmarkGroupCounter m_landmarkPrefabs;
}