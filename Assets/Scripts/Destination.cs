using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Destination : MonoBehaviour
{
    [SerializeField] private Collider m_collider;

    private void Awake() {
        m_collider.isTrigger = true;        
    }

    private void OnTriggerEnter(Collider other) {
        if (other.gameObject.tag != "Player") return;
        Debug.Log("Destination Reached!");
        CanvasController.current.ToggleWinScreen(true);
    }
}
