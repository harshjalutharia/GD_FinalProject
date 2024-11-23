using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// public class CameraFowllowPointControl : MonoBehaviour
// {
//     [Tooltip("Player Object")]
//     public Transform player;
//
//     private Rigidbody playerRb;
//     private PlayerMovement playerMovement;
//
//     [Tooltip("Maximum ground distance tolerated")]
//     public float maxGroundDistance;
//
//     [Tooltip("layers of ground")] 
//     public LayerMask groundMask;
//
//     public float followSpeed;
//
//     [SerializeField, Tooltip("if camera track point overlap with player")]
//     private bool track;
//     
//
//     private void Awake()
//     {
//         playerMovement = player.GetComponent<PlayerMovement>();
//     }
//
//     void Start()
//     {
//         playerRb = player.GetComponent<Rigidbody>();
//         track = false;
//     }
//
//     
//     
//     
//     void Update()
//     {
//         RaycastHit hit = new RaycastHit();
//         
//         
//         if (Physics.Raycast(player.position, Vector3.down, out hit, maxGroundDistance, groundMask))
//         {
//             Vector3 groundPos = player.position + hit.distance * Vector3.down;
//             if (track)
//             {
//                 float supposedY = Mathf.Lerp(transform.position.y, player.position.y, Time.deltaTime * followSpeed);
//                 transform.position = new Vector3(player.position.x, Mathf.Max(groundPos.y, supposedY), player.position.z);
//                 // transform.position = Vector3.Lerp(transform.position, groundPos, Time.deltaTime * followSpeed);
//             }
//             else
//             {
//                 transform.position = groundPos;
//             }
//         }
//         else
//         {
//             transform.position = Vector3.Lerp(transform.position, player.position, Time.deltaTime * followSpeed);
//             track = true;
//         }
//     }
//     
//     private void OnEnable()
//     {
//         Debug.Log(playerMovement.maxFlightStamina);
//         playerMovement.OnLanding += Landing;
//     }
//
//     private void OnDisable()
//     {
//         playerMovement.OnLanding -= Landing;
//     }
//
//     private void Landing()
//     {
//         track = false;
//     }
// }


public class CameraFowllowPointControl : MonoBehaviour
{
    [Tooltip("Player Object")]
    public Transform player;
    
    //public float followSpeed;//0.3f
    [Tooltip("Approximately the time the camera will take to reach the player")]
    public float smoothTime;
    Vector3 currentVelocity = Vector3.zero;
    public bool trackXZ;
    
    void Start()
    {
        transform.SetParent(null);
    }


    private void FixedUpdate()
    {
        if (trackXZ)
        {
            transform.position = new Vector3(player.position.x, transform.position.y, player.position.z);
        }
        transform.position = Vector3.SmoothDamp(transform.position, player.position, ref currentVelocity, smoothTime);

    }
}




