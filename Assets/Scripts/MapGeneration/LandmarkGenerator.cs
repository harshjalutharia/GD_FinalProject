using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEditor;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;

public class LandmarkGenerator : MonoBehaviour
{
    [Header("=== Landmark Settings ===")]
    [SerializeField] private CombinedMap terrainMap;                // Combined map game object
    [SerializeField] private GameObject landmarkPrefab;             // Prefab for landmarks
    [SerializeField] private float minimumDistanceBetweenLandmarks; // Minimum distance between any 2 landmarks
    [SerializeField] private int landmarkCount;                     // Total number of landmarks to spawn
    [SerializeField] private Transform finalLandmarkPosition;       // Position of the final landmark in the map
    [SerializeField] private float spawnBorderPadding;              // Padding to avoid spawning on edge of map
    [SerializeField] private int spawnTriesPerLandmark;             // Possible spawn points for each new landmark
    [SerializeField] private int maxRetries = 10;                   // Maximum retries to spawn all landmarks before it gives up
    [SerializeField] private float randomizationFactor;             // Randomization to add when generating new points

    private List<GameObject> landmarks;         // List of landmarks in the map
    private GameObject landmarkParentObject;    // Parent game object for all landmarks

    public void GenerateLandmarks()
    {
        if (landmarkPrefab == null)
        {
            Debug.Log("Landmark prefab not assigned in editor");
            return;
        }
        float x_size = (float)terrainMap.noiseMap.GetLength(0)/2;
        float z_size = (float)terrainMap.noiseMap.GetLength(1)/2;

        ClearLandmarks();
        landmarkParentObject = new GameObject("Landmarks");
        landmarks = new List<GameObject>();

        // First spawn the final Landmark
        GameObject finalLandmark = Instantiate(landmarkPrefab, finalLandmarkPosition.position,
            finalLandmarkPosition.rotation, landmarkParentObject.transform);
        landmarks.Add(finalLandmark);

        // Stores location of last landmark
        Vector3 lastLandmarkPosition = finalLandmark.transform.position;

        int currentRetries = 0;

        for (int i = 0; i < landmarkCount; i++)
        {
            // Generate spawn points based on last landmark's position
            List<Vector3> possibleSpawns = GenerateSpawnPoints(lastLandmarkPosition);

            // if no spawn points are valid, go back to a previous landmark and try again
            if (possibleSpawns.Count == 0)
            {
                currentRetries++;
                // if exhausted all possible retries, stop generating new points
                if (currentRetries > maxRetries)
                    break;
                i--;
                lastLandmarkPosition = landmarks[Random.Range(0, landmarks.Count-1)].transform.position;
                continue;
            }

            currentRetries = Math.Clamp(currentRetries - 1, 0, maxRetries);

            // Spawn landmark at random new position and add it to the list
            Vector3 newLandmarkPosition = possibleSpawns[Random.Range(0, possibleSpawns.Count)];
            GameObject temp = Instantiate(landmarkPrefab, newLandmarkPosition, Quaternion.identity, landmarkParentObject.transform);
            landmarks.Add(temp);
            lastLandmarkPosition = newLandmarkPosition;
        }
    }

    // Generates spawn points in a circle around a location
    private List<Vector3> GenerateSpawnPoints(Vector3 location)
    {
        float x_size = (float) terrainMap.noiseMap.GetLength(0) / 2;
        float z_size = (float) terrainMap.noiseMap.GetLength(1) / 2;

        List<Vector3> spawnLocations = new List<Vector3>();
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
                bool isValidPoint = true;

                // checks if new point is far away from other landmarks
                foreach (var landmark in landmarks)
                {
                    float distance = Vector3.Distance(newPoint, landmark.transform.position);
                    if (distance < minimumDistanceBetweenLandmarks)
                    {
                        isValidPoint = false;
                        break;
                    }
                }

                if (isValidPoint)
                {
                    spawnLocations.Add(newPoint);
                }
                    
            }

            angle += 2 * Math.PI / (double)spawnTriesPerLandmark;
        }

        return spawnLocations;
    }

    public void ClearLandmarks()
    {
        if (!EditorApplication.isPlaying)
            DestroyImmediate(landmarkParentObject);
        else
            Destroy(landmarkParentObject);
    }
}
