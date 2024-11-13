using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SimulateCloth : MonoBehaviour
{
    public float springStrength = 50f;
    public float damping = 1f;
    public float maxStretch = 1.1f;
    private Mesh mesh;
    private Vector3[] originalVertices;
    private GameObject[] points;
    private Rigidbody[] rbs;
    public GameObject samplePoint;
    
    
    // Start is called before the first frame update
    void Start()
    {
        // get model's mesh
        mesh = GetComponent<MeshFilter>().mesh;
        originalVertices = mesh.vertices;
        
        points = new GameObject[originalVertices.Length];
        rbs = new Rigidbody[originalVertices.Length];
        for (int i = 0; i < originalVertices.Length; i++)  // create gameObject at each vertex
        {
            // GameObject point = new GameObject($"Point_{i}");
            GameObject point = Instantiate(samplePoint);
            point.transform.SetParent(this.transform);
            point.name = "point" + i;
            point.transform.position = transform.TransformPoint(originalVertices[i]);
            
            Rigidbody rb = point.AddComponent<Rigidbody>();     // add Rigidbody
            if (fixedPointsId.Contains(i))
            {
                rb.isKinematic = true;
            }
            // if (endPointsId.Contains(i))
            // {
            //     rb.useGravity = true;
            // }
            // else
            // {
            //     rb.useGravity = false;
            // }
            rb.freezeRotation = true;
            rb.mass = 0.001f;
            
            points[i] = point;
            rbs[i] = rb;
        }
        
        //record original vertical distance
        for (int i = 2; i < 13; i++)
        {
            for (int j = 0; j < 7; j++)
            {
                distanceMatrix[i, j] = (points[meshVertexMatrix[i, j]].transform.position -
                                        points[meshVertexMatrix[i - 1, j]].transform.position).magnitude;
            }
        }

        // for (int i = 1; i < 13; i++)
        // {
        //     for (int j = 1; j < 4; j++)
        //     {
        //         distanceMatrixHorizontal[i, 3 + j] = (points[meshVertexMatrix[i, 2 + j]].transform.position -
        //                                 points[meshVertexMatrix[i, 3 + j]].transform.position).magnitude;
        //         distanceMatrixHorizontal[i, 3 - j] = (points[meshVertexMatrix[i, 4 - j]].transform.position -
        //                                    points[meshVertexMatrix[i, 3 - j]].transform.position).magnitude;
        //     }
        // }
        
        // create springs
        for (int i = 2; i <= 12; i++)
        {
            for (int j = 0; j <= 5; j++)
            {
                AddSpring(meshVertexMatrix[i, j], meshVertexMatrix[i, j + 1]);
            }
        }
        for (int i = 1; i <= 11; i++)
        {
            for (int j = 0; j <= 6; j++)
            {
                AddSpring(meshVertexMatrix[i, j], meshVertexMatrix[i + 1, j]);
            }
        }
        for (int i = 2; i <= 12; i++)
        {
            for (int j = 0; j <= 4; j++)
            {
                AddSpring(meshVertexMatrix[i, j], meshVertexMatrix[i, j + 2]);
            }
        }
        for (int i = 1; i <= 10; i++)
        {
            for (int j = 0; j <= 6; j++)
            {
                AddSpring(meshVertexMatrix[i, j], meshVertexMatrix[i + 2, j]);
            }
        }

        for (int i = 1; i <= 11; i = i + 2)
        {
            for (int j = 1; j <= 5; j = j + 2)
            {
                if (i > 1)
                {
                    AddSpring(meshVertexMatrix[i, j], meshVertexMatrix[i - 1, j - 1]); 
                    AddSpring(meshVertexMatrix[i, j], meshVertexMatrix[i - 1, j + 1]);
                }
                AddSpring(meshVertexMatrix[i, j], meshVertexMatrix[i + 1, j - 1]);
                AddSpring(meshVertexMatrix[i, j], meshVertexMatrix[i + 1, j + 1]);
            }
        }
        // AddSpring(meshVertexMatrix[12, 0], meshVertexMatrix[12, 6]);
        
        
        // expand renderer bound
        var meshRenderer = GetComponent<MeshRenderer>();
        var meshRendererBound = meshRenderer.bounds;
        meshRendererBound.Expand(99);
        meshRenderer.bounds = meshRendererBound;
    }

    // Update is called once per frame
    void Update()
    {
        // rebind mesh vertex
        Vector3[] vertices = new Vector3[originalVertices.Length];
        for (int i = 0; i < points.Length; i++)
        {
            vertices[i] = transform.InverseTransformPoint(points[i].transform.position);
        }
        mesh.vertices = vertices;
        mesh.RecalculateNormals();
    }

    private void FixedUpdate()
    {
        // restrain vertical distance
        for (int i = 2; i < 13; i++)
        {
            for (int j = 0; j < 7; j++)
            {
                int p1 = meshVertexMatrix[i, j], p2 = meshVertexMatrix[i - 1, j];
                if ((points[p1].transform.position - points[p2].transform.position).magnitude >
                    distanceMatrix[i, j] * maxStretch)
                {
                    Vector3 direction = (points[p1].transform.position - points[p2].transform.position).normalized;
                    points[p1].transform.position = points[p2].transform.position + distanceMatrix[i, j] * maxStretch * direction;
                }
            }
        }
        
        // restrain horizontal distance
        // for (int i = 1; i < 13; i++)
        // {
        //     for (int j = 1; j < 4; j++)
        //     {
        //         int p1 = meshVertexMatrix[i, 3 + j], p2 = meshVertexMatrix[i, 2 + j];
        //         if ((points[p1].transform.position - points[p2].transform.position).magnitude >
        //             distanceMatrixHorizontal[i, 3 + j] * maxStretch)
        //         {
        //             Vector3 direction = (points[p1].transform.position - points[p2].transform.position).normalized;
        //             points[p1].transform.position = points[p2].transform.position +
        //                                             distanceMatrix[i, 3 + j] * maxStretch * direction;
        //         }
        //
        //         int p3 = meshVertexMatrix[i, 3 - j], p4 = meshVertexMatrix[i, 4 - j];
        //         if ((points[p3].transform.position - points[p4].transform.position).magnitude >
        //             distanceMatrixHorizontal[i, 3 - j] * maxStretch)
        //         {
        //             Vector3 direction = (points[p3].transform.position - points[p4].transform.position).normalized;
        //             points[p3].transform.position = points[p4].transform.position +
        //                                             distanceMatrix[i, 3 - j] * maxStretch * direction;
        //         }
        //     }
        // }
    }
    
    private void AddSpring(int vertexId1, int vertexId2)
    {
        SpringJoint spring = points[vertexId1].AddComponent<SpringJoint>();
        spring.connectedBody = points[vertexId2].GetComponent<Rigidbody>();
        spring.spring = springStrength;
        spring.damper = damping;
        spring.autoConfigureConnectedAnchor = false;
        spring.connectedAnchor = Vector3.zero;
        spring.anchor = Vector3.zero;
        float distance = (points[vertexId1].transform.position - points[vertexId2].transform.position)
                                     .magnitude;
        spring.minDistance = distance * 0.8f;
        spring.maxDistance = distance;
    }

    public void AddForce(Vector3 force)
    {
        force /= meshVertexMatrix.Length;
        foreach (var id in meshVertexMatrix)
        {
            rbs[id].AddForce(force, ForceMode.Force);
        }
    }

    private int[,] meshVertexMatrix =
    {
        { 88, 89, 85, 84, 78, 75, 74 },
        { 81, 87, 90, 83, 77, 73, 66 },
        { 72, 80, 86, 82, 76, 65, 60 },
        { 69, 71, 79, 64, 67, 59, 55 },
        { 31, 68, 70, 63, 62, 54, 51 },
        { 24, 30, 61, 58, 57, 50, 46 },
        { 18, 23, 56, 52, 53, 45, 44 },
        { 15, 17, 29, 49, 48, 43, 41 },
        { 11, 14, 22, 47, 42, 40, 38 },
        {  9, 10, 16, 28, 39, 37, 36 },
        {  5,  8, 12, 21, 25, 35, 34 },
        {  1,  3,  6, 13, 19, 26, 32 },
        {  0,  2,  4,  7, 20, 27, 33 }
    };

    private float[,] distanceMatrix = new float[13, 7];
    private float[,] distanceMatrixHorizontal = new float[13, 7];
    
    private int[] fixedPointsId = { 88, 89, 85, 84, 78, 75, 74, 81, 87, 90, 83, 77, 73, 66, 86, 82, 76};
    private int[] endPointsId = { 0, 2, 4, 7, 20, 27, 33 };
}
