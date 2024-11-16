using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class FindShortestPath : MonoBehaviour
{
    [Header("=== References ===")]
    [SerializeField] private NoiseMap m_terrainGenerator;
    [SerializeField] private VoronoiMap m_voronoiMap;
    [SerializeField] private NavMeshSurface m_navMeshSurface;

    [Header("=== Settings ===")]
    [SerializeField] private float m_pathPointDiscretization = 10f;
    [SerializeField] private bool m_autoUpdate = false;
    public bool autoUpdate => m_autoUpdate;

    [Header("=== Debug ===")]
    [SerializeField] private Transform m_startRef;
    [SerializeField] private Transform m_endRef;
    [SerializeField] private Vector3[] m_pathCorners;
    [SerializeField] private Vector3[] m_pathPositions;

    #if UNITY_EDITOR
    void OnDrawGizmos() {
        if (m_pathCorners != null && m_pathCorners.Length > 0) {
            Gizmos.color = Color.red;
            for(int i = 0; i < m_pathCorners.Length; i++) Gizmos.DrawSphere(m_pathCorners[i], 0.75f);
        }
        if (m_pathPositions != null && m_pathPositions.Length > 0) {
            Gizmos.color = Color.blue;
            for(int i = 0; i < m_pathPositions.Length-1; i++) {
                Gizmos.DrawSphere(m_pathPositions[i], 0.5f);
                Gizmos.DrawLine(m_pathPositions[i], m_pathPositions[i+1]);
            }
            Gizmos.DrawSphere(m_pathPositions[m_pathPositions.Length-1], 0.5f);
        }
    }
    #endif

    public void GenerateShortestPath() {
        if (m_terrainGenerator == null) {
            Debug.LogError("Cannot generate shorteset path with missing terrain generator");
            return;
        }
        if (m_navMeshSurface == null) {
            Debug.LogError("Cannot generate shorteset path with missing nav mesh surface");
            return;
        }
        if (m_startRef == null || m_endRef == null) {
            Debug.LogError("Missing references for start and end");
            return;
        }

        CalculatePath(m_startRef.position, m_endRef.position, true, true, out List<Vector3> points, out List<int> indices);
    }

    public bool GenerateShortestPath(Vector3 start, Vector3 end) {
        if (m_terrainGenerator == null) {
            Debug.LogError("Cannot generate shorteset path with missing terrain generator");
            return false;
        }
        if (m_navMeshSurface == null) {
            Debug.LogError("Cannot generate shorteset path with missing nav mesh surface");
            return false;
        }

        return CalculatePath(start, end, true, true, out List<Vector3> points, out List<int> indices);
    }

    public bool CalculatePath(Vector3 start, Vector3 end, bool generateMap, bool buildMesh, out List<Vector3> points, out List<int> segmentIndices) {
        // First, generate the map
        if (generateMap) m_terrainGenerator.GenerateMap();
        // Second, ask the nav mesh surface to bake
        if (buildMesh) m_navMeshSurface.BuildNavMesh();

        // Initialize some variables
        NavMeshPath navPath = new NavMeshPath();
        points = new List<Vector3>();
        segmentIndices = new List<int>();
        int prevSegmentIndex = -1;

        float startY = m_terrainGenerator.QueryHeightAtWorldPos(start.x, start.z, out int startCoordX, out int startCoordZ);
        float endY = m_terrainGenerator.QueryHeightAtWorldPos(end.x, end.z, out int endCoordX, out int endCoordZ);

        // Next, use NavMesh to predict an optimal path. Return early if path not found
        GetClosestPointOnMesh(new Vector3(start.x, startY, start.z), out Vector3 meshStart);
        GetClosestPointOnMesh(new Vector3(end.x, endY, end.z), out Vector3 meshEnd);
        bool pathFound = NavMesh.CalculatePath(meshStart, meshEnd, NavMesh.AllAreas, navPath);
        Debug.Log($"Path Found: {pathFound.ToString()}");
        if (!pathFound) return false;
        
        // Given this, can we generate a new set of path points based on the corners with some discretization to create "in-between" points?
        for(int i = 0; i < navPath.corners.Length-1; i++) {
            Vector3 displacement = navPath.corners[i+1] - navPath.corners[i];
            float totalDistance = displacement.magnitude;
            Vector3 direction = displacement.normalized;
            
            // How many points will we experience?
            int numIterations = Mathf.FloorToInt(totalDistance/m_pathPointDiscretization);
            
            // Iterate, add points as they come
            for(int k = 0; k <= numIterations; k++) {
                Vector3 pos = navPath.corners[i] + direction * k * m_pathPointDiscretization;
                // Query what point this corner point is
                float h = m_terrainGenerator.QueryHeightAtWorldPos(pos.x, pos.z, out int x, out int z);
                points.Add(new Vector3(pos.x, h, pos.z));
                // Check what region index this is in
                int segmentIndex = m_voronoiMap.QueryVoronoiSegmentAtWorldPos(pos.x, pos.z, out x, out z);
                if (prevSegmentIndex != segmentIndex) {
                    Debug.Log($"Moving to segment {segmentIndex+1}");
                    segmentIndices.Add(segmentIndex);
                    prevSegmentIndex = segmentIndex;
                }

            }
        }

        m_pathCorners = navPath.corners;
        m_pathPositions = points.ToArray();

        Debug.Log(segmentIndices.Count);
        return true;
    }

    public void BuildNavMesh() { 
        if (m_navMeshSurface == null) {
            Debug.LogError("Cannot bake nav mesh with unbaked navmesh");
            return;
        }
        m_navMeshSurface.BuildNavMesh(); 
    } 

    public bool GetClosestPointOnMesh(Vector3 query, out Vector3 closest, float queryRange=10f) {
        NavMeshHit hit;
		if (NavMesh.SamplePosition(query, out hit, queryRange, NavMesh.AllAreas)) {
			closest = hit.position;
			return true;
		}
        closest = query;
        return false;
    }
}
