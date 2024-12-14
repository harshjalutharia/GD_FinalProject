using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "Cutscene", menuName = "Generation/Cutscene", order = 2)]
public class Cutscene : ScriptableObject
{
    public string sceneName;
    public List<Slide> slides;
}

[System.Serializable]
public class Slide {
    public Sprite slide;
    public float displayTime;
    public float transitionTime;
}