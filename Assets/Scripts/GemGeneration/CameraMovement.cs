using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public float moveSpeed = 5f; 
    public HashSet<GameObject> visibleObjects = new HashSet<GameObject>();
    public int numVisibleObjects;

    void Update()
    {
        float moveX = 0f;
        float moveZ = 0f;

        if (Input.GetKey(KeyCode.W))
        {
            moveZ = 1f;
        }
        if (Input.GetKey(KeyCode.S))
        {
            moveZ = -1f;
        }
        if (Input.GetKey(KeyCode.A))
        {
            moveX = -1f;
        }
        if (Input.GetKey(KeyCode.D))
        {
            moveX = 1f;
        }

        Vector3 movement = new Vector3(moveX, 0f, moveZ).normalized * moveSpeed * Time.deltaTime;

        transform.position += movement;

        foreach (GameObject obj in visibleObjects)
        {
            Debug.Log($"Visible object: {obj.name}");
        }

        numVisibleObjects = visibleObjects.Count;
    }

}
