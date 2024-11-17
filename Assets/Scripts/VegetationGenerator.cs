using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VegetationGenerator : MonoBehaviour
{
    [System.Serializable]
    public class VegetationPrefab {
        public GameObject prefab;
        public int mapRenderSize;
        public Color mapColor;
    }

    [Header("=== References ===")]
    [SerializeField, Tooltip("Reference to the noise map used for terrain generation")]                                     private NoiseMap m_terrainGenerator;
    [SerializeField, Tooltip("Reference to the voronoi map used for region segmentation")]                                  private  VoronoiMap m_voronoiMap;
    [SerializeField, Tooltip("Prefabs used for this generator. Allows for modding of their appearance on the held map")]    private List<VegetationPrefab> m_prefabs;
    [SerializeField, Tooltip("The parent of the generated objects. If unset, will auto-set to this game object.")]          private Transform m_vegetationParent;
    
    [Header("=== Generation Settings ===")]
    [SerializeField, Tooltip("The seed used to control psuedo-rng for this map.")]                                                  private int m_seed;
    [SerializeField, Tooltip("Max number of vegetation objects we want to spawn.")]                                                 private int m_maxNumVegetation = 200;
    [SerializeField, Tooltip("For each pixel, will spawn vegetation object if prng number is below this threshold"), Range(0f, 1f)] private float m_generationThreshold = 0.5f;
    [SerializeField, Tooltip("How much away (relatively) from border edge do we want to not spawn objects?"), Range(0f, 0.5f)]      private float m_edgeBuffer;    
    [SerializeField, Tooltip("Min noise map threshold to consider a pixel cell to generate in"), Range(0f,1f)]                      private float m_minHeight = 0.3f;
    [SerializeField, Tooltip("Max noise map threshold to consider a pixel cell to generate in"), Range(0f,1f)]                      private float m_maxHeight = 0.6f;
    [SerializeField, Tooltip("Rotation Offset for rotating the prefab"), Range(0,30)]                      private int m_rotationOffset;

    [Header("=== Outputs - READ ONLY ===")]
    public List<GameObject> generatedTree;

    private void Awake() {
        if (m_vegetationParent == null) m_vegetationParent = this.transform;
    }

    
    public virtual void SetSeed(string newSeed) {
        if (newSeed.Length > 0 && int.TryParse(newSeed, out int validNewSeed)) {
            m_seed = validNewSeed;
            return;
        }
        m_seed = UnityEngine.Random.Range(0, 1000001);
    }
    public virtual void SetSeed(int newSeed) {
        m_seed = newSeed;
    }

    public void GenerateVegetation() {
        // Determine random number generator based on seed value
        System.Random prng = new System.Random(m_seed);

        // Determine some constants, counters, and the output array
        int mapChunkSize = m_terrainGenerator.mapChunkSize;
        int edgeDistance  = Mathf.FloorToInt(mapChunkSize* m_edgeBuffer);
        int treeCount = 0;
        generatedTree= new List<GameObject> ();

        // Iterate through each pixel
        for (int y= 0; y < mapChunkSize; y++){
            
            // Ignore this pixel cell if the y-coord is within buffer zone
            if (y < edgeDistance || y > mapChunkSize - edgeDistance) continue;

            for (int x =0; x < mapChunkSize; x++){
    
                // Ignore this pixel cell if the x-coord is within buffer zone
                if (x < edgeDistance || x > mapChunkSize - edgeDistance) continue;
                
                // x and y are coordinate for the map.
                // Get the noise value from the voronoi map
                float noise = m_voronoiMap.QueryNoiseAtCoords(x ,y, out Vector3 dummy);

                // If the noise value is not within the range determined by min and max height, ignore this pixel.
                if (noise < m_minHeight || noise > m_maxHeight) continue;

                // Check if we should actually plant this...
                float shouldPlant = (float)prng.Next(0, 100) / 100f;
                if (shouldPlant > m_generationThreshold) continue;

                // Select which prefab to use, and get its characteristics
                int prefabIndex = prng.Next(0, m_prefabs.Count);
                GameObject prefab = m_prefabs[prefabIndex].prefab;
                int mapRadius = m_prefabs[prefabIndex].mapRenderSize;
                Color mapColor = m_prefabs[prefabIndex].mapColor;

                // Get the world position of the new object to be spawned.
                float heightNoise = m_terrainGenerator.QueryHeightAtCoords(x, y, out Vector3 pixelPosition);
                float posOffsetX = (float)prng.Next(0, 100) / 100f;
                float posOffsetZ = (float)prng.Next(0, 100) / 100f;
                Vector3 pos = pixelPosition + new Vector3 (posOffsetX, 0f, posOffsetZ);

                // Get the orientation of the planted tree
                float rotOffsetX = prng.Next(0,m_rotationOffset);
                float rotOffsetY = prng.Next(0,360);
                float rotOffsetZ = prng.Next(0,m_rotationOffset);
                Quaternion rot = Quaternion.Euler(rotOffsetX, rotOffsetY, rotOffsetZ);

                // instantiate the object, store its reference in `generatedTree`, and add its circle to held map
                GameObject t = Instantiate (prefab, pos , rot, m_vegetationParent);
                generatedTree.Add(t);
                m_terrainGenerator.DrawCircleOnHeldMap(pos.x, pos.z, mapRadius, mapColor);

                // Break early of the total # of generated trees already has reached the max number possible.
                if (generatedTree.Count >= m_maxNumVegetation) break;
            }
            // Break early of the total # of generated trees already has reached the max number possible.
            if (generatedTree.Count >= m_maxNumVegetation) break;
        }

    }
}
