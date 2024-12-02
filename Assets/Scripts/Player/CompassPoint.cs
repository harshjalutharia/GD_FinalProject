using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CompassPoint : MonoBehaviour
{
    public Transform forwardTransformRef = null;
    public Vector3 direction;

    public float angle;

    private void Awake() {
        if (forwardTransformRef == null) forwardTransformRef = this.transform;        
    }

    private void Update() {

        /*
        // project needle north to world pos
        // Vector3 projectedNorth = Vector3.ProjectOnPlane(Vector3.forward, transform.forward);
        
        direction = Vector3.Cross(Vector3.right, transform.forward);
        
        angle = Vector3.SignedAngle(forwardTransformRef.forward, direction, Vector3.up);

        // rotate along z axis
        transform.Rotate(0, 0, angle, Space.Self);
        */

        transform.localRotation = Quaternion.Euler(0f, 0f, -Vector3.SignedAngle(Vector3.forward, forwardTransformRef.forward, Vector3.up));
    }
}
