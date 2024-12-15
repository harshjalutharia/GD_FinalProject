using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SpeechBubbleController : MonoBehaviour
{
    public static SpeechBubbleController current;
    
    [SerializeField, Tooltip("The Text Mesh Pro object")]
    private TextMeshPro textMeshPro;

    [SerializeField, Tooltip("Current showing text")]
    private String targetText;
    
    [SerializeField, Tooltip("Buffered text list")]
    private Queue<String> textBuffer = new Queue<String>();

    [SerializeField, Tooltip("How long bubble lasts by default")]
    private float defaultDuration;

    [SerializeField, Tooltip("How fast it types")]
    private float typingInterval = 0.08f;

    [SerializeField, Tooltip("How fast it shows up")]
    private float showUpTime = 0.3f;

    [SerializeField, Tooltip("state of showing")]
    private bool showing;

    [SerializeField, Tooltip("scale of the bubble")]
    private Vector3 scale = new Vector3(2, 2, 2);

    [Tooltip("Text templates")] 
    public TextTemplate[] textTemplates;

    [Serializable]
    public struct TextTemplate
    {
        public String title, text;
    }
    

    private void Awake()
    {
        current = this;
    }

    void Start()
    {
        showing = false;
        transform.localScale = Vector3.zero;
    }

    // Update is called once per frame
    void Update()
    {
        if (showing)
        {
            transform.rotation = ThirdPersonCam.current.transform.rotation;
        }
    }

    public TextTemplate[] GetTextTemplates() { return textTemplates; }

    public void ShowText(String text)
    {
        textBuffer.Enqueue(text);   //add text to buffer
        // if current showing dialog, don't start another Coroutine
        if (!showing)
        {
            StartCoroutine(BubbleLifeCircle());
        }
    }
    
    private IEnumerator BubbleLifeCircle()
    {
        if (textBuffer.Count == 0)
            yield break;
        targetText = textBuffer.Dequeue();  //fetch text from buffer
        showing = true;
        
        // show up bubble
        float startTime = Time.time;
        while (Time.time - startTime <= showUpTime)
        {
            yield return null;
            transform.localScale = Vector3.Lerp(Vector3.zero, scale, (Time.time - startTime) / showUpTime);
        }
        
        // type the text
        while (textMeshPro.text.Length < targetText.Length)
        {

            textMeshPro.text = textMeshPro.text + targetText[textMeshPro.text.Length];
            yield return new WaitForSeconds(typingInterval);
        }

        yield return new WaitForSeconds(defaultDuration);
        
        // hide the bubble
        startTime = Time.time;
        while (Time.time - startTime <= showUpTime)
        {
            yield return null;
            transform.localScale = Vector3.Lerp(scale, Vector3.zero, (Time.time - startTime) / showUpTime);
        }
        
        textMeshPro.text = "";
        
        //if text buffer is not null, restart the life circle
        if (textBuffer.Count > 0)
        {
            StartCoroutine(BubbleLifeCircle());
        }
        else
        {
            showing = false;
        }
    }
}
