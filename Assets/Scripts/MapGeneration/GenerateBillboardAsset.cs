using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class GenerateBillboardAsset : MonoBehaviour
{
    [SerializeField] private int m_numPoints = 15;
    [SerializeField] private float m_radius = 10f;
    [SerializeField] private Vector3 m_pivotPoint;
    [SerializeField] private Camera m_renderCamera;
    [SerializeField] private Renderer m_rendererRef;
    [SerializeField] private RenderTexture m_renderTextureRef;
    [Space]
    [SerializeField] private Vector3[] m_points;
    [SerializeField, Range(0f,1f)] private float m_cameraRotation = 0f;

    #if UNITY_EDITOR
    [SerializeField] private bool m_drawGizmos = false;
    [SerializeField] private Color m_pointColor = Color.blue;

    private void OnDrawGizmos() {
        m_points = new Vector3[m_numPoints];
        float angleDiff = 360f/m_numPoints;
        Vector3 pivot = transform.position + m_pivotPoint;
        for(int i = 0; i < m_numPoints; i++) {
            Vector3 direction = Quaternion.Euler(0f,angleDiff*i,0f) * Vector3.forward * m_radius;
            m_points[i] = pivot + direction;
        }

        if (m_renderCamera != null) {
            if (m_rendererRef != null) {
                Vector3 min = m_rendererRef.bounds.min;
                Vector3 viewport = m_renderCamera.WorldToViewportPoint(min);
                Debug.Log(viewport.y);
            }
            int index = Mathf.RoundToInt(m_cameraRotation * (m_numPoints-1));
            m_renderCamera.transform.position = m_points[index];
            m_renderCamera.transform.LookAt(pivot);
        }

        if (!m_drawGizmos) return;
        Gizmos.color = m_pointColor;
        for(int i = 0; i < m_numPoints; i++) {
            Gizmos.DrawSphere(m_points[i], 0.25f);
        }
    }
    #endif

    public void GeneratePNG() {
        if (m_renderCamera == null || m_renderTextureRef == null) return;
        m_renderCamera.Render();

        RenderTexture.active = m_renderTextureRef;
        Texture2D toPrint = new Texture2D(m_renderTextureRef.width, m_renderTextureRef.height, TextureFormat.RGBA32, false);
        toPrint.ReadPixels(new Rect(0, 0, m_renderTextureRef.width, m_renderTextureRef.height), 0, 0);
        toPrint.Apply();

        byte[] bytes = toPrint.EncodeToPNG();
        var dirPath = Application.dataPath + "/SaveImages/";
        if(!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);
        File.WriteAllBytes(dirPath + "Generated" + ".png", bytes);

        #if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
        #endif
    }

    private void OnValidate() {
        if (m_numPoints < 1) m_numPoints = 1;
    }
}
