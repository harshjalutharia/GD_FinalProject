/// <summary>
/// Asym frustum.
/// based on http://paulbourke.net/stereographics/stereorender/
/// and http://answers.unity3d.com/questions/165443/asymmetric-view-frusta-selective-region-rendering.html
/// </summary>
using UnityEngine;
using System.Collections;
using System;

[RequireComponent(typeof(Camera))]
[ExecuteInEditMode]
public class AsymFrustum : MonoBehaviour
{
    private Camera m_cam;
    public Camera m_refCam;
    [Range(-1f,1f)] public float topFustrumModifier = 1f;
    [Range(-1f,1f)] public float bottomFustrumModifier = 1f;
    [Range(-1f,1f)] public float leftFrustumModifier = 1f;
    [Range(-1f,1f)] public float rightFrustumModifier = 1f;

    private void OnValidate() {
        m_cam = GetComponent<Camera>();
        if (m_cam != null && m_refCam != null) {
            ApplyAsymmetricFrustum(m_refCam, m_cam);
        }
    }

    public void ApplyAsymmetricFrustum(Camera in_camera, Camera out_camera) {
        // Get the reference camera's settings
        float nearClip = in_camera.nearClipPlane;
        float farClip = in_camera.farClipPlane;
        float fov = in_camera.fieldOfView;
        float aspectRatio = in_camera.aspect;

        // Calculate the half vertical field of view based on the camera's aspect ratio and field of view
        float top = Mathf.Tan(Mathf.Deg2Rad * fov * 0.5f) * nearClip;
        float bottom = -top;

        // Calculate the left and right frustum planes based on the original aspect ratio and the modifier
        float right = aspectRatio * nearClip;
        float left = -right;

        // Modify the right plane to create the asymmetric frustum
        top *= topFustrumModifier;
        bottom *= bottomFustrumModifier;
        left *= leftFrustumModifier * 0.5f;
        right *= rightFrustumModifier * 0.5f;

        // Set up the new asymmetric projection matrix
        Matrix4x4 projectionMatrix = Matrix4x4.Frustum(left, right, bottom, top, nearClip, farClip);

        // Apply the custom projection matrix to the camera
        out_camera.projectionMatrix = projectionMatrix;
    }

    /*
    private Camera m_cam;
    public Camera m_refCam;
	public float left = -0.2f;
    public float right = 0.2f;
    public float top = 0.2f;
    public float bottom = -0.2f;

    private void OnValidate() {
        m_cam = GetComponent<Camera>();
        if (m_refCam != null) {
            float refFOV = 
        }
    }

    private void LateUpdate () {
        if (m_cam == null) return;
        Matrix4x4 m = PerspectiveOffCenter(left, right, bottom, top, m_cam.nearClipPlane, m_cam.farClipPlane );
        m_cam.projectionMatrix = m;
    }

    public static Matrix4x4 PerspectiveOffCenter(
        float left, float right, float bottom, float top,
        float near, float far
    ) {
        float x = (2.0f * near) / (right - left);
        float y = (2.0f * near) / (top - bottom);
        float a = (right + left) / (right - left);
        float b = (top + bottom) / (top - bottom);
        float c = -(far + near) / (far - near);
        float d = -(2.0f * far * near) / (far - near);
        float e = -1.0f;

        Matrix4x4 m = new Matrix4x4();
        m[0,0] = x; m[0,1] = 0f; m[0,2] = a; m[0,3] = 0f;
        m[1,0] = 0f; m[1,1] = y; m[1,2] = b; m[1,3] = 0f;
        m[2,0] = 0f; m[2,1] = 0f; m[2,2] = c; m[2,3] = d;
        m[3,0] = 0f; m[3,1] = 0f; m[3,2] = e; m[3,3] = 0f;
        return m;
    }
    */
}