using System.Collections;
using UnityEngine;

public static class Generators {

    public enum FalloffShape { Box, Radial }
    public enum NormalizeMode { Local, Global }

    public static float[,] GenerateFloatMap(int mapWidth, int mapHeight, float defaultValue) {
        float[,] noiseMap = new float[mapWidth, mapHeight];
        for(int y = 0; y < mapHeight; y++) {
            for(int x = 0; x < mapWidth; x++) {
                noiseMap[x,y] = defaultValue;
            }
        }
        return noiseMap;
    }

    public static float[,] GenerateRandomMap(int mapWidth, int mapHeight, System.Random prng) {
        float[,] noiseMap = new float[mapWidth, mapHeight];
        for(int y = 0; y < mapHeight; y++) {
            for(int x = 0; x < mapWidth; x++) {
                float val = (float)prng.Next(0,100) / 100f;
                noiseMap[x,y] = val;
            }
        }
        return noiseMap;
    }

    // Generator function: Generates Perlin Noise map, given map dimensions, 
    //      the randomization seed, and Perlin Noise properties.
    public static float[,] GenerateNoiseMap(
            int mapWidth, int mapHeight, float scale,
            System.Random prng,
            int octaves, float persistance, float lacunarity,
            Vector2 offset,
            NormalizeMode normalizeMode
    ) {
        float[,] noiseMap = new float[mapWidth,mapHeight];

        Vector2[] octaveOffsets = new Vector2[octaves];
        float maxPossibleHeight = 0;
        float amplitude = 1;
        float frequency = 1;

        for(int i = 0; i < octaves; i++) {
            float offsetX = prng.Next(-100000, 100000) + offset.x;
            float offsetY = prng.Next(-100000, 100000) + offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);

            maxPossibleHeight += amplitude;
            amplitude *= persistance;
        }

        if (scale <= 0f) scale = 0.0001f;

        float maxLocalNoiseHeight = float.MinValue;
        float minLocalNoiseHeight = float.MaxValue;

        float halfWidth = mapWidth/2f;
        float halfHeight = mapHeight/2f;
        Debug.Log(200-halfWidth + octaveOffsets[0].x);

        for(int y = 0; y < mapHeight; y++) {
            for(int x = 0; x < mapWidth; x++) {
                
                amplitude = 1;
                frequency = 1;
                float noiseHeight = 0;

                for (int o = 0; o < octaves; o++) {
                    float sampleX = (float)(x-halfWidth + octaveOffsets[o].x) / scale * frequency;
                    float sampleY = (float)(y-halfHeight - octaveOffsets[o].y) / scale * frequency;
                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }

                if (noiseHeight > maxLocalNoiseHeight) maxLocalNoiseHeight = noiseHeight;
                if (noiseHeight < minLocalNoiseHeight) minLocalNoiseHeight = noiseHeight;
                noiseMap[x,y] = noiseHeight;
            }
        }

