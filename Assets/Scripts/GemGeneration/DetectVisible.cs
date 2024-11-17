using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DetectVisible : MonoBehaviour
{
    private Renderer objectRenderer;
    [SerializeField] private CameraMovement cameraMovement; 

    void Start()
    {
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer == null)
        {
            Debug.LogError($"No Renderer found on {gameObject.name}. OnBecameVisible will not work!");
        }

        // Find the CameraMovement script on the MainCamera
        cameraMovement = Camera.main.GetComponent<CameraMovement>();
        if (cameraMovement == null)
        {
            Debug.LogError("No CameraMovement script found on the MainCamera.");
        }
    }

    void OnBecameVisible()
    {
        if (cameraMovement != null)
        {
            cameraMovement.visibleObjects.Add(gameObject);
            Debug.Log($"{gameObject.name} became visible.");
        }
    }

    void OnBecameInvisible()
    {
        if (cameraMovement != null)
        {
            cameraMovement.visibleObjects.Remove(gameObject);
            Debug.Log($"{gameObject.name} became invisible.");
        }
    }
}
