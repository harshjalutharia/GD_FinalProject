using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "Cutscene", menuName = "Generation/Cutscene", order = 2)]
public class Cutscene : ScriptableObject
{
    public string sceneName;
    public List<Sprite> slides;
    public float slideDisplayTime = 2f;
    public float slideTransitionTime = 1f;
}
