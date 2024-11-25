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
using static LandmarkGenerator;


#if UNITY_EDITOR
using UnityEditor;
#endif

public class LandmarkGenerator : MonoBehaviour
{

    [System.Serializable]
    public class LandmarkGroupCounter {
        public List<Landmark> prefabs = new List<Landmark>();
        public int counter = 0;
        public Landmark GetNextPrefab() {
            if (prefabs.Count == 0) return null;
            Landmark prefab = prefabs[counter];
            counter++;
            if (counter >= prefabs.Count) counter = 0;
            return prefab;
        }
        public bool TryGetNextPrefab(out Landmark prefab) {
            if (prefabs.Count == 0) {
                prefab = null;
                return false;
            }

            prefab = prefabs[counter];
            counter++;
            if (counter >= prefabs.Count) counter = 0;
            return true;
        }
        public bool TryGetPrefabAtIndex(int index, out Landmark prefab) {
            if (index >= prefabs.Count) {
                prefab = null;
                return false;
            }
            prefab = prefabs[index];
            return true;
        }
    }

    [Header("=== References ===")]
    [SerializeField, Tooltip("Voronoi Map used in combinedMap")]                            private VoronoiMap m_voronoiMap;
    [SerializeField, Tooltip("Ref to the follow point control that dictates 1st or 3rd person camera stuff")]   private CameraFowllowPointControl m_camControl;
    [SerializeField, Tooltip("Parent for landmarks")]                                       private Transform m_landmarkParent;
    [Space]
    [SerializeField, Tooltip("Prefab for the destination landmark")]                        private Landmark m_destinationLandmark;
    [SerializeField, Tooltip("Prefabs for landmarks around the destination landmark")]      private Landmark m_surroundingDestinationLandmark;
    [SerializeField, Tooltip("Prefabs for landmarks at weenies")]                           private LandmarkGroupCounter m_weenieLandmarks;
    [SerializeField, Tooltip("Prefab for weenies")]                                         private List<Landmark> weeniePrefab;
    

    [Header("=== Generation Settings ===")]
    [SerializeField, Tooltip("Seed integer for landmark randomization")]                    private int m_seed;
    [SerializeField, Tooltip("Minimum distance between any 2 landmarks")]                   private float minimumDistanceBetweenLandmarks = 50f;
    [SerializeField, Tooltip("Maximum distance between 2 close landmarks")]                 private float maximumDistanceBetweenLandmarks = 70f;
    [SerializeField, Tooltip("Total number of landmarks to spawn")]                         private int landmarkCount = 6;
    [SerializeField, Tooltip("Total number of weenies between landmarks to spawn")]         private int weenieBetweenLandmarksCount = 3;
    [SerializeField, Tooltip("Padding to avoid spawning on edge of map"), Range(0f, 0.5f)]  private float edgePadding = 0.15f;
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
    [SerializeField, Tooltip("Height limit of landmark/weenie spawns")]                     private float spawnHeightLimit = 40f;
    [SerializeField, Tooltip("Total number of weenies to spawn")]                           private int weenieCount = 20;
    [SerializeField, Tooltip("Landmark placement offset amount")]                           private float landmarkPlacementOffset = 10f;
    [SerializeField, Tooltip("Weenie placement offset amount")]                             private float weeniePlacementOffset = 3f;


    [Header("=== Outputs - READ ONLY ===")]
    [SerializeField, Tooltip("List of landmark positions")]                                 private List<SpawnPoint> m_landmarkPositions;
    public List<SpawnPoint> landmarkPositions => m_landmarkPositions;
    [SerializeField, Tooltip("List of weenie positions")]                                   private List<Vector3> m_weeniePositions;
    public List<Vector3> weeniePositions => m_weeniePositions;
    [SerializeField, Tooltip("List of positions' distances to check when spawning weenie")] private List<Vector3> m_weenieCheckPositions;
    [SerializeField, Tooltip("The final list of landmarks generated.")]                     private List<Landmark> m_landmarks;

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

