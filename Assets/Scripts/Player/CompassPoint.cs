using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CompassPoint : MonoBehaviour
{
    public Vector3 direction;

    public float angle;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

        // project needle north to world pos
        // Vector3 projectedNorth = Vector3.ProjectOnPlane(Vector3.forward, transform.forward);
        
        direction = Vector3.Cross(Vector3.right, transform.forward);
        
        angle = Vector3.SignedAngle(transform.up, direction, transform.forward);

        // rotate along z axis
        transform.Rotate(0, 0, angle, Space.Self);
    }
}
