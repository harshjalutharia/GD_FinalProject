using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class FPS : MonoBehaviour
{
    //Declare these in your class
    private int m_frameCounter = 0;
    private float m_timeCounter = 0.0f;
    private int m_lastFramerate = 0;
    [SerializeField] private float m_refreshTime = 0.5f;
    [SerializeField] private TextMeshProUGUI m_textbox;

    void Update() {
        if(m_timeCounter < m_refreshTime) {
            m_timeCounter += Time.deltaTime;
            m_frameCounter++;
        }
        else {
            //This code will break if you set your m_refreshTime to 0, which makes no sense.
            m_lastFramerate = (int)((float)m_frameCounter/(float)m_timeCounter);
            m_frameCounter = 0;
            m_timeCounter = 0.0f;
        }

        if (m_textbox != null) m_textbox.text = m_lastFramerate.ToString();
    }

    private void OnValidate() {
        if (m_refreshTime <= 0.1f) m_refreshTime = 0.1f;
    }
}