using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

public class GemTrail : MonoBehaviour
{
    public Transform defaultFollowRef;
    public TrailRenderer trailRenderer;

    public bool traveling = false;
    public List<Vector3> points;
    public Vector3 velocity;
    public float travelTimePerPoint = 0.1f;
    public float trailTime = 3f;

    private int curPointIndex = 0;
    private float timeToTravel = 0f;
    private float curTime = 0f;

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
        this.points.Insert(0, transform.position);
        curPointIndex = 0;
        curTime = 0f;
        trailRenderer.time = trailTime;
        traveling = true;
    }
    
    public void Initialize(List<Vector3> points, bool addNoise=true) {
        this.points = new List<Vector3>(points);
        if (addNoise) {
            for(int i = 0; i < this.points.Count; i++) this.points[i] += new Vector3(Random.Range(0f,1f), Random.Range(0f,1f), Random.Range(0f, 1f));
        }
        this.points.Insert(0, transform.position);
        curPointIndex = 0;
        curTime = 0f;
        trailRenderer.time = trailTime;
        traveling = true;
    }

    private void Update() {
        if (!traveling)  {
            if (defaultFollowRef != null) transform.position = defaultFollowRef.position;
            return;
        }

        if (curPointIndex < this.points.Count)  {
            curTime += Time.deltaTime;
            if (Vector3.Distance(transform.position, this.points[curPointIndex]) <= 0.05f) {
                curPointIndex++;
                curTime = 0f;
                if (curPointIndex == this.points.Count) {
                    onLandmarkDestinationReached?.Invoke(gem, destination);
                    onDestinationReached?.Invoke();
                    Destroy(gameObject, trailRenderer.time);
                    return;
                }
                timeToTravel = Vector3.Distance(this.points[curPointIndex-1], this.points[curPointIndex]) / travelTimePerPoint;
            }

            transform.position = Vector3.Lerp(this.points[curPointIndex-1], this.points[curPointIndex], curTime/travelTimePerPoint);
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
