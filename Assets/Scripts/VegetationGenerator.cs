using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VegetationGenerator : MonoBehaviour
{
    // about tree
    public List<GameObject> treePrefab;
    public int numOfTree = 200;


    // about map
    public VoronoiMap voronoiMap;
    public int mapChunkSize = 481;

    public NoiseMap terrainGenerator;

    [SerializeField, Range(0f, 1f)] private float thresholdPlantTree =0.5f;
    [Range(0f, 0.5f)] public float edgeBuffer;


    // about generated tree
    
    public float MinHeight = 0.3f;
    public float MaxHeight = 0.6f;

    public List<GameObject> generatedTree;



    // Update is called once per frame
    void Update()
    {
        
    }

    public void GenerateVegetation(){

        int edgeDistance  = Mathf.FloorToInt(mapChunkSize* edgeBuffer);
        int treeCount =0;
        generatedTree= new List<GameObject> ();

        for (int y= 0; y < mapChunkSize; y++){
            for (int x =0; x < mapChunkSize; x++){
                // x and y are coordinate for the map

                // check the buffer

                if (x < edgeDistance) continue;
                if (x > mapChunkSize - edgeDistance) continue;
                if (y < edgeDistance) continue;
                if (y > mapChunkSize - edgeDistance) continue;

                float noise = voronoiMap.QueryNoiseAtCoords(x ,y, out Vector3 dummy);
                //Debug.Log(noise);

                if (noise < MinHeight || noise > MaxHeight) continue;

                float shouldPlant= UnityEngine.Random.Range(0f, 1f);

                if (shouldPlant > thresholdPlantTree) continue;

                int PrefabIndex = UnityEngine.Random.Range(0, treePrefab.Count);

                GameObject prefab = treePrefab[PrefabIndex];

                //the coordinate offset of the planted tree
                Vector3 posOffset= new Vector3 (UnityEngine.Random.Range(0f, 1f), 0f, UnityEngine.Random.Range(0f, 1f));

                //the orientation of the planted tree
                Quaternion cor = Quaternion.Euler(0, UnityEngine.Random.Range(0f, 360f), 0);

                float heightNoise = terrainGenerator.QueryHeightAtCoords(x, y, out Vector3 pixelPosition);

                GameObject t = Instantiate (prefab, posOffset+pixelPosition , cor );

                generatedTree.Add(t);

                if (generatedTree.Count >= numOfTree) break;
            }
            if (generatedTree.Count >= numOfTree) break;
        }

    }
}
