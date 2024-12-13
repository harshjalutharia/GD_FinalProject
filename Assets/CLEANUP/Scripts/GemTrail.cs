using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GemTrail : MonoBehaviour
{
    public Transform playerRef;
    public TrailRenderer trailRenderer;

    public bool traveling = false;
    public List<Vector3> points;
    public Vector3 velocity;
    public float travelTimePerPoint = 0.1f;
    public float trailTime = 3f;

    public Landmark destination;
    public UnityEvent<Landmark> onDestinationReached;

    public void SetDestination(Landmark destination) {
        this.destination = destination;
        points = BezierCurves.DetermineQuadraticCurve(20, transform.position, destination.pathingForward.position, destination.pathingDestination.position);
        trailRenderer.time = trailTime;
        if (Voronoi.current != null) {
            Region region = Voronoi.current.regions[destination.regionIndex];
            trailRenderer.colorGradient = region.attributes.gradient;
        }
        traveling = true;
    }

    private void Update() {
        if (!traveling) {
            transform.position = playerRef.position;
            return;
        }
        if (Vector3.Distance(transform.position, points[0]) <= 0.05f) {
            points.RemoveAt(0);
            if (points.Count == 0) {
                Reset();
                onDestinationReached?.Invoke(destination);
                return;
            }
        }
        transform.position = Vector3.SmoothDamp(transform.position, points[0], ref velocity, travelTimePerPoint);
    }

    public void Reset() {
        trailRenderer.time = 0f;
        transform.position = playerRef.position;
        velocity = Vector3.zero;
        traveling = false;
    }
}