        public void InstantiateObject(Landmark prefab, Transform objectParent) 
        {
            this.spawnedObject = Instantiate(prefab.gameObject, this.location, Quaternion.identity, objectParent);
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

    private void Start() {
        StartCoroutine(SortLandmarks());
    }

    public void GenerateLandmarks(Vector3 destination, Vector3 startPosition, NoiseMap terrainMap)
    {
        // Check Flag: do we have a landmark prefab to begin with?
        if ( m_destinationLandmark == null) 
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
        m_weeniePositions = new List<Vector3>();
        m_landmarks = new List<Landmark>();

        if (m_voronoiMap != null)
        {
            // OFfset landmark by spawning it in 1 of 4 offsets with the highest height
            destination = OffsetPointByMaxHeight(destination, landmarkPlacementOffset, terrainMap);

            m_landmarkPositions.Add(new SpawnPoint(destination));
            m_weenieCheckPositions.Add(destination);
            
            GenerateRuinsAroundPoint(destination, terrainMap);

            // TODO: Replace this with path from path generation
            //List<Vector3> path = new List<Vector3>
            //{
            //    startPosition,
            //    destination
            //};
            
            destination.y += 4f;    // doing this to make raycasts shoot from top of landmark

            // First, spawn as many weenies visible from landmark as possible
            while (GenerateWeenieFromPoint(destination, terrainMap, x_size, z_size));

            //for (int i = 0; i < m_weeniePositions.Count; i++)
            //{

            //    bool weenieGenerated = GenerateWeenieFromPoint(m_weeniePositions[i], terrainMap, x_size, z_size);
            //    if (m_weeniePositions.Count >= weenieCount)
            //        break;
            //}

            // Try spawning weenies from other weenies
            for (int i = 0; i < m_weeniePositions.Count; i++)
            {
                if (GenerateWeenieFromPoint(m_weeniePositions[i], terrainMap, x_size, z_size))
                {
                    i--;
                    if (m_weeniePositions.Count >= weenieCount)
                        break;
                }
            }

            //Vector3 lastWeenieLocation = destination;
            //while (m_weeniePositions.Count < weenieCount)
            //{
            //    bool weenieGenerated = GenerateWeenieFromPoint(lastWeenieLocation, terrainMap, x_size, z_size);

            //    // no new weenie can be generated from this point
            //    if (!weenieGenerated)
            //    {
            //        if (lastWeenieLocation == destination)
            //            break;
            //        // start again from destination
            //        lastWeenieLocation = destination;
            //    }
            //    else
            //    {
            //        lastWeenieLocation = m_weeniePositions[^1];
            //    }
            //}

            // Spawn landmark at destination
            //m_landmarkPositions[0].InstantiateObject( m_destinationLandmark, m_landmarkParent);
            Landmark destinationLandmark = Instantiate(m_destinationLandmark, m_landmarkPositions[0].location, Quaternion.identity, m_landmarkParent);
            destinationLandmark.OffsetLandmarkInYaxis(terrainMap);

            // Spawn all the weenies
            for (int i = 0; i < m_weeniePositions.Count; i++)
            {
                var newPosition = OffsetPointByMaxHeight(m_weeniePositions[i], weeniePlacementOffset, terrainMap);
                if (m_weenieLandmarks.TryGetNextPrefab(out Landmark pre)) {
                    Landmark newWeenie = Instantiate(pre, newPosition, Quaternion.identity, m_landmarkParent) as Landmark;
                    m_landmarks.Add(newWeenie);
                    newWeenie.OffsetLandmarkInYaxis(terrainMap);
                }
            }

            // old weenie generation which spawns weenies based on path from start to destination
            //GenerateWeeniesInAllRegions(path, new List<int>(), terrainMap, x_size, z_size);
        }

        // Remove if need to spawn other landmarks
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
            m_landmarkPositions[0].InstantiateObject( m_destinationLandmark, m_landmarkParent);
            
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
        //if (m_placeLandmarkAtDest) m_landmarkPositions[0].InstantiateLandmark( m_destinationLandmark, m_landmarkParent);

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
        for(int i = 1; i < m_landmarkPositions.Count; i++) m_landmarkPositions[i].InstantiateObject( m_destinationLandmark, m_landmarkParent);

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

    // Generates a weenie from a point and returns whether a weenie was generated or not
    private bool GenerateWeenieFromPoint(Vector3 startPoint, NoiseMap terrainMap, float x_size, float z_size)
    {
        // Generates spawn points in a circle around startPoint
        startPoint.y += 2f;
        var possibleSpawnPoints = GenerateSpawnPoints(startPoint, m_weenieCheckPositions, minimumDistanceBetweenWeenies,
            terrainMap, x_size, z_size);

        if (possibleSpawnPoints.Count == 0)
            return false;

        var visionPoints = new List<Vector3>
        {
            startPoint
        };

        UpdatePointsBasedOnVisibility(ref possibleSpawnPoints, visionPoints);

        if (possibleSpawnPoints.Count == 0)
            return false;

        var nextPoint = ChooseSpawnPoint(possibleSpawnPoints);

        if (nextPoint != null)
        {
            m_weeniePositions.Add(nextPoint.location);
            m_weenieCheckPositions.Add(nextPoint.location);
            return true;
        }

        return false;
    }

    // Removes points that aren't visible from vision points
    private void UpdatePointsBasedOnVisibility(ref List<SpawnPoint> updatePoints, List<Vector3> visionPoints)
    {
        if (updatePoints == null)
            return;

        for (int i = 0; i < updatePoints.Count; i++)
        {
            int count = 0;
            updatePoints[i].location.y += 2f;
            foreach (var visionPoint in visionPoints)
            {
                float distance = Vector3.Distance(updatePoints[i].location, visionPoint);

                bool hit = Physics.Raycast(visionPoint, updatePoints[i].location - visionPoint, out RaycastHit raycastHit, distance + 2f);
                if (hit)
                {
                    if (raycastHit.distance >= distance)
                    {
                        //Debug.DrawRay(visionPoint, updatePoints[i].location - visionPoint, Color.red, 120f);
                        count++;
                    }
                }
                else
                {
                    //Debug.DrawRay(visionPoint, updatePoints[i].location - visionPoint, Color.red, 120f);
                    count++;
                }
            }

            if (count == 0)
            {
                updatePoints.RemoveAt(i);
                i--;
            }
            else
            {
                updatePoints[i].location.y -= 1f;
            }
        }
    }

    private Vector3 OffsetPointByMaxHeight(Vector3 point, float offsetAmount, NoiseMap terrainMap)
    {
        Vector3[] offsetSpawns =
        {
            point, point, point, point,
            point, point, point, point
        };
        int maxIndex = -1;
        float maxHeight = int.MinValue;

        offsetSpawns[0].x += offsetAmount;
        offsetSpawns[1].x -= offsetAmount;
        offsetSpawns[2].z += offsetAmount;
        offsetSpawns[3].z -= offsetAmount;
        offsetSpawns[4].x += offsetAmount;
        offsetSpawns[4].z += offsetAmount;
        offsetSpawns[5].x += offsetAmount;
        offsetSpawns[5].z -= offsetAmount;
        offsetSpawns[6].x -= offsetAmount;
        offsetSpawns[6].z += offsetAmount;
        offsetSpawns[7].x -= offsetAmount;
        offsetSpawns[7].z -= offsetAmount;

        for (int i = 0; i < 8; i++)
        {
            float height =
                terrainMap.QueryHeightAtWorldPos(offsetSpawns[i].x, offsetSpawns[i].z, out int x, out int y);
            if (height > maxHeight)
            {
                maxHeight = height;
                maxIndex = i;
            }
        }

        offsetSpawns[maxIndex].y = maxHeight;
        return offsetSpawns[maxIndex];
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
            if (m_weenieLandmarks.TryGetNextPrefab(out Landmark pre)) {
                Landmark newWeenie = Instantiate(pre, spawnPoint, Quaternion.identity, m_landmarkParent) as Landmark;
                m_landmarks.Add(newWeenie);
                newWeenie.OffsetLandmarkInYaxis(terrainMap);
            }
        }

        var possibleSpawnPoints = GeneratePointsNormalToPoints(pathPoints, visionPoints, terrainMap);
        UpdateWeightsBasedOnVisibility(ref possibleSpawnPoints, visionPoints);

        possibleSpawnPoints.Sort();

        for (int i = 0; i < possibleSpawnPoints.Count; i++)
        {
            SpawnPoint nextPoint = possibleSpawnPoints[i];
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
            if (m_weenieLandmarks.TryGetNextPrefab(out Landmark pre)) {
                Landmark newWeenie = Instantiate(pre, spawnPoint, Quaternion.identity, m_landmarkParent) as Landmark;
                m_landmarks.Add(newWeenie);
                newWeenie.OffsetLandmarkInYaxis(terrainMap);
            }
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

    private void UpdateWeightsBasedOnVisibility(ref List<SpawnPoint> updatePoints, List<Vector3> visionPoints)
    {
        if (updatePoints == null)
            return;

        foreach (var spawnPoint in updatePoints)
        {
            int count = 0;
            spawnPoint.location.y += 1f;
            foreach (var visionPoint in visionPoints)
            {
                float distance = Vector3.Distance(spawnPoint.location, visionPoint);

                bool hit = Physics.Raycast(visionPoint, spawnPoint.location - visionPoint, out RaycastHit raycastHit, distance + 2f);
                if (hit)
                {
                    if (raycastHit.distance >= distance)
                    {
                        Debug.DrawRay(visionPoint, spawnPoint.location - visionPoint, Color.red, 120f);
                        count++;
                    }
                }
                else
                {
                    Debug.DrawRay(visionPoint, spawnPoint.location - visionPoint, Color.red, 120f);
                    count++;
                }
            }
            spawnPoint.location.y -= 1f;
            spawnPoint.weight = count;
        }
    }

    private void GenerateWeeniesBetweenPoints(Vector3 point1, Vector3 point2, int weenieCount, NoiseMap terrainMap)
    {
        // Check Flag: do we have a landmark prefab to begin with?
        if (m_weenieLandmarks.prefabs.Count == 0)
        {
            Debug.LogError("Weenie landmark prefabs not assigned to 'weenieLandmarks' in editor!");
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

            if (m_weenieLandmarks.TryGetPrefabAtIndex(regionIndex, out Landmark pre))
            {
                Debug.Log("Printing via generating between points");
                Landmark newWeenie = Instantiate(pre, spawnPoint, Quaternion.identity, m_landmarkParent);
                newWeenie.OffsetLandmarkInYaxis(terrainMap);
            }
        }
    }

    private void GenerateRuinsAroundPoint(Vector3 point, NoiseMap terrainMap)
    {
        List<Vector3> ruinSpawnPoints = new List<Vector3>();
        m_destinationLandmark.CalculateBounds();
        m_surroundingDestinationLandmark.CalculateBounds();

        float landmarkSize =  m_destinationLandmark.bounds.extents.magnitude;
        float ruinSize = m_surroundingDestinationLandmark.bounds.extents.magnitude;

        for (int i = 0; i < endRegionRuinCount; i++)
        {
            double r = landmarkSize + (endRegionRadius * Math.Sqrt(Random.value));
            double theta = Random.value * 2 * Math.PI;

            double x = point.x + (r * Math.Cos(theta));
            double z = point.z + (r * Math.Sin(theta));

            bool validPoint = true;

            foreach (var ruinSpawn in ruinSpawnPoints)
            {
                // makes sure new point is not too close to other ruins
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

            float y = terrainMap.QueryHeightAtWorldPos((float)x, (float)z, out int gridX, out int gridY);
            ruinSpawnPoints.Add(new Vector3((float)x, y, (float)z));
        }

        foreach (var spawnPoint in ruinSpawnPoints)
        {
            Landmark newRuin = Instantiate(m_surroundingDestinationLandmark, spawnPoint, Quaternion.identity, m_landmarkParent);
            newRuin.OffsetLandmarkInYaxis(terrainMap);
        }
    }

    // Generates spawn points in a circle around a location
    private List<SpawnPoint> GenerateSpawnPoints(Vector3 location, List<Vector3> checkPoints, float minimumDistance, NoiseMap terrainMap, float x_size, float z_size)
    {
        float spawnBorderPadding = x_size * edgePadding * 2;
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
                float yNoise = terrainMap.QueryNoiseAtWorldPos((float)x, (float)z, out gridX, out gridY);

                // If new point is not too high up
                if (y <= spawnHeightLimit)
                {
                    Vector3 newPoint = new Vector3((float)x, y, (float)z);

                    // If point is not too close to other checkpoints, add it to list
                    if (CheckDistanceFromPoints(newPoint, checkPoints, minimumDistance, out float totalDistance))
                    {
                        spawnLocations.Add(new SpawnPoint(newPoint, (int)totalDistance));
                        //Debug.DrawRay(location,  newPoint-location, Color.blue, 120f);
                    }
                    else
                    {
                        //Debug.DrawRay(location, newPoint - location, Color.grey, 120f);
                    }
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

    // Chooses next point based on weight
    private SpawnPoint ChooseSpawnPoint(List<SpawnPoint> spawnPoints)
    {
        if (spawnPoints.Count == 1)
        {
            if (spawnPoints[0].weight == 0)
                return null;
            return spawnPoints[0];
        }

        // TODO: Changed to only return max. Fix this to make it random again
        int totalWeight = 0;
        int minWeight = int.MaxValue;
        int maxWeight = int.MinValue;
        SpawnPoint maxPoint = null;

        foreach (var point in spawnPoints) 
        {
            totalWeight += point.weight;
            if(point.weight < minWeight)
                minWeight = point.weight;

            if (point.weight > maxWeight)
            {
                maxWeight = point.weight;
                maxPoint = point;
            }
        }

        //return maxPoint;

        if (totalWeight == 0)
        {
            int randomIndex = Random.Range(0, spawnPoints.Count);
            return spawnPoints[randomIndex];
        }

        minWeight -= 1;
        totalWeight = totalWeight - (minWeight * spawnPoints.Count);

        int randomWeight = Random.Range(1, totalWeight+1);
        int processedWeight = 0;
        foreach (var point in spawnPoints)
        {
            processedWeight += point.weight - minWeight;
            if (randomWeight <= processedWeight)
            {
                return point;
            }
        }
        // only null when all weights are 0
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

    private IEnumerator SortLandmarks() {
        WaitForSeconds sortDelay = new WaitForSeconds(0.5f);
        while(true) {
            if (!m_camControl.firstPersonCameraActive) {
                yield return sortDelay;
                continue;
            }
            if (m_landmarks.Count <= 1) {
                yield return sortDelay;
                continue;
            }
            List<Landmark> sorted = m_landmarks.OrderBy(a => a.inFustrum ? 0 : 1).ThenBy(a => a.distToCamCenter).ToList();
            m_landmarks = sorted;
            yield return sortDelay;
        }   
    }

    private void OnDisable() {
        StopAllCoroutines();
    }
}
