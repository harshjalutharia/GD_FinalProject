using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class LandmarkGenerator : MonoBehaviour
{
    [Header("=== References ===")]
    [SerializeField, Tooltip("Prefab for landmarks")]                                       private GameObject landmarkPrefab;
    [SerializeField, Tooltip("Parent for landmarks")]                                       private Transform m_landmarkParent;

    [Header("=== Generation Settings ===")]
    [SerializeField, Tooltip("Seed integer for landmark randomization")]                    private int m_seed;
    [SerializeField, Tooltip("Minimum distance between any 2 landmarks")]                   private float minimumDistanceBetweenLandmarks = 50f;
    [SerializeField, Tooltip("Total number of landmarks to spawn")]                         private int landmarkCount = 6;
    [SerializeField, Tooltip("Padding to avoid spawning on edge of map")]                   private float spawnBorderPadding = 35f;
    [SerializeField, Tooltip("Possible spawn points for each new landmark")]                private int spawnTriesPerLandmark = 24;
    [SerializeField, Tooltip("Maximum retries to spawn all landmarks before it gives up")]  private int maxRetries = 10;
    [SerializeField, Tooltip("Randomization to add when generating new points")]            private float randomizationFactor = 20f;
    [SerializeField, Tooltip("Place landmark at destination?")]                             private bool m_placeLandmarkAtDest = false;

    [Header("=== Outputs - READ ONLY ===")]
    [SerializeField, Tooltip("List of landmark positions")]                                 private List<SpawnPoint> m_landmarkPositions;

    [System.Serializable]
    public class SpawnPoint
    {
        public Vector3 location;
        public float distanceToLandmarks;
        public GameObject landmarkObject;
        public SpawnPoint(Vector3 location) {
            this.location = location;
        }
        public SpawnPoint(Vector3 location, float distanceToLandmarks) {
            this.location = location;
            this.distanceToLandmarks = distanceToLandmarks;
        }

        public void InstantiateLandmark(GameObject prefab, Transform objectParent) {
            this.landmarkObject = Instantiate(prefab, this.location, Quaternion.identity, objectParent);
        }
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

    public void GenerateLandmarks(Vector3 destination, NoiseMap terrainMap)
    {
        // Check Flag: do we have a landmark prefab to begin with?
        if (landmarkPrefab == null) {
            Debug.Log("Landmark prefab not assigned in editor");
            return;
        }

        // Define the landmark parent
        if (m_landmarkParent == null) m_landmarkParent = this.transform;

        // Pre-calculate the x_size and z_size of the provided terrain map
        float x_size = (float)terrainMap.noiseMap.GetLength(0)/2;
        float z_size = (float)terrainMap.noiseMap.GetLength(1)/2;

        // Clear our previous existing list of landmarks, if they exist.
        ClearLandmarks();
        m_landmarkPositions = new List<SpawnPoint>();

        // First generate the spawn point of the final Landmark. If we want to create a landmark at the destination, do so.
        m_landmarkPositions.Add(new SpawnPoint(destination));
        if (m_placeLandmarkAtDest) m_landmarkPositions[0].InstantiateLandmark(landmarkPrefab, m_landmarkParent);

        // Stores location of last landmark
        Vector3 lastLandmarkPosition = destination;

        // Initialize the number of active retries so far
        int currentRetries = 0;

        // Iterate through the number of landmarks we want to generate
        for (int i = 0; i < landmarkCount; i++) {
            // Generate spawn points based on last landmark's position
            List<SpawnPoint> possibleSpawns = GenerateSpawnPoints(lastLandmarkPosition, terrainMap, x_size, z_size);

            // if no spawn points are valid, go back to a previous landmark and try again
            if (possibleSpawns.Count == 0) {
                currentRetries++;
                // if exhausted all possible retries, stop generating new points
                if (currentRetries > maxRetries)
                    break;
                i--;
                lastLandmarkPosition = m_landmarkPositions[Random.Range(0, m_landmarkPositions.Count)].location;
                continue;
            }

            currentRetries = Math.Clamp(currentRetries - 1, 0, maxRetries);

            // Spawn landmark at random new position and add it to the list
            SpawnPoint randomPoint = ChooseSpawnPoint(possibleSpawns);
            m_landmarkPositions.Add(randomPoint);
            lastLandmarkPosition = randomPoint.location;
        }

        // After everything, spawn all landmarks. We skip the first one because that's the final spawn point
        for(int i = 1; i < m_landmarkPositions.Count; i++) m_landmarkPositions[i].InstantiateLandmark(landmarkPrefab, m_landmarkParent);

    }

    // Generates spawn points in a circle around a location
    private List<SpawnPoint> GenerateSpawnPoints(Vector3 location, NoiseMap terrainMap, float x_size, float z_size)
    {
        List<SpawnPoint> spawnLocations = new List<SpawnPoint>();
        double angle = 0;
        while (angle < 2 * Math.PI)
        {
            double x = Math.Cos(angle) * minimumDistanceBetweenLandmarks;
            x += location.x + Random.Range(-randomizationFactor, randomizationFactor);
            double z = Math.Sin(angle) * minimumDistanceBetweenLandmarks;
            z += location.z + Random.Range(-randomizationFactor, randomizationFactor);

            // If new point is within bounds of map
            if (x < x_size - spawnBorderPadding && x > -x_size + spawnBorderPadding && z < z_size - spawnBorderPadding && z > -z_size + spawnBorderPadding)
            {
                int gridX, gridY;   // not used here but required for the function call below
                float y = terrainMap.QueryHeightAtWorldPos((float)x, (float)z, out gridX, out gridY);
                Vector3 newPoint = new Vector3((float)x, y, (float)z);
                float totalDistance;

                // If point is not too close to landmarks, add it to list
                if (CheckDistanceFromLandmarks(newPoint, out totalDistance)) {
                    SpawnPoint newSpawnPoint = new SpawnPoint(newPoint,totalDistance);
                    spawnLocations.Add(newSpawnPoint);
                }
            }

            angle += 2 * Math.PI / (double)spawnTriesPerLandmark;
        }

        return spawnLocations;
    }

    // Returns false if point is too close to landmarks and outs the total distance from landmarks if point is valid
    private bool CheckDistanceFromLandmarks(Vector3 point, out float totalDistance)
    {
        bool isValidPoint = true;
        totalDistance = 0f;

        // checks distance of new point from other landmarks
        foreach(SpawnPoint landmark in m_landmarkPositions) {
            float distance = Vector3.Distance(point, landmark.location);

            // if the new point is too close to any landmark, discard it
            if (distance < minimumDistanceBetweenLandmarks) {
                isValidPoint = false;
                break;
            }
            totalDistance += distance;
        }

        return isValidPoint;
    }

    private SpawnPoint ChooseSpawnPoint(List<SpawnPoint> spawnPoints)
    {
        float totalDistance = 0;
        foreach (var point in spawnPoints) {
            totalDistance += point.distanceToLandmarks;
        }

        float randomTotalDistance = Random.Range(1, totalDistance+1);
        float processedDistance = 0f;
        foreach (var point in spawnPoints)
        {
            processedDistance += point.distanceToLandmarks;
            if (randomTotalDistance <= processedDistance) {
                return point;
            }
        }

        return null;
    }

    public void ClearLandmarks()
    {
        while(m_landmarkPositions.Count > 0) {
            GameObject landmark = m_landmarkPositions[0].landmarkObject;
            m_landmarkPositions.RemoveAt(0);
            #if UNITY_EDITOR
                if (!EditorApplication.isPlaying)   DestroyImmediate(landmark);
                else                                Destroy(landmark);
            #else
                Destroy(landmark);
            #endif
        }
    }
}
