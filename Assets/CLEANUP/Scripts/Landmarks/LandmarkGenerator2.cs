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
using System.IO;
using System.Linq;
using UnityEngine.Events;
using static LandmarkGenerator;
using UnityEngine.TerrainUtils;
#if UNITY_EDITOR
using static UnityEditor.Experimental.AssetDatabaseExperimental.AssetDatabaseCounters;
using UnityEditor;
#endif

public class LandmarkGenerator2 : MonoBehaviour
{
    public static LandmarkGenerator2 current;

    [System.Serializable]
    public class SpawnPoint : IComparable<SpawnPoint> {
        public Vector3 location;
        public int weight;
        public GameObject spawnedObject;
        public SpawnPoint(Vector3 location) {
            this.location = location;
        }
        public SpawnPoint(Vector3 location, int weight) {
            this.location = location;
            this.weight = weight;
        }

        public int CompareTo([AllowNull] SpawnPoint otherPoint) {
            return this.weight.CompareTo(otherPoint.weight);
        }
    }

    [System.Serializable]
    public class PathBetweenLandmarks : IComparable<PathBetweenLandmarks>
    {
        public Landmark landmark1, landmark2;
        public float distance;
        public List<Vector3> generatedPath;

        public PathBetweenLandmarks(Landmark landmark1, Landmark landmark2)
        {
            this.landmark1 = landmark1;
            this.landmark2 = landmark2;
        }

        public PathBetweenLandmarks(Landmark landmark1, Landmark landmark2, float distance)
        {
            this.landmark1 = landmark1;
            this.landmark2 = landmark2;
            this.distance = distance;
        }

        public PathBetweenLandmarks(Landmark landmark1, Landmark landmark2, float distance, List<Vector3> generatedPath)
        {
            this.landmark1 = landmark1;
            this.landmark2 = landmark2;
            this.distance = distance;
            this.generatedPath = generatedPath;
        }
        public int CompareTo([AllowNull] PathBetweenLandmarks otherPath)
        {
            return this.distance.CompareTo(otherPath.distance);
        }
    }

    [Header("=== Generator Settings ===")]
    [SerializeField, Tooltip("Seed integer for landmark randomization")]    private int m_seed;
    public int seed => m_seed;
    [SerializeField, Tooltip("Parent for landmarks")]                       private Transform m_landmarkParent;
    private System.Random m_prng;

    [Header("=== Landmark Prefabs ===")]
    [SerializeField, Tooltip("Prefab for the arch object")]                                private Landmark m_archPrefab;
    [SerializeField, Tooltip("Prefab for the King bell tower object")]                     private Landmark m_kingBellTowerPrefab;
    [SerializeField, Tooltip("Prefab for the major region bell tower object")]             private Landmark m_majorBellTowerPrefab;
    [SerializeField, Tooltip("Prefab for the minor region bell tower object")]             private Landmark m_minorBellTowerPrefab;

    [Header("=== Generation Settings ===")]
    [SerializeField, Tooltip("Weenie path (normal based) offset amount")]                  private float weeniePathMaxOffset = 10f;
    [SerializeField, Tooltip("Minimum distance between any 2 arches")]                     private float minimumDistanceBetweenArches = 40f;
    [SerializeField, Tooltip("Number of arches per path")]                                 private int archCountPerPath = 5;
    [SerializeField, Tooltip("Minimum points for region to be considered minor")]          private int minimumPointsInMinorRegion = 5;

    [Header("=== Outputs - READ ONLY ===")]
    [SerializeField, Tooltip("The final list of landmarks generated.")]                    private List<Landmark> m_landmarks;
    public List<Landmark> landmarks => m_landmarks;
    [SerializeField] private bool m_generated = false;
    public bool generated => m_generated;
    public UnityEvent onGenerationEnd;

