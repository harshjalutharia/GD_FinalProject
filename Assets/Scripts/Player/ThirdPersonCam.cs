using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThirdPersonCam : MonoBehaviour
{
    [SerializeField, Tooltip("player transform")]private Transform player;
    [SerializeField, Tooltip("player's sub object orientation")] private Transform orientation;
    
    private void Start()
    {
        player = PlayerMovement.current.transform;
        orientation = PlayerMovement.current.orientation;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        // rotate orientation
        Vector3 viewDir = player.position - new Vector3(transform.position.x, player.position.y, transform.position.z);
        orientation.forward = viewDir.normalized;
    }

    
}