        for(int y = 0; y < mapHeight; y++) {
            for(int x = 0; x < mapWidth; x++) {
                // This assumes we do not need to stich together chunks that are next to each other
                if (normalizeMode == NormalizeMode.Local) {
                    noiseMap[x,y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x,y]);
                }
                // This assumes we want to estimate a global scalee for the min and max noise height
                else {
                   //float normalizedHeight = (noiseMap[x,y]+1f) / (2f*maxPossibleHeight / 1.75f);
                    //noiseMap[x,y] = normalizedHeight;
                    noiseMap[x,y] = 1 / (1 + Mathf.Exp(noiseMap[x,y] * 3));
                }
            }
        }

        return noiseMap;
    }

    // Generator function: Generates a "falloff" map where cells of a map at the edges are made flat, 
    //      and those close to the center retain their height.
    public static float[,] GenerateFalloffMap(
            int mapWidth, int mapHeight, 
            float falloffStart, float falloffEnd
    ) {
        float[,] heightMap = new float[mapWidth, mapHeight];

        float halfWidth = mapWidth/2f;
        float halfHeight = mapHeight/2f;
        
        for(int y = 0; y < mapHeight; y++) {
            for(int x = 0; x < mapWidth; x++) {
                float xPos = (float)(x) / mapWidth * 2f - 1;
                float yPos = (float)(y) / mapHeight * 2f - 1;

                // Find which value is closer to the edge
                float t = Mathf.Max(Mathf.Abs(xPos), Mathf.Abs(yPos));
                if (t < falloffStart) heightMap[x,y] = 1;
                else if (t > falloffEnd) heightMap[x,y] = 0;
                else heightMap[x,y] = Mathf.SmoothStep(1,0,Mathf.InverseLerp(falloffStart, falloffEnd, t));
            }
        }

        return heightMap;
    }
    // Generator function: Generates a "falloff" map where cells of a map at the edges are made flat, 
    //      and those close to a given centroid position retain their height.
    public static float[,] GenerateFalloffMap(
            int mapWidth, int mapHeight,
            float centerX, float centerY, FalloffShape shape, 
            float falloffStart, float falloffEnd,
            bool invert = false
    ) {
        float[,] heightMap = new float[mapWidth, mapHeight];
        float halfWidth = mapWidth/2f;
        float halfHeight = mapHeight/2f;
        Vector2 centroid = new Vector2(centerX, centerY);
        
        for(int y = 0; y < mapHeight; y++) {
            for(int x = 0; x < mapWidth; x++) {
                // This forces the X position and Y position to be between -1 and 1
                float xPos = (float)(x) / mapWidth * 2f - 1;
                float yPos = (float)(y) / mapHeight * 2f - 1;

                // Depending on the falloff shape (i.e. box or radial), we have to determine the `t` value
                float t = (shape == FalloffShape.Box)
                    ? Mathf.Max(Mathf.Abs(xPos-centerX), Mathf.Abs(yPos-centerY))
                    : Vector2.Distance(centroid, new Vector2(xPos, yPos));
                // Find which value is closer to the edge
                //float t = Mathf.Max(Mathf.Abs(xPos), Mathf.Abs(yPos));
                if (t < falloffStart) heightMap[x,y] = (invert) ? 0 : 1;
                else if (t > falloffEnd) heightMap[x,y] = (invert) ? 1 : 0;
                else heightMap[x,y] = (invert) 
                    ? Mathf.SmoothStep(1,0,Mathf.InverseLerp(falloffEnd, falloffStart, t))
                    : Mathf.SmoothStep(1,0,Mathf.InverseLerp(falloffStart, falloffEnd, t));
            }
        }

        return heightMap;
    }

    public static float[,] GenerateHeightMap(float[,] noiseMap, AnimationCurve heightCurve, float heightMultiplier) {
        int width = noiseMap.GetLength(0);
        int height = noiseMap.GetLength(1);
        float[,] heightMap = new float[width, height];

        for(int y = 0; y < height; y++) {
            for(int x = 0; x < width; x++) {
                heightMap[x,y] = heightCurve.Evaluate(noiseMap[x,y]) * heightMultiplier;
            }
        }

        return heightMap;
    }

    // Generator function: Generate a new mesh, combplete with vertices and triangles, from a provided float[,] map.
    public static void GenerateMesh(
            int mapWidth, int mapHeight, 
            float[,] heightMap, 
            ref Mesh mesh
    ) {
        // Generate the vertices
        Vector3[] vertices = new Vector3[(mapWidth+1)*(mapHeight+1)];
        float minTerrainHeight = float.MaxValue, maxTerrainHeight = float.MinValue;

        for(int i = 0, z = 0; z <= mapHeight; z++) {
            for(int x = 0; x <= mapWidth; x++) {
                int xi = (x == mapWidth) ? x-1 : x;
                int zi = (z == mapHeight) ? z-1 : z;
                float y = heightMap[xi,zi];
                vertices[i] = new Vector3(x,y,z);
                if (y > maxTerrainHeight) maxTerrainHeight = y;
                if (y < minTerrainHeight) minTerrainHeight = y;
                i++;
            }
        }

        // Generate the triangles from the vertices
        int[] triangles = new int[mapWidth*mapHeight*6];
        int vert = 0;
        int tris = 0;
        for(int z = 0; z < mapHeight; z++) {
            for(int x = 0; x < mapWidth; x++) {
                triangles[tris] = vert + 0;
                triangles[tris+1] = vert + mapWidth+1;
                triangles[tris+2] = vert + 1;
                triangles[tris+3] = vert + 1;
                triangles[tris+4] = vert + mapWidth+1;
                triangles[tris+5] = vert + mapWidth+2;

                vert++;
                tris += 6;
            }
            vert++;
        }

        // Update the mesh!
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }

    // Helper function: Given two maps of equal size, add the two maps together
    public static float[,] AddMap(
            int mapWidth, int mapHeight, 
            float[,] firstMap, 
            float[,] secondMap
    ) {
        float[,] sumMap = new float[mapWidth, mapHeight];
        for(int y = 0; y < mapHeight; y++) {
            for(int x = 0; x < mapWidth; x++) {
                sumMap[x,y] = firstMap[x,y] + secondMap[x,y];
            }
        }
        return sumMap;
    }
    // Helper function: Given two maps of equal size, subtract the two maps together
    public static float[,] SubtractMap(
            int mapWidth, int mapHeight, 
            float[,] firstMap, 
            float[,] secondMap
    ) {
        float[,] diffMap = new float[mapWidth, mapHeight];
        for(int y = 0; y < mapHeight; y++) {
            for(int x = 0; x < mapWidth; x++) {
                diffMap[x,y] = firstMap[x,y] - secondMap[x,y];
            }
        }
        return diffMap;
    }
    // Helper function: Given two maps of equal size, multiply the two maps together
    public static float[,] MultiplyMap(
            int mapWidth, int mapHeight, 
            float[,] firstMap, 
            float[,] secondMap
    ) {
        float[,] multMap = new float[mapWidth, mapHeight];
        for(int y = 0; y < mapHeight; y++) {
            for(int x = 0; x < mapWidth; x++) {
                multMap[x,y] = firstMap[x,y] * secondMap[x,y];
            }
        }
        return multMap;
    }
    // Helper function: Given two maps of equal size, divide the two maps together
    public static float[,] DivideMap(
            int mapWidth, int mapHeight, 
            float[,] firstMap, 
            float[,] secondMap
    ) {
        float[,] quotientMap = new float[mapWidth, mapHeight];
        for(int y = 0; y < mapHeight; y++) {
            for(int x = 0; x < mapWidth; x++) {
                float divisor = (secondMap[x,y] == 0) ? 0.0001f : secondMap[x,y];
                quotientMap[x,y] = firstMap[x,y] / divisor;
            }
        }
        return quotientMap;
    }

    // Helper function: Given a map and a min and max value range, produce a new 2D map whose values 
    //      are between the proposed min and max values. It's expected that the map's original values
    //      are already normalized between 0 and 1
    public static float[,] ScaleMap(
            int mapWidth, int mapHeight, 
            float[,] heightMap, 
            float minValue, float maxValue
    ) {
        float[,] scaleMap = new float[mapWidth, mapHeight];

        for(int y = 0; y < mapHeight; y++) {
            for(int x = 0; x < mapWidth; x++) {
                scaleMap[x,y] = Mathf.Lerp(minValue,maxValue,heightMap[x,y]);
            }
        }

        return scaleMap;
    }

    public static Texture2D TextureFromColorMap(int width, int height, Color[] colorMap, FilterMode filterMode) {
        Texture2D texture = new Texture2D(width, height);
        texture.filterMode = filterMode;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(colorMap);
        texture.Apply();
        return texture;
    }

    public static Texture2D TextureFromHeightMap(float[,] heightMap, FilterMode filterMode) {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        Texture2D texture = new Texture2D(width,height);
        
        Color[] colorMap = new Color[width*height];
        for(int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                colorMap[y*width+x] = Color.Lerp(Color.black, Color.white, heightMap[x,y]);
            }
        }
        
        return TextureFromColorMap(width, height, colorMap, filterMode);
    }

    public static MeshData GenerateTerrainMesh(float[,] heightMap, AnimationCurve heightCurve, float heightMultiplier) {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        float topLeftX = (width-1) / -2f;
        float topLeftZ = (height-1) / 2f;

        MeshData meshData = new MeshData(width, height);
        int vertexIndex = 0;

        for(int y = 0; y < height; y++) {
            for(int x = 0; x < width; x++) {
                meshData.vertices[vertexIndex] = new Vector3(topLeftX + x, heightCurve.Evaluate(heightMap[x,y])*heightMultiplier, topLeftZ - y);
                meshData.uvs[vertexIndex] = new Vector2(x/(float)width, y/(float)height);
                if (x < width-1 && y < height-1) {
                    meshData.AddTriangle(vertexIndex, vertexIndex+width+1, vertexIndex+width);
                    meshData.AddTriangle(vertexIndex+width+1, vertexIndex, vertexIndex+1);
                }
                vertexIndex++;
            }
        }

        return meshData;
    }

    public static Texture2D FlipTextureVertically(Texture2D original) {
        var originalPixels = original.GetPixels();
        var newPixels = new Color[originalPixels.Length];

        var width = original.width;
        var rows = original.height;

        for (var x = 0; x < width; x++) {
            for (var y = 0; y < rows; y++) {
                newPixels[x + y * width] = originalPixels[x + (rows - y -1) * width];
            }
        }

        return TextureFromColorMap(width, rows, newPixels, original.filterMode);
    }

    public static Texture2D FlipTextureHorizontally(Texture2D original) {
        var originalPixels = original.GetPixels();
        var newPixels = new Color[originalPixels.Length];

        var width = original.width;
        var rows = original.height;

        for (var x = 0; x < width; x++) {
            for (var y = 0; y < rows; y++) {
                newPixels[x + y * width] = originalPixels[(width - x - 1) + y * width];
            }
        }

        return TextureFromColorMap(width, rows, newPixels, original.filterMode);
    }

    public static Texture2D DrawCircleOnTexture(Texture2D original, int x, int y, int radius, Color color) {

        var width = original.width;
        var rows = original.height;

        // Mod it so that it draws a big, fat circle at the destination point.
        float rSquared = radius * radius;
        for (int u = x - radius; u < x + radius + 1; u++) {
            for (int v = y - radius; v < y + radius + 1; v++) {
                if ((x - u) * (x - u) + (y - v) * (y - v) < rSquared) original.SetPixel(u, v, color);
            }
        }

        // Set the new pixels
        return TextureFromColorMap(width, rows, original.GetPixels(), original.filterMode);
    }
}

public class MeshData {
    public Vector3[] vertices;
    public int[] triangles;
    public Vector2[] uvs;

    int triangleIndex;

    public MeshData(int meshWidth, int meshHeight) {
        vertices = new Vector3[meshWidth * meshHeight];
        triangles = new int[(meshWidth-1)*(meshHeight-1)*6];
        uvs = new Vector2[meshWidth * meshHeight];
    }

    public void AddTriangle(int a, int b, int c) {
        triangles[triangleIndex] = a;
        triangles[triangleIndex+1] = b;
        triangles[triangleIndex+2] = c;
        triangleIndex += 3;
    }

    public Mesh CreateMesh() {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        return mesh;
    }
}
