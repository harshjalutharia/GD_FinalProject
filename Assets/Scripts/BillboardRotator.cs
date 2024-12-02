using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BillboardRotator : MonoBehaviour
{
    private void LateUpdate() {
        if (!CameraController.current.enabled)return;
        transform.rotation = CameraController.current.currentCamera.transform.rotation;
    }
}