    private void Awake() {
        current = this;
        if (m_landmarkParent == null) m_landmarkParent = this.transform;
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

    public void Generate() {
        // Check if both voronoi and terrain map are generated, then we can proceed. However, if we have neither, then we cannot do anything
        if (TerrainManager.current == null || !TerrainManager.current.generated) {
            Debug.Log("Landmark Generator 2: Cannot start generating landmarks until terrian is generated.");
            return;
        }
        if (Voronoi.current == null || !Voronoi.current.generated) {
            Debug.Log("Landmark Generator 2: Cannot start generating landmarks until voronoi is generated.");
            return;
        }

        StartCoroutine(GenerateCoroutine());
    }

    private IEnumerator GenerateCoroutine() {
        // Initialize random number generator
        m_prng = new System.Random(m_seed);

        m_landmarks = new List<Landmark>();

        // King bell tower in grasslands region
        TerrainManager.current.TryGetPointOnTerrain(Voronoi.current.regions[0].coreCluster.centroid, out var terrainPoint,
            out var normal, out var steepness);
        Vector3 kingDirection = new Vector3(normal.x, 0, normal.z);
        Quaternion kingRotation = (kingDirection.magnitude == 0) 
            ? Quaternion.Euler(0f, m_prng.Next(0,360), 0f)
            : Quaternion.LookRotation(kingDirection.normalized, Vector3.up);
        Landmark kingTower = InstantiateLandmark(m_kingBellTowerPrefab, terrainPoint, kingRotation, true, 50f);
        kingTower.regionIndex = 0;
        Voronoi.current.regions[0].towerLandmark = kingTower;

        Debug.Log("Generated Grasslands major landmark");
        yield return null;

        // Major bell towers in other major regions' core clusters
        for (int i = 1; i < Voronoi.current.regions.Count; i++) {
            TerrainManager.current.TryGetPointOnTerrain(Voronoi.current.regions[i].coreCluster.centroid, out terrainPoint,
                out normal, out steepness);
            Vector3 majorDirection = new Vector3(normal.x, 0, normal.z);
            Quaternion majorRotation = (majorDirection.magnitude == 0) 
                ? Quaternion.Euler(0f, m_prng.Next(0,360), 0f)
                : Quaternion.LookRotation(majorDirection.normalized, Vector3.up);
            Landmark bellTower = InstantiateLandmark(m_majorBellTowerPrefab, terrainPoint, majorRotation, true, 50f);
            bellTower.regionIndex = i;
            Voronoi.current.regions[i].towerLandmark = bellTower;
        }

        Debug.Log("Generated other regions' major landmark");
        yield return null;

        // Minor bell towers in the regions' sub-clusters
        for (int i = 0; i < Voronoi.current.regions.Count; i++) {
            foreach (var cluster in Voronoi.current.regions[i].subClusters) {
                if (cluster.centroids.Count >= minimumPointsInMinorRegion) {
                    TerrainManager.current.TryGetPointOnTerrain(cluster.centroid, out terrainPoint, out normal,
                        out steepness);
                    Vector3 minorDirection = new Vector3(normal.x, 0, normal.z);
                    Quaternion minorRotation = (minorDirection.magnitude == 0) 
                        ? Quaternion.Euler(0f, m_prng.Next(0,360), 0f)
                        : Quaternion.LookRotation(minorDirection.normalized, Vector3.up);
                    Landmark minorLandmark = InstantiateLandmark(m_minorBellTowerPrefab, terrainPoint, minorRotation, true, 50f);
                    minorLandmark.regionIndex = i;
                    Voronoi.current.regions[i].minorLandmarks.Add(minorLandmark);

                    yield return null;
                }
            }
        }

        Debug.Log("Generated minor bell towers");

        var generatedPaths = GeneratePathsBetweenLandmarks();

        Debug.Log("Generated paths between landmarks");

        if (m_archPrefab != null) {
            Debug.Log("Generating arches along pathways");
            List<Vector3> archPositions = new List<Vector3>();
            // Spawn arches in the paths generated
            foreach (var path in generatedPaths) {
                int pointsInEachSegment = path.generatedPath.Count / (archCountPerPath + 1);
                for (int i = 1; i <= archCountPerPath; i++) {
                    // if too close to other arches, continue
                    if (!CheckDistanceFromPoints(path.generatedPath[pointsInEachSegment * i], archPositions,
                            minimumDistanceBetweenArches,
                            out float totalDistance))
                        continue;

                    Vector3 direction = path.generatedPath[(pointsInEachSegment * i) + 1] - path.generatedPath[(pointsInEachSegment * i)];
                    direction.Normalize();
                    InstantiateLandmark(m_archPrefab, path.generatedPath[pointsInEachSegment * i], Quaternion.LookRotation(direction, Vector3.up), false, 40f);
                    archPositions.Add(path.generatedPath[pointsInEachSegment * i]);
                    yield return null;
                }
            }
        }

        Debug.Log("Landmark Generation Completed");
        m_generated = true;
        onGenerationEnd?.Invoke();
    }

    public Landmark InstantiateLandmark(Landmark prefab, Vector3 pos, Quaternion rot, bool addToLandmarks = true, float treeRemovalRadius = 0f) {
        // Instantiate the landmark itself, given the prefab, position, and rotation
        Landmark newWeenie = Instantiate(prefab, pos, rot, m_landmarkParent) as Landmark;
        if (addToLandmarks) m_landmarks.Add(newWeenie);
        if (treeRemovalRadius > 0f && VegetationGenerator2.current != null) VegetationGenerator2.current.DeactivateTreesInRadius(pos, treeRemovalRadius);
        return newWeenie;
    }

    // Returns false if point is too close to checkPoints and outs the total distance from checkPoints if point is valid
    private bool CheckDistanceFromPoints(Vector3 point, List<Vector3> checkPoints, float minimumDistance, out float totalDistance) {
        bool isValidPoint = true;
        totalDistance = 0f;

        // checks distance of new point from other checkpoints
        foreach (var checkPoint in checkPoints) {
            float distance = Vector3.Distance(point, checkPoint);

            // if the new point is too close to any checkpoint, discard it
            if (distance < minimumDistance) {
                isValidPoint = false;
                break;
            }
            totalDistance += distance;
        }

        return isValidPoint;
    }

    // Uses kruskal's algorithm to generate a MST that connects all landmarks. Then generates paths based on normals
    private List<PathBetweenLandmarks> GeneratePathsBetweenLandmarks() 
    {
        List<PathBetweenLandmarks> generatedPaths = new List<PathBetweenLandmarks>();
        List<PathBetweenLandmarks> possiblePaths = new List<PathBetweenLandmarks>();

        HashSet<HashSet<Landmark>> landmarkSets = new HashSet<HashSet<Landmark>>();

        foreach (var landmark in m_landmarks)
        {
            landmarkSets.Add(new HashSet<Landmark> { landmark });
            var closeLandmarkList = GetClosestLandmarksList(landmark.transform.position);
            foreach (var landmarkDistanceTuple in closeLandmarkList)
            {
                possiblePaths.Add(new PathBetweenLandmarks(landmark, landmarkDistanceTuple.Item1, landmarkDistanceTuple.Item2));
            }
        }

        possiblePaths = possiblePaths.OrderBy(i => i.distance).ToList();

        foreach (var path in possiblePaths)
        {
            HashSet<Landmark> set1 = null;
            HashSet<Landmark> set2 = null;
            foreach (var set in landmarkSets)
            {
                if (set.Contains(path.landmark1)) set1 = set;
                if (set.Contains(path.landmark2)) set2 = set;
                if (set1 != null && set2 != null) break;
            }

            if (set1 == set2) continue;

            var reversedPath = new PathBetweenLandmarks(path.landmark2, path.landmark1, path.distance);

            path.generatedPath = GeneratePathBetweenPoints(path.landmark1.transform.position, path.landmark2.transform.position);
            reversedPath.generatedPath = new List<Vector3>(path.generatedPath);
            reversedPath.generatedPath.Reverse();

            path.landmark1.m_pathsToOtherLandmarks.Add(path);
            path.landmark2.m_pathsToOtherLandmarks.Add(reversedPath);

            if (path.landmark1.transform.position.y > path.landmark2.transform.position.y)
                generatedPaths.Add(path);
            else
                generatedPaths.Add(reversedPath);

            if (generatedPaths.Count == m_landmarks.Count - 1)
                break;

            landmarkSets.Remove(set1);
            landmarkSets.Remove(set2);

            foreach (var point in set2) set1.Add(point);

            landmarkSets.Add(set1);
        }

        return generatedPaths;
    }

    private List<(Landmark, float)> GetClosestLandmarksList(Vector3 position) {
        List<(Landmark, float)> closeLandmarks = new List<(Landmark, float)>();
        foreach (var landmark in m_landmarks) {
            if (landmark.transform.position == position)    continue;
            float distance = Vector3.Distance(position, landmark.transform.position);
            closeLandmarks.Add((landmark, distance));
        }

        closeLandmarks.Sort((x, y) => x.Item2.CompareTo(y.Item2));
        return closeLandmarks;
    }

    private List<Vector3> GeneratePathBetweenPoints(Vector3 point1, Vector3 point2) {
        List<Vector3> pointsInPath = new List<Vector3>();

        Vector2 point1v2 = new Vector2(point1.x, point1.z);
        Vector2 point2v2 = new Vector2(point2.x, point2.z);
        Vector2 directionv2 = point2v2 - point1v2;
        directionv2.Normalize();

        float distance = Vector2.Distance(point1v2, point2v2);
        Vector2 lastPointv2 = point1v2;
        Vector3 lastPoint = point1;

        while (distance >= 4f) {
            Vector2 spawnPointv2 = lastPointv2 + (directionv2 * 2f);

            TerrainManager.current.TryGetPointOnTerrain(spawnPointv2.x, spawnPointv2.y, out var spawnPoint,
                out var normal, out var steepness);

            directionv2 = point2v2 - spawnPointv2;
            directionv2.Normalize();

            Vector3 offsetDirection = normal - (Vector3.Dot(normal, Vector3.up) * Vector3.up);
            Vector2 offsetV2 = new Vector2(offsetDirection.x, offsetDirection.z);


            float angle = Vector2.Angle(directionv2, offsetV2);
            spawnPointv2 += weeniePathMaxOffset * MathF.Sin((angle * Mathf.PI) / 180) * offsetV2;

            TerrainManager.current.TryGetPointOnTerrain(spawnPointv2.x, spawnPointv2.y, out spawnPoint,
                out normal, out steepness);

            pointsInPath.Add(spawnPoint);
            //Debug.DrawLine(lastPoint, spawnPoint, lastPoint == point1 ? Color.blue : Color.red, 3000f, false);

            directionv2 = point2v2 - spawnPointv2;
            directionv2.Normalize();

            lastPointv2 = spawnPointv2;
            lastPoint = spawnPoint;
            distance = Vector2.Distance(spawnPointv2, point2v2);
        }

        return pointsInPath;
    }

    private void OnDisable() {
        StopAllCoroutines();
    }

    // =============================================================================
}
