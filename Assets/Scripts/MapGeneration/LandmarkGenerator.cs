using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Unity.VisualScripting;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;
using static Cinemachine.CinemachineBlendDefinition;
using Vector2 = UnityEngine.Vector2;
using System.Diagnostics.CodeAnalysis;
using System.Linq;


#if UNITY_EDITOR
using UnityEditor;
#endif

public class LandmarkGenerator : MonoBehaviour
{
    [Header("=== References ===")]
    [SerializeField, Tooltip("Prefab for landmarks")]                                       private GameObject landmarkPrefab;
    [SerializeField, Tooltip("Prefab for weenies")]                                         private List<GameObject> weeniePrefab;
    [SerializeField, Tooltip("Parent for landmarks")]                                       private Transform m_landmarkParent;
    [SerializeField, Tooltip("Voronoi Map used in combinedMap")]                            private VoronoiMap m_voronoiMap;

    [Header("=== Generation Settings ===")]
    [SerializeField, Tooltip("Seed integer for landmark randomization")]                    private int m_seed;
    [SerializeField, Tooltip("Minimum distance between any 2 landmarks")]                   private float minimumDistanceBetweenLandmarks = 50f;
    [SerializeField, Tooltip("Maximum distance between 2 close landmarks")]                 private float maximumDistanceBetweenLandmarks = 70f;
    [SerializeField, Tooltip("Total number of landmarks to spawn")]                         private int landmarkCount = 6;
    [SerializeField, Tooltip("Total number of weenies between landmarks to spawn")]         private int weenieBetweenLandmarksCount = 3;
    [SerializeField, Tooltip("Padding to avoid spawning on edge of map")]                   private float spawnBorderPadding = 35f;
    [SerializeField, Tooltip("Possible spawn points for each new landmark")]                private int spawnTriesPerLandmark = 24;
    [SerializeField, Tooltip("Maximum retries to spawn all landmarks before it gives up")]  private int maxRetries = 10;
    [SerializeField, Tooltip("Randomization to add when generating new landmarks")]         private float randomizationFactor = 10f;
    [SerializeField, Tooltip("Place landmark at destination?")]                             private bool m_placeLandmarkAtDest = false;
    [SerializeField, Tooltip("Radius of the circle around end point where ruins spawn")]    private float endRegionRadius = 20f;
    [SerializeField, Tooltip("Number of ruins to spawn in end region")]                     private int endRegionRuinCount = 10;
    [SerializeField, Tooltip("Minimum distance between 2 ruins")]                           private float minimumDistanceBetweenRuins = 5f;
    [SerializeField, Tooltip("Number of points in path from where 1 weenie is visible")]    private int visionPointsCount = 10;
    [SerializeField, Tooltip("Minimum distance of weenie from path")]                       private float minimumDistanceInWeenieAndPath = 40f;
    [SerializeField, Tooltip("Minimum distance between any 2 weenies")]                     private float minimumDistanceBetweenWeenies = 40f;


    [Header("=== Outputs - READ ONLY ===")]
    [SerializeField, Tooltip("List of landmark positions")]                                 private List<SpawnPoint> m_landmarkPositions;
    [SerializeField, Tooltip("List of weenie positions")]                                   private List<Vector3> m_weeniePositions;

    [System.Serializable]
    public class SpawnPoint : IComparable<SpawnPoint>
    {
        public Vector3 location;
        public int weight;
        public GameObject spawnedObject;
        public SpawnPoint(Vector3 location) 
        {
            this.location = location;
        }
        public SpawnPoint(Vector3 location, int weight) 
        {
            this.location = location;
            this.weight = weight;
        }

        public int CompareTo([AllowNull] SpawnPoint otherPoint)
        {
            return this.weight.CompareTo(otherPoint.weight);
        }

