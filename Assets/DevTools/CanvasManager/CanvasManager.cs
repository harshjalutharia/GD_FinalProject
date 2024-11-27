using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CanvasManager : MonoBehaviour
{
    public static CanvasManager current;
    public enum CanvasState { Off, InTransition, OutTransition, On }

    [System.Serializable]
    public class _Canvas {
        public string name;
        public CanvasGroup[] groups;
        public CanvasState state;
        public float delayTime, fadeInTime, fadeOutTime, maxTime;
        public bool initializeOnStart;
        public KeyCode debugKeyToggle;

        public float alpha = 0f;
        public IEnumerator transition;
        public string nextCanvasAfterOff;
        public UnityEvent nextEventsAfterOff;
    }

    [SerializeField] private _Canvas[] m_inspectorCanvases;
    [SerializeField] private bool m_useDebugToggles = false;
    public Dictionary<string, _Canvas> canvases;
    
    // This one is a bit unique. This class supports the ability to transition between canvases as needed.
    // To do this, we have a list of active canvases. There can be one, two, several even. This list can also be empty.
    // When a canvas is ADDED to this list, we initiate a coroutine specifically for this canvas to transition in.
    // Simultaneously, when a canvas is REMOVED, then we initiate a coroutine to handle the out transition.
    // If a _Canvas has a max time set to 0, then after the fade in we do not initialize a fade out transition.
    // However, if a _Canvas has some max time that's bigger than 0, then we initiate the fade in, then wait, then ininitialize a fade-out within the fade-in

    private void Awake() {
        current = this;

        canvases = new Dictionary<string, _Canvas>();
        foreach(_Canvas c in m_inspectorCanvases) {
            // Check if a canvas exists with the same name
            if (canvases.ContainsKey(c.name)) {
                Debug.Log($"A canvas with the name '{c.name}' already exists. Overwriting...");
                canvases[c.name] = c;
            } else {
                canvases.Add(c.name, c);
            }
        }

        foreach(_Canvas c in canvases.Values) {
            if (c.initializeOnStart) SetCanvas(c, true);
            else ForceCanvas(c, false);
        }
    }

    private void Update() {
        if (!m_useDebugToggles) return;
        foreach(_Canvas c in canvases.Values) {
            if (Input.GetKeyDown(c.debugKeyToggle)) SetCanvas(c, (c.state == CanvasState.Off || c.state == CanvasState.OutTransition) ? true : false);
        }
    }

    private void OnDestroy() {
        // Terminate safely all coroutines that are actively running
        StopAllCoroutines();
    }

    public void SetCanvas(string name, bool setTo) {
        // can't do anything if we don't have a canvas with the provided name
        if (!canvases.ContainsKey(name)) {
            Debug.LogError($"No canvas with the name '{name}' found. Cannot set to this canvas");
            return;
        }
        SetCanvas(canvases[name], setTo);
    }

    public void SetCanvas(_Canvas c, bool setTo) {
        // If the canvas's current state matches the `setTo`, then nothing needs to be done.
        if ( 
            (setTo && (c.state == CanvasState.On || c.state == CanvasState.InTransition)) 
            || (!setTo && (c.state == CanvasState.Off || c.state == CanvasState.OutTransition) )
        ) return;

        // So either 1. we want it on and it's either in the transition out or off, or 2. we want it off and it's either in the transition in or on...
        // We terminate its current transition and instantiate a new one.
        if (c.transition != null) StopCoroutine(c.transition);
        if (setTo) c.transition = FadeInTransition(c);
        else c.transition = FadeOutTransition(c);
        StartCoroutine(c.transition);
    }

    public void ForceCanvas(string name, bool setTo) {
        // can't do anything if we don't have a canvas with the provided name
        if (!canvases.ContainsKey(name)) {
            Debug.LogError($"No canvas with the name '{name}' found. Cannot set to this canvas");
            return;
        }
        ForceCanvas(canvases[name], setTo);
    }

    public void ForceCanvas(_Canvas c, bool setTo) {
        if (c.transition != null) StopCoroutine(c.transition);
        foreach(CanvasGroup cg in c.groups) ToggleCanvasGroup(cg, setTo);
    }

    private IEnumerator FadeInTransition(_Canvas g) {
        // If the canvas has a fade in time of 0 or smaller, then just set it to active.
        if (g.fadeInTime <= 0f || g.alpha == 1f) {
            g.alpha = 1f;
            g.state = CanvasState.On;
            foreach(CanvasGroup cg in g.groups) ToggleCanvasGroup(cg, true);
            yield break;
        }

        if (g.delayTime > 0f) yield return new WaitForSeconds(g.delayTime);

        // From here, we calculate the time to transition
        float startTime = Time.time;
        float endTime = startTime + g.fadeInTime;
        float startBuffer = g.fadeInTime * g.alpha;
        float currentTime = Time.time + startBuffer;
        g.state = CanvasState.InTransition;

        while(currentTime < endTime) {
            currentTime = Time.time + startBuffer;
            g.alpha = (currentTime-startTime)/g.fadeInTime;
            foreach(CanvasGroup cg in g.groups) SetCanvasGroupAlpha(cg, g.alpha);
            yield return null;
        }

        // At this point, make the groups really interactable and active
        g.alpha = 1f;
        g.state = CanvasState.On;
        foreach(CanvasGroup cg in g.groups) ToggleCanvasGroup(cg, true);

        // If the canvas has a `maxTime` that's bigger than 0, then we wait, and then instantiate a fade-out afterwards
        if (g.maxTime <= 0) yield break;
        yield return new WaitForSeconds(g.maxTime);
        g.transition = FadeOutTransition(g);
        StartCoroutine(g.transition);
    }

    private IEnumerator FadeOutTransition(_Canvas g) {
        // If the canvas has a fade in time of 0 or smaller, then just set it to active.
        if (g.fadeOutTime <= 0f || g.alpha == 0f) {
            g.alpha = 0f;
            g.state = CanvasState.Off;
            foreach(CanvasGroup cg in g.groups) ToggleCanvasGroup(cg, false);
            yield break;
        }

        // From here, we calculate the time to transition
        float startTime = Time.time;
        float endTime = startTime + g.fadeOutTime;
        float startBuffer = g.fadeOutTime * (1f-g.alpha);
        float currentTime = Time.time+startBuffer;
        g.state = CanvasState.OutTransition;

        while(currentTime < endTime) {
            currentTime = Time.time+startBuffer;
            g.alpha = 1f - ((currentTime-startTime)/g.fadeOutTime);
            foreach(CanvasGroup cg in g.groups) SetCanvasGroupAlpha(cg, g.alpha);
            yield return null;
        }

        // At this point, make the groups really interactable and active
        g.alpha = 0f;
        g.state = CanvasState.Off;
        foreach(CanvasGroup cg in g.groups) ToggleCanvasGroup(cg, false);

        // If this canvas has a next group to activate, toggle it
        if (g.nextCanvasAfterOff != null && g.nextCanvasAfterOff.Length > 0) SetCanvas(g.nextCanvasAfterOff, true);
        g.nextEventsAfterOff?.Invoke();
    }



    /* ==== STATIC FUNCTIONS ====
    These are PURELY for external use by outside classes, if they have their own system for canvases
    These are also used by the this class's monobehavior for controlling canvases.
    ========================== */

    public static void ToggleCanvasGroup(CanvasGroup group, bool setTo) {
        float setToFloat = setTo ? 1f : 0f;
        group.alpha = setTo ? 1f : 0f;
        group.interactable = setTo;
        group.blocksRaycasts = setTo;
    }

    private static IEnumerator ToggleCanvasGroupCoroutine(CanvasGroup group, bool setTo, float transitionTime) {
        float endAlpha = 1f, startAlpha = 0f, timePassed = 0f;
        if (!setTo) {
            startAlpha = 1f;
            endAlpha = 0f;
        }
        
        while(timePassed/transitionTime < 1f) {
            timePassed += Time.deltaTime;
            group.alpha = Mathf.Lerp(startAlpha, endAlpha, timePassed/transitionTime);
            yield return null;
        }

        group.interactable = setTo;
        group.blocksRaycasts = setTo;
    }

    public static void SetCanvasGroupAlpha(CanvasGroup group, float setTo) {
        group.alpha = setTo;
    }
    public static void SetCanvasGroupInteractable(CanvasGroup group, bool setTo) {
        group.interactable = setTo;
        group.blocksRaycasts = setTo;
    }
}
