using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GemTrail : MonoBehaviour
{
    public Transform defaultFollowRef;
    public TrailRenderer trailRenderer;

    public bool traveling = false;
    public List<Vector3> points;
    public Vector3 velocity;
    public float travelTimePerPoint = 0.1f;
    public float trailTime = 3f;

    public Gem gem;
    public Landmark destination;
    public UnityEvent<Gem, Landmark> onLandmarkDestinationReached;
    public UnityEvent onDestinationReached;

    public void SetTrailGradient(Gradient gradient) {
        trailRenderer.colorGradient = gradient;
    }

    public void InitializeAsGem(Gem gem, Landmark destination) {
        this.gem = gem;
        this.destination = destination;
        this.points = BezierCurves.DetermineQuadraticCurve(20, transform.position, destination.pathingForward.position, destination.pathingDestination.position);
        trailRenderer.time = trailTime;
        traveling = true;
    }
    
    public void Initialize(List<Vector3> points) {
        this.points = points;
        trailRenderer.time = trailTime;
        traveling = true;
    }

    private void Update() {
        if (!traveling) {
            if (defaultFollowRef != null) transform.position = defaultFollowRef.position;
            return;
        }
        if (points.Count > 0) {
            if (Vector3.Distance(transform.position, points[0]) <= 0.05f) {
                points.RemoveAt(0);
                if (points.Count == 0) {
                    onLandmarkDestinationReached?.Invoke(gem, destination);
                    onDestinationReached?.Invoke();
                    return;
                }
            }
            transform.position = Vector3.SmoothDamp(transform.position, points[0], ref velocity, travelTimePerPoint);
        }
    }

    public void Reset() {
        trailRenderer.time = 0f;
        if (defaultFollowRef != null) transform.position = defaultFollowRef.position;
        this.gem = null;
        this.destination = null;
        velocity = Vector3.zero;
        traveling = false;
    }
}
