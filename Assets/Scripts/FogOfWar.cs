using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FogOfWar : MonoBehaviour
{
    public int textureSize = 512; // Adjust as needed
    public Color fogColor = new Color(0, 0, 0, 0.8f);
    public Transform player;
    public float revealRadius = 20f; // Adjust based on your map scale
    private Texture2D fogTexture;
    private Vector3 mapOrigin;
    private Vector3 mapSize;

    void Start()
    {
        // Initialize the fog texture
        fogTexture = new Texture2D(textureSize, textureSize, TextureFormat.ARGB32, false);
        fogTexture.wrapMode = TextureWrapMode.Clamp;
        fogTexture.filterMode = FilterMode.Bilinear;

        // Fill the texture with the fog color
        Color[] colors = new Color[textureSize * textureSize];
        for (int i = 0; i < colors.Length; i++)
            colors[i] = fogColor;

        fogTexture.SetPixels(colors);
        fogTexture.Apply();

        // Assign the texture to the material
        Renderer renderer = GetComponent<Renderer>();
        renderer.material.mainTexture = fogTexture;

        // Ensure the material's shader supports transparency
        if (renderer.material.shader.name != "Unlit/Transparent")
        {
            renderer.material.shader = Shader.Find("Unlit/Transparent");
        }

        // Get map boundaries
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        mapOrigin = meshRenderer.bounds.min;
        mapSize = meshRenderer.bounds.size;

        // Debug the map size
        Debug.Log($"Map Size: {mapSize.x}, {mapSize.y}, {mapSize.z}");
    }

    void Update()
    {
        if (player != null)
        {
            // Update fog around the player
            RevealFog(player.position);

            // For debugging, save the fog texture when pressing 'S'
            if (Input.GetKeyDown(KeyCode.F))
            {
                SaveFogTexture();
            }
        }
        else
        {
            Debug.LogWarning("Player Transform is not assigned in the FogOfWar script.");
        }
    }

    void RevealFog(Vector3 position)
    {
        // Convert world position to texture coordinates
        float posX = (position.x - mapOrigin.x) / mapSize.x;
        float posY = (position.z - mapOrigin.z) / mapSize.z;

        // Flip posY if necessary
        posY = 1.0f - posY;

        // Clamp posX and posY between 0 and 1
        posX = Mathf.Clamp01(posX);
        posY = Mathf.Clamp01(posY);

        int texPosX = Mathf.RoundToInt(posX * textureSize);
        int texPosY = Mathf.RoundToInt(posY * textureSize);

        // Compute the size of one pixel in world units
        float pixelSize = mapSize.x / textureSize;

        // Calculate the radius in pixels
        int radius = Mathf.RoundToInt(revealRadius / pixelSize);

#if UNITY_EDITOR
        Debug.Log($"posX: {posX}, posY: {posY}");
        Debug.Log($"texPosX: {texPosX}, texPosY: {texPosY}, radius: {radius}");
#endif

        // Loop through the pixels in the radius and set them to transparent
        for (int x = texPosX - radius; x <= texPosX + radius; x++)
        {
            for (int y = texPosY - radius; y <= texPosY + radius; y++)
            {
                // Check if the pixel is within the texture bounds
                if (x >= 0 && x < textureSize && y >= 0 && y < textureSize)
                {
                    float distance = Vector2.Distance(new Vector2(texPosX, texPosY), new Vector2(x, y));

                    if (distance <= radius)
                    {
                        fogTexture.SetPixel(x, y, Color.clear);
                    }
                }
            }
        }
        fogTexture.Apply();
    }

    void SaveFogTexture()
    {
        byte[] bytes = fogTexture.EncodeToPNG();
        string path = Application.dataPath + "/fogTexture.png";
        System.IO.File.WriteAllBytes(path, bytes);
        Debug.Log($"Fog texture saved to {path}");
    }
}
