using UnityEngine;

public class FadeObject : MonoBehaviour
{
    public float fadeDuration = 1.0f; // Duration of fade in/out in seconds
    public float rotationSpeed = 10.0f; // Speed of rotation around the Z-axis

    private Renderer objectRenderer;
    private Material objectMaterial;
    private Color objectColor;
    private bool isFadingOut = false;
    private bool isFadingIn = false;
    private float fadeTimer = 0f;

    void Start()
    {
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer != null)
        {
            objectMaterial = objectRenderer.material;
            objectColor = objectMaterial.color;
        }
        else
        {
            Debug.LogError("No Renderer found on the object. Please add a Renderer component.");
        }
    }

    void Update()
    {
        // Handle fade out when key '7' is pressed
        if (Input.GetKeyDown(KeyCode.Alpha7)) // Key '7'
        {
            StartFadeOut();
        }

        // Handle fade in when key '6' is pressed
        if (Input.GetKeyDown(KeyCode.Alpha6)) // Key '6'
        {
            StartFadeIn();
        }

        // Perform fade out if active
        if (isFadingOut)
        {
            FadeOut();
        }

        // Perform fade in if active
        if (isFadingIn)
        {
            FadeIn();
        }

        // Rotate the object around the Z-axis
        RotateObject();
    }

    public void StartFadeOut()
    {
        isFadingOut = true;
        isFadingIn = false;
        fadeTimer = 0f;
    }

    public void StartFadeIn()
    {
        isFadingIn = true;
        isFadingOut = false;
        fadeTimer = 0f;
    }

    public void FadeOut()
    {
        if (objectMaterial != null)
        {
            fadeTimer += Time.deltaTime;
            float alpha = Mathf.Lerp(objectColor.a, 0f, fadeTimer / fadeDuration);
            objectColor.a = Mathf.Clamp01(alpha);
            objectMaterial.color = objectColor;

            if (fadeTimer >= fadeDuration)
            {
                isFadingOut = false;
            }
        }
    }

    public void FadeIn()
    {
        if (objectMaterial != null)
        {
            fadeTimer += Time.deltaTime;
            float alpha = Mathf.Lerp(objectColor.a, 1f, fadeTimer / fadeDuration);
            objectColor.a = Mathf.Clamp01(alpha);
            objectMaterial.color = objectColor;

            if (fadeTimer >= fadeDuration)
            {
                isFadingIn = false;
            }
        }
    }

    private void RotateObject()
    {
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
    }
}
