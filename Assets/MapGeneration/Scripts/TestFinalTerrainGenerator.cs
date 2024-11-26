using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class TestFinalTerrainGenerator : MonoBehaviour
{
    [Header("=== Perlin Noise Settings ===")]
    [SerializeField, Tooltip("The visual (world) width of this map")]   private int width = 256;
    [SerializeField, Tooltip("The visual (world) height of this map")]  private int height = 256;
    public int gridWidth => width+1;    // The grid map width; +1 of world width due to vertices requiring one more point at the end
    public int gridHeight => height+1;  // The grid map height;
    [SerializeField, Tooltip("The scale of the perlin noise map")]      private float scale = 20f;
    [SerializeField] private float offsetX = 0f;
    [SerializeField] private float offsetY = 0f;
    [SerializeField, Range(0,6), Tooltip("The LOD level used to control the mesh fidelity.")] private int m_levelOfDetail;
    [SerializeField, Tooltip("The height curve that maps noise map to world height")]   private AnimationCurve m_heightCurve;
    [SerializeField, Tooltip("The height multiplier")]  private float m_heightMultiplier;

    [Header("=== Renderer Settings ===")]
    [SerializeField] private MeshFilter m_meshFilter;
    [SerializeField] private Renderer m_renderer;
    [SerializeField] private MeshCollider m_collider;
    [SerializeField] private float[,] m_noiseMap;
    [SerializeField] private FilterMode m_filterMode;

    [Header("=== Debug Settings ===")]
    [SerializeField] private bool m_autoUpdate = false;
    public bool autoUpdate => m_autoUpdate;

    public void GenerateMap() {
        Debug.Log("Generating map");
        m_noiseMap = GenerateNoise();
        
        Texture2D tData = GenerateTexture();
        MeshData mData = GenerateMeshData();
    
        if (m_renderer != null) {
            //m_meshFilter.sharedMesh = mData.CreateMesh();
            //if (m_collider != null) m_collider.sharedMesh = m_meshFilter.sharedMesh;
            var tempMaterial = new Material(m_renderer.sharedMaterial);
            tempMaterial.mainTexture = tData;
            m_renderer.sharedMaterial = tempMaterial;
        }

        if (m_meshFilter != null) {
            m_meshFilter.sharedMesh = mData.CreateMesh();
            //if (m_collider != null) m_collider.sharedMesh = m_meshFilter.sharedMesh;
        }
    }

    private float[,] GenerateNoise() {
        float[,] noiseMap = new float[gridWidth,gridHeight];
        for (int x = 0; x < gridWidth; x++) {
            for (int y = 0; y < gridHeight; y++) {
                noiseMap[x,y] = CalculateNoise(x,y,gridWidth,gridHeight);
            }
        }
        return noiseMap;
    }
    
    private MeshData GenerateMeshData() {
        float topLeftX = gridWidth / -2f;
        float topLeftZ = gridHeight / 2f;

        int meshSimplificationIncrement = (m_levelOfDetail == 0) ? 1 : m_levelOfDetail * 2;
        int verticesPerLine = gridWidth / meshSimplificationIncrement + 1;

        MeshData meshData = new MeshData(verticesPerLine, verticesPerLine);
        int vertexIndex = 0;

        for(int y = 0; y < gridHeight; y+=meshSimplificationIncrement) {
            for(int x = 0; x < gridWidth; x+=meshSimplificationIncrement) {
                float worldY = m_noiseMap[x,y] * m_heightMultiplier;
                meshData.vertices[vertexIndex] = new Vector3(topLeftX + x, worldY, topLeftZ - y);
                meshData.uvs[vertexIndex] = new Vector2(x/(float)gridWidth, y/(float)gridHeight);
                if (x < width && y < height) {
                    meshData.AddTriangle(vertexIndex, vertexIndex+verticesPerLine+1, vertexIndex+verticesPerLine);
                    meshData.AddTriangle(vertexIndex+verticesPerLine+1, vertexIndex, vertexIndex+1);
                }
                vertexIndex++;
            }
        }

        return meshData;
    }

    private Texture2D GenerateTexture() {
        Texture2D texture = new Texture2D(gridWidth, gridHeight);
        texture.filterMode = m_filterMode;
        texture.wrapMode = TextureWrapMode.Clamp;

        for (int x = 0; x < gridWidth; x++) {
            for (int y = 0; y < gridHeight; y++) {
                float sample = m_noiseMap[x,y];
                Color color = new Color(sample,sample,sample);
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        return texture;
    }
    private float CalculateNoise(int x, int y, int width, int height)
    {
        float xCoord = (float)x / width * scale + offsetX;
        float yCoord = (float)y / height * scale + offsetY;
        return Mathf.PerlinNoise(xCoord, yCoord);
    }

    private void OnValidate() {
        if (scale <= 1f) scale = 1f;
    }
}