        public void InstantiateObject(GameObject prefab, Transform objectParent) 
        {
            this.spawnedObject = Instantiate(prefab, this.location, Quaternion.identity, objectParent);
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

    public void GenerateLandmarks(Vector3 destination, Vector3 startPosition, NoiseMap terrainMap)
    {
        // Check Flag: do we have a landmark prefab to begin with?
        if (landmarkPrefab == null) 
        {
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

        if (m_voronoiMap != null)
        {
            m_landmarkPositions.Add(new SpawnPoint(destination));
            m_landmarkPositions[0].InstantiateObject(landmarkPrefab, m_landmarkParent);

            GenerateRuinsAroundPoint(destination, terrainMap);

            List<Vector3> path = new List<Vector3>();
            path.Add(startPosition);
            path.Add(destination);

            GenerateWeeniesInAllRegions(path, new List<int>(), terrainMap, x_size, z_size);
        }
        // TODO: REMOVE IF NEED TO SPAWN OTHER LANDMARKS
        return;

        if (m_voronoiMap != null)
        {
            List<Vector3> centroidPositions = m_voronoiMap.GetCentroidsInWorldPos();
            Dictionary<int, List<int>> neighbourMap = m_voronoiMap.GetNeighbourMap();

            List<int> regionLookup = new List<int>();
            
            for (int i = 0; i < centroidPositions.Count; i++)
            {
                int gridX, gridY;
                regionLookup.Add(m_voronoiMap.QueryRegionAtWorldPos(centroidPositions[i].x, centroidPositions[i].z, out gridX, out gridY));
            }

            for (int i = 0; i < centroidPositions.Count; i++)
            {
                int gridX, gridY;
                float x = centroidPositions[i].x;// + Random.Range(-randomizationFactor, randomizationFactor);
                float z = centroidPositions[i].z;// + Random.Range(-randomizationFactor, randomizationFactor);
                float y = terrainMap.QueryHeightAtWorldPos(x, z, out gridX, out gridY);
                centroidPositions[i] = new Vector3(x, y, z);
            }

            List<int> similarNeighbourCount = new List<int>();

            int maxCount = -1;
            int maxCountIndex = -1;

            for (int i = 0; i < neighbourMap.Count; i++)
            {
                if (regionLookup[i] != 2)
                {
                    similarNeighbourCount.Add(-1);
                    continue;
                }

                int count = 0;
                var neighbours = neighbourMap[i];
                foreach (var neighbour in neighbours)
                {
                    if (regionLookup[neighbour] == 2)
                        count++;
                }

                similarNeighbourCount.Add(count);
                if (count > maxCount)
                {
                    maxCount = count;
                    maxCountIndex = i;
                }
            }

            // spawn destination in the biggest region
            m_landmarkPositions.Add(new SpawnPoint(centroidPositions[maxCountIndex]));
            //if (m_placeLandmarkAtDest)
            m_landmarkPositions[0].InstantiateObject(landmarkPrefab, m_landmarkParent);
            
            for (int i = 0; i < landmarkCount-1; i++)
            {
                var possibleSpawnPoints = GetValidSpawnPoints(centroidPositions, similarNeighbourCount);

                if (possibleSpawnPoints.Count == 0)
                {
                    Debug.Log("No possible spawn points available.");
                    break;
                }

                var newPoint = ChooseSpawnPoint(possibleSpawnPoints);

                if (newPoint == null)
                {
                    Debug.Log("Choose spawn point is outputting NULL");
                    break;
                }

                m_landmarkPositions.Add(newPoint); 
            }
        }
        
        //// First generate the spawn point of the final Landmark. If we want to create a landmark at the destination, do so.
        //m_landmarkPositions.Add(new SpawnPoint(destination));
        //if (m_placeLandmarkAtDest) m_landmarkPositions[0].InstantiateLandmark(landmarkPrefab, m_landmarkParent);

        //// Stores location of last landmark
        //Vector3 lastLandmarkPosition = destination;

        //// Initialize the number of active retries so far
        //int currentRetries = 0;

        //// Iterate through the number of landmarks we want to generate
        //for (int i = 0; i < landmarkCount; i++) {
        //    // Generate spawn points based on last landmark's position
        //    List<SpawnPoint> possibleSpawns = GenerateSpawnPoints(lastLandmarkPosition, terrainMap, x_size, z_size);

        //    // if no spawn points are valid, go back to a previous landmark and try again
        //    if (possibleSpawns.Count == 0) {
        //        currentRetries++;
        //        // if exhausted all possible retries, stop generating new points
        //        if (currentRetries > maxRetries)
        //            break;
        //        i--;
        //        lastLandmarkPosition = m_landmarkPositions[Random.Range(0, m_landmarkPositions.Count)].location;
        //        continue;
        //    }

        //    currentRetries = Math.Clamp(currentRetries - 1, 0, maxRetries);

        //    // Spawn landmark at random new position and add it to the list
        //    SpawnPoint randomPoint = ChooseSpawnPoint(possibleSpawns);
        //    m_landmarkPositions.Add(randomPoint);
        //    lastLandmarkPosition = randomPoint.location;
        //}

        // After everything, spawn all landmarks. We skip the first one because that's the final spawn point
        for(int i = 1; i < m_landmarkPositions.Count; i++) m_landmarkPositions[i].InstantiateObject(landmarkPrefab, m_landmarkParent);

        List<(int, int)> generatedPaths = new List<(int, int)>();
        // Generate weenies in paths between landmarks
        for (int i = 0; i < m_landmarkPositions.Count; i++)
        {
            Vector3 point1 = m_landmarkPositions[i].location;
            var closestLandmark = GetClosestLandmark(point1);
            int closestIndex = m_landmarkPositions.IndexOf(closestLandmark);

            // if path between them already exists, continue
            if (generatedPaths.Contains((closestIndex, i))) continue;

            generatedPaths.Add((i, closestIndex));
            Vector3 point2 = closestLandmark.location;
            
            GenerateWeeniesBetweenPoints(point1, point2, weenieBetweenLandmarksCount, terrainMap);
        }
    }

    private List<SpawnPoint> GetValidSpawnPoints(List<Vector3> possibleSpawns, List<int> spawnSimilarNeighbourCount)
    {
        List<SpawnPoint> spawnLocations = new List<SpawnPoint>();

        for (int i = 0; i<possibleSpawns.Count; i++)
        {
            // spawn point does not have region index = 2
            if (spawnSimilarNeighbourCount[i] == -1)
                continue;

            float totalDistance;
            if (CheckDistanceFromLandmarks(possibleSpawns[i], out totalDistance))
            {
                spawnLocations.Add(new SpawnPoint(possibleSpawns[i], 2*spawnSimilarNeighbourCount[i]));
            }
        }

        return spawnLocations;
    }

    private SpawnPoint GetClosestLandmark(Vector3 position)
    {
        float closestDistance = float.MaxValue;
        SpawnPoint closestLandmark = null;

        // checks distance of new point from other landmarks
        foreach (SpawnPoint landmark in m_landmarkPositions)
        {
            if (landmark.location == position)
                continue;

            float distance = Vector3.Distance(position, landmark.location);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestLandmark = landmark;
            }
        }

        return closestLandmark;
    }

    public void GenerateWeeniesInAllRegions(List<Vector3> pathPoints, List<int> pathThroughSegments, NoiseMap terrainMap, float x_size, float z_size)
    {
        
        // Check Flag: do we have a landmark prefab to begin with?
        if (weeniePrefab == null)
        {
            Debug.Log("Weenie prefabs not assigned in editor");
            return;
        }

        if (weeniePrefab.Count != 3)
        {
            Debug.Log("3 weenie prefabs need to be assigned in editor");
            return;
        }

        List<Vector3> weenieSpawns = new List<Vector3>();
        List<Vector3> visionPoints = new List<Vector3>();

        Vector3 direction = pathPoints[^1] - pathPoints[0];
        direction.Normalize();
        float totalLength = Vector3.Distance(pathPoints[^1], pathPoints[0]);
        float sectionLength = totalLength / (visionPointsCount + 1);
        float randomness = sectionLength * 0.2f;
        float currentLength = 0f;

        // Points from where visibility of weenies will be checked
        for (int i = 0; i < visionPointsCount; i++)
        {
            currentLength += sectionLength;

            Vector3 visionPoint = pathPoints[0] + (direction * currentLength);

            int gridX, gridY;
            visionPoint.y = 1f + terrainMap.QueryHeightAtWorldPos(visionPoint.x, visionPoint.z, out gridX, out gridY);
            visionPoints.Add(visionPoint);
        }

        foreach (var spawnPoint in visionPoints)
        {
            Instantiate(weeniePrefab[2], spawnPoint, Quaternion.identity, m_landmarkParent);
        }

        var possibleSpawnPoints = GeneratePointsNormalToPoints(pathPoints, visionPoints, terrainMap);
        var updatedSpawnPoints = UpdateWeightsBasedOnVisibility(possibleSpawnPoints, visionPoints);

        updatedSpawnPoints.Sort();

        for (int i = 0; i < updatedSpawnPoints.Count; i++)
        {
            SpawnPoint nextPoint = updatedSpawnPoints[i];
            if (nextPoint.weight == 0)
            {
                continue;
            }

            if (CheckDistanceFromPoints(nextPoint.location, weenieSpawns, minimumDistanceBetweenWeenies,
                out float distance))
            {
                weenieSpawns.Add(nextPoint.location);
            }
        }

        for (int i = 0; i < weenieSpawns.Count; i++)
        {
            var possibleSpawns = GenerateSpawnPoints(weenieSpawns[i], weenieSpawns, minimumDistanceBetweenWeenies, terrainMap, x_size, z_size);
            if (possibleSpawns.Count == 0)
                continue;
            var nextPoint = ChooseSpawnPoint(possibleSpawns);
            weenieSpawns.Add(nextPoint.location);
        }

        foreach (var spawnPoint in weenieSpawns)
        {
            Instantiate(weeniePrefab[1], spawnPoint, Quaternion.identity, m_landmarkParent);
        }
    }

    private List<SpawnPoint> GeneratePointsNormalToPoints(List<Vector3> curve, List<Vector3> requiredPoints, NoiseMap terrainMap)
    {
        // TODO: Change to handle curves
        Vector2 endPoint = new Vector2(curve[^1].x, curve[^1].z);
        Vector2 startPoint = new Vector2(curve[0].x, curve[0].z);
        Vector2 direction = endPoint - startPoint;
        direction.Normalize();

        List<SpawnPoint> normalPoints = new List<SpawnPoint>();

        foreach (var point in requiredPoints)
        {
            Vector2 tangent = direction;
            tangent.Normalize();

            Vector2 normal = new Vector2(-tangent.y, tangent.x);
            Vector3 newPoint = point + (new Vector3(normal.x, 0f, normal.y) * minimumDistanceInWeenieAndPath);
            newPoint.y = 1f + terrainMap.QueryHeightAtWorldPos(newPoint.x, newPoint.z, out int gridX, out int gridY);
            normalPoints.Add(new SpawnPoint(newPoint));

            Vector2 oppositeNormal = new Vector2(-normal.x, -normal.y);
            Vector3 newPoint2 = point + (new Vector3(oppositeNormal.x, 0f, oppositeNormal.y) * minimumDistanceInWeenieAndPath);
            newPoint2.y = 1f + terrainMap.QueryHeightAtWorldPos(newPoint2.x, newPoint2.z, out gridX, out gridY);
            normalPoints.Add(new SpawnPoint(newPoint2));
        }

        //foreach (var spawnPoint in normalPoints)
        //{
        //    spawnPoint.InstantiateLandmark(weeniePrefab[1], m_landmarkParent);
        //}

        return normalPoints;
    }

    private List<SpawnPoint> UpdateWeightsBasedOnVisibility(List<SpawnPoint> updatePoints, List<Vector3> visionPoints)
    {
        foreach (var spawnPoint in updatePoints)
        {
            int count = 0;
            foreach (var visionPoint in visionPoints)
            {
                float distance = Vector3.Distance(spawnPoint.location, visionPoint);

                bool hit = Physics.Raycast(visionPoint, spawnPoint.location - visionPoint, out RaycastHit raycastHit, distance + 2f);
                if (hit)
                {
                    if (raycastHit.distance >= distance)
                        count++;
                }
                else
                {
                    count++;
                }
            }

            spawnPoint.weight = count;
        }

        return updatePoints;
    }

    private void GenerateWeeniesBetweenPoints(Vector3 point1, Vector3 point2, int weenieCount, NoiseMap terrainMap)
    {
        // Check Flag: do we have a landmark prefab to begin with?
        if (weeniePrefab == null)
        {
            Debug.Log("Weenie prefabs not assigned in editor");
            return;
        }

        if (weeniePrefab.Count != 3)
        {
            Debug.Log("3 weenie prefabs need to be assigned in editor");
            return;
        }

        Vector3 direction = point2 - point1;
        direction.Normalize();
        float distance = Vector3.Distance(point1, point2);
        float weenieDistance = distance / (weenieCount+1);
        float randomness = weenieDistance * 0.2f;
        float currentDistance = 0f;
        for (int i = 0; i < weenieCount; i++)
        {
            currentDistance += weenieDistance;
            Vector3 spawnPoint = point1 + (direction * currentDistance);
            spawnPoint.x += Random.Range(-randomness, randomness);
            spawnPoint.z += Random.Range(-randomness, randomness);

            int gridX, gridY;
            spawnPoint.y = terrainMap.QueryHeightAtWorldPos(spawnPoint.x, spawnPoint.z, out gridX, out gridY);
            int regionIndex = m_voronoiMap.QueryRegionAtWorldPos(spawnPoint.x, spawnPoint.z, out gridX, out gridY);

            if (regionIndex < weeniePrefab.Count)
            {
                Instantiate(weeniePrefab[regionIndex], spawnPoint, Quaternion.identity, m_landmarkParent);
            }
        }
    }

    private void GenerateRuinsAroundPoint(Vector3 point, NoiseMap voronoiMap)
    {
        List<Vector3> ruinSpawnPoints = new List<Vector3>();
        float landmarkSize = landmarkPrefab.GetComponent<Renderer>().bounds.extents.magnitude;
        float ruinSize = weeniePrefab[0].GetComponent<Renderer>().bounds.extents.magnitude;

        for (int i = 0; i < endRegionRuinCount; i++)
        {
            double r = landmarkSize + (endRegionRadius * Math.Sqrt(Random.value));
            double theta = Random.value * 2 * Math.PI;

            double x = point.x + (r * Math.Cos(theta));
            double z = point.z + (r * Math.Sin(theta));

            bool validPoint = true;

            foreach (var ruinSpawn in ruinSpawnPoints)
            {
                if (Vector3.Distance(new Vector3(ruinSpawn.x, 0f, ruinSpawn.z), new Vector3((float)x, 0f, (float)z)) <
                    minimumDistanceBetweenRuins + (2*ruinSize))
                {
                    validPoint = false;
                    break;
                }
            }

            if (!validPoint)
            {
                i--;
                continue;
            }

            float y = voronoiMap.QueryHeightAtWorldPos((float)x, (float)z, out int gridX, out int gridY);
            ruinSpawnPoints.Add(new Vector3((float)x, y, (float)z));
        }

        foreach (var spawnPoint in ruinSpawnPoints)
        {
            Instantiate(weeniePrefab[0], spawnPoint, Quaternion.identity, m_landmarkParent);
        }
    }

    // Generates spawn points in a circle around a location
    private List<SpawnPoint> GenerateSpawnPoints(Vector3 location, List<Vector3> checkPoints, float minimumDistance, NoiseMap terrainMap, float x_size, float z_size)
    {
        List<SpawnPoint> spawnLocations = new List<SpawnPoint>();
        double angle = 0;
        while (angle < 2 * Math.PI)
        {
            double x = Math.Cos(angle) * minimumDistance;
            x += location.x + Random.Range(-randomizationFactor, randomizationFactor);
            double z = Math.Sin(angle) * minimumDistance;
            z += location.z + Random.Range(-randomizationFactor, randomizationFactor);

            // If new point is within bounds of map
            if (x < x_size - spawnBorderPadding && x > -x_size + spawnBorderPadding && z < z_size - spawnBorderPadding && z > -z_size + spawnBorderPadding)
            {
                float y = terrainMap.QueryHeightAtWorldPos((float)x, (float)z, out int gridX, out int gridY);
                Vector3 newPoint = new Vector3((float)x, y, (float)z);

                // If point is not too close to other checkpoints, add it to list
                if (CheckDistanceFromPoints(newPoint, checkPoints, minimumDistance, out float totalDistance)) 
                {
                    spawnLocations.Add(new SpawnPoint(newPoint, (int)totalDistance));
                }
            }

            angle += 2 * Math.PI / (double)spawnTriesPerLandmark;
        }

        return spawnLocations;
    }

    // Returns false if point is too close to checkPoints and outs the total distance from checkPoints if point is valid
    private bool CheckDistanceFromPoints(Vector3 point, List<Vector3> checkPoints, float minimumDistance, out float totalDistance)
    {
        bool isValidPoint = true;
        totalDistance = 0f;

        // checks distance of new point from other checkpoints
        foreach (var checkPoint in checkPoints)
        {
            float distance = Vector3.Distance(point, checkPoint);

            // if the new point is too close to any checkpoint, discard it
            if (distance < minimumDistance)
            {
                isValidPoint = false;
                break;
            }
            totalDistance += distance;
        }
        
        return isValidPoint;
    }

    // Returns false if point is too close to landmarks or is too far from its closest landmark and outs the total distance from landmarks if point is valid
    private bool CheckDistanceFromLandmarks(Vector3 point, out float totalDistance)
    {
        bool isValidPoint = true;
        totalDistance = 0f;
        float closestDistance = float.MaxValue;

        // checks distance of new point from other landmarks
        foreach(SpawnPoint landmark in m_landmarkPositions) 
        {
            float distance = Vector3.Distance(point, landmark.location);

            // if the new point is too close to any landmark, discard it
            if (distance < minimumDistanceBetweenLandmarks) 
            {
                isValidPoint = false;
                break;
            }
            if (distance < closestDistance)
            {
                closestDistance = distance;
            }
            totalDistance += distance;
        }

        // Makes sure it is not too far from the closest landmark
        if (closestDistance > maximumDistanceBetweenLandmarks)
            isValidPoint = false;

        return isValidPoint;
    }

    private SpawnPoint ChooseSpawnPoint(List<SpawnPoint> spawnPoints)
    {
        if (spawnPoints.Count == 1)
            return spawnPoints[0];

        int totalWeight = 0;
        foreach (var point in spawnPoints) 
        {
            totalWeight += point.weight;
        }

        if (totalWeight == 0)
        {
            int randomIndex = Random.Range(0, spawnPoints.Count);
            return spawnPoints[randomIndex];
        }

        int randomWeight = Random.Range(1, totalWeight+1);
        int processedWeight = 0;
        foreach (var point in spawnPoints)
        {
            processedWeight += point.weight;
            if (randomWeight <= processedWeight)
            {
                return point;
            }
        }
        // should never return null
        return null;
    }

    public void ClearLandmarks()
    {
        while(m_landmarkPositions.Count > 0) {
            GameObject landmark = m_landmarkPositions[0].spawnedObject;
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
