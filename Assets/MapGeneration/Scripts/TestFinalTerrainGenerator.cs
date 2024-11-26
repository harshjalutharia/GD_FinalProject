using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class TestFinalTerrainGenerator : MonoBehaviour
{
    [Header("=== Perlin Noise Settings ===")]
    public int width = 256;
    public int height = 256;
    public int mapWidth => width+1;
    public int mapHeight => height+1;
    public float scale = 20f;
    [SerializeField] private float offsetX = 0f;
    [SerializeField] private float offsetY = 0f;

    [Header("=== Renderer Settings ===")]
    [SerializeField] private MeshFilter m_meshFilter;
    [SerializeField] private Renderer m_renderer;
    [SerializeField] private MeshCollider m_collider;
    [SerializeField] private float[,] m_noiseMap;
    [SerializeField, Range(0f,1f)] private float m_minXTexture = 0f;
    [SerializeField, Range(0f,1f)] private float m_maxXTexture = 1f;
    [SerializeField, Range(0f,1f)] private float m_minYTexture = 0f;
    [SerializeField, Range(0f,1f)] private float m_maxYTexture = 1f;
    [SerializeField] private FilterMode m_filterMode;

    [Header("=== Debug Settings ===")]
    [SerializeField] private bool m_autoUpdate = false;
    public bool autoUpdate => m_autoUpdate;

    public void GenerateMap() {
        m_noiseMap = GenerateNoise();

        if (m_renderer != null && m_meshFilter != null) {
            //MeshData mData = GenerateMeshData();
            Texture2D tData = GenerateTexture();

            //m_meshFilter.sharedMesh = mData.CreateMesh();
            //if (m_collider != null) m_collider.sharedMesh = m_meshFilter.sharedMesh;
        
            var tempMaterial = new Material(m_renderer.sharedMaterial);
            tempMaterial.mainTexture = tData;
            m_renderer.sharedMaterial = tempMaterial;
        }
    }

    private float[,] GenerateNoise() {
        float[,] noiseMap = new float[mapWidth,mapHeight];
        for (int x = 0; x < mapWidth; x++) {
            for (int y = 0; y < mapHeight; y++) {
                noiseMap[x,y] = CalculateNoise(x,y,mapWidth,mapHeight);
            }
        }
        return noiseMap;
    }
    
    private MeshData GenerateMeshData() {
        Vector2Int xCoords = new Vector2Int(Mathf.RoundToInt(mapWidth*m_minXTexture), Mathf.RoundToInt(mapWidth*m_maxXTexture));
        Vector2Int yCoords = new Vector2Int(Mathf.RoundToInt(mapHeight*m_minYTexture), Mathf.RoundToInt(mapHeight*m_maxYTexture));
        int textureWidth = xCoords.y - xCoords.x;
        int textureHeight = yCoords.y - yCoords.x;


        //int mapWidth = m_noiseMap.GetLength(0);
        //int mapHeight = m_noiseMap.GetLength(1);
        float topLeftX = width / -2f;
        float topLeftZ = height / 2f;

        //int meshSimplificationIncrement = (m_levelOfDetail == 0) ? 1 : m_levelOfDetail * 2;
        //int verticesPerLine = (width-1) / meshSimplificationIncrement + 1;

        MeshData meshData = new MeshData(textureWidth, textureHeight);
        int vertexIndex = 0;

        for(int y = 0; y < textureHeight; y++) {
            for(int x = 0; x < textureWidth; x++) {
                meshData.vertices[vertexIndex] = new Vector3(x, m_noiseMap[x,y], -y);
                meshData.uvs[vertexIndex] = new Vector2(x/(float)mapWidth, y/(float)mapHeight);
                if (x < width && y < height) {
                    meshData.AddTriangle(vertexIndex, vertexIndex+textureHeight+1, vertexIndex+textureHeight);
                    meshData.AddTriangle(vertexIndex+textureHeight+1, vertexIndex, vertexIndex+1);
                }
                vertexIndex++;
            }
        }

        return meshData;
    }

    private Texture2D GenerateTexture() {
        // Texture dimensions (width and height) are based on how much of the original width and height we want to render
        Vector2Int xCoords = new Vector2Int(Mathf.RoundToInt(width*m_minXTexture), Mathf.RoundToInt(width*m_maxXTexture));
        Vector2Int yCoords = new Vector2Int(Mathf.RoundToInt(height*m_minYTexture), Mathf.RoundToInt(height*m_maxYTexture));
        int textureWidth = xCoords.y - xCoords.x;
        int textureHeight = yCoords.y - yCoords.x;

        Texture2D texture = new Texture2D(textureWidth, textureHeight);
        texture.filterMode = m_filterMode;
        texture.wrapMode = TextureWrapMode.Clamp;

        for (int x = 0; x < textureWidth; x++) {
            for (int y = 0; y < textureHeight; y++) {
                int xi = x + xCoords.x;
                int yi = y + yCoords.x;
                float sample = m_noiseMap[xi,yi];
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
        if (scale <= 0) scale = 0.001f;
        if (m_maxXTexture <= m_minXTexture) m_maxXTexture = m_minXTexture + 0.001f;
        if (m_maxYTexture <= m_minYTexture) m_maxYTexture = m_minYTexture + 0.001f;
    }
}
