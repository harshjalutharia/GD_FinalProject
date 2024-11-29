using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class DistanceLOD : MonoBehaviour
{
    const float HORIZONTAL_FOV_RADIUS = 0.6f;
    const float VERTICAL_FOV_RADIUS = 0.6f;

    [System.Serializable]
    public class _LODLevel {
        public Mesh model;
        public List<Material> materials;
        public float threshold;
    }

    [Header("=== References ===")]
    [SerializeField, Tooltip("Reference to the mesh filter")]           private MeshFilter m_filter;
    [SerializeField, Tooltip("Reference to the mesh renderer")]         private Renderer m_renderer;
    [SerializeField, Tooltip("The fustrum camera we want to check")]    private FustrumCamera m_fustrumCamera;
    
    [Header("=== About the Object ===")]
    [SerializeField, Tooltip("Positional offset")]                          private Vector3 m_pivotOffset = Vector3.zero;
    [SerializeField, Tooltip("The LOD levels we want to cycle through")]    private List<_LODLevel> m_levels = new List<_LODLevel>();
    
    [Header("=== About the LOD Parameters ===")]
    [SerializeField, Tooltip("Do we do horizontal fustrum cull?")]                              private bool m_cullHorizontal = true;
    [SerializeField, Tooltip("Do we do vertical fustrum cull?")]                                private bool m_cullVertical = true;
    [SerializeField, Tooltip("Distance threshold from the camera")]                             private float m_maxDistanceThreshold = 20f;
    [SerializeField, Tooltip("Camera speed threshold")]                                         private float m_cameraSpeedThreshold = 10f;
    [SerializeField, Tooltip("Animation Curve to control the influence of camera speed")]       private AnimationCurve m_camSpeedModifier;
    [SerializeField, Tooltip("Max amount we want to cap the LOD to based on the camera ratio")] private float m_minCamSpeedWeight;
    //[SerializeField, Tooltip("FOV distance weight. 0 = purely horizontal FOV, 1 = purely vertical FOV, 0.5 = both equally"), Range(0f,1f)]      private float m_fovWeight;
    //[SerializeField, Tooltip("Animation Curve for handling the distance issue when distance is max weight vs when it's not so max weighted")]   private AnimationCurve m_FOVToDistanceRatioModifier;

    #if UNITY_EDITOR
    void OnDrawGizmos() {
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(transform.position + m_pivotOffset, 0.1f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(m_fustrumCamera.transform.position, m_maxDistanceThreshold);
    }
    #endif

    private void LateUpdate() {
        if (m_fustrumCamera == null) return;
        if (m_levels.Count == 0) return;

        // Calc. the current pivot we'll be doing all calculations off of
        Vector3 refPivot = transform.position + m_pivotOffset;

        // Calculate the world to viewport point from this transform.position, which returns a Vector3.
        Vector3 viewportPoint = m_fustrumCamera.camera.WorldToViewportPoint(refPivot);

        // Calculate the horizontal and vertical FOV ratio, then combine based on weighted summation
        Vector3 dirFromCamera = (refPivot-m_fustrumCamera.transform.position).normalized;
        float horizontalOffset = (Vector3.Dot(new Vector3(dirFromCamera.x, 0f, dirFromCamera.z), new Vector3(m_fustrumCamera.transform.forward.x, 0f, m_fustrumCamera.transform.forward.z)) <= 0f) ? HORIZONTAL_FOV_RADIUS : 0f;
        float horizontalRatio = horizontalOffset + Mathf.Abs(viewportPoint.x - 0.5f);
        float verticalOffset = (Vector3.Dot(new Vector3(dirFromCamera.z, dirFromCamera.y), new Vector2(m_fustrumCamera.transform.forward.z, m_fustrumCamera.transform.forward.y)) <= 0f) ? VERTICAL_FOV_RADIUS : 0f;
        float verticalRatio = verticalOffset + Mathf.Abs(viewportPoint.y - 0.5f);
        Debug.Log($"{horizontalOffset}, {verticalOffset}");
        if ( (m_cullHorizontal && horizontalRatio > HORIZONTAL_FOV_RADIUS) || (m_cullVertical && verticalRatio > VERTICAL_FOV_RADIUS) ) {
            Debug.Log("Culled");
            m_renderer.enabled = false;
            return;
        }

        // Calculate the distance from the camera as a means of distance thresholding
        float distanceRatio = Mathf.Abs(Vector3.Distance(refPivot,m_fustrumCamera.transform.position)/m_maxDistanceThreshold);

        // Calculate the camera speed influence
        float camSpeedRatio = Mathf.Clamp(m_fustrumCamera.speed/m_cameraSpeedThreshold, 0f, 1f);

        // Combine all ratios into a singular LOD value\
        float lodRatio =  Mathf.Clamp(distanceRatio, m_camSpeedModifier.Evaluate(camSpeedRatio)*m_minCamSpeedWeight, distanceRatio);
        

        /*
        float radiusRatio = Mathf.Clamp(Vector2.Distance(viewportPoint, 0.5f*Vector2.one), 0f,1f);

        // Combine all ratios into a singular LOD value
        float fovRatio = horizontalRatio * (1f-m_fovWeight) + verticalRatio * m_fovWeight;
        float lodRatio = fovRatio * radiusRatio + distanceRatio * m_FOVToDistanceRatioModifier.Evaluate(1f-radiusRatio);
        lodRatio = Mathf.Clamp(lodRatio, m_camSpeedModifier.Evaluate(camSpeedRatio)*m_minCamSpeedWeight, lodRatio);
        */

        // Calculate the LOD based on the resulting final lod value.
        bool lodSet = false;
        for(int i = 0; i < m_levels.Count; i++) {
            if (lodRatio <= m_levels[i].threshold) {
                m_filter.mesh = m_levels[i].model;
                m_renderer.SetMaterials(m_levels[i].materials);
                lodSet = true;
                break;
            }
        }
        m_renderer.enabled = lodSet;
    }
}
