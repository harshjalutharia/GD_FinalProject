using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkyboxController : MonoBehaviour
{
    [Header("=======SkyBoxPreset=======")]
    [SerializeField] private Material daySkyBox;
    [SerializeField] private Material twilightSkyBox;
    [SerializeField] private Material nightSkyBox;

    [Header("=======CloudlyFading Preset=======")]
    [SerializeField] private GameObject Cloud; // Reference to the Cloud GameObject
    private FadeObject fadeObject; // Reference to the FadeObject script on Cloud

    [Header("=======Control for Time=======")]
    [SerializeField] private GameObject sun;
    private Light sunlight;
    [SerializeField, Range(0, 24)] private float timeOfDay;
    [SerializeField] private float sunRotationSpeed;

    private float targetTimeOfDay;
    [SerializeField] private float timeToChange = 3f;
    private float timeChangeProgress = 0f;

    [Header("=======LightingPreset=======")]
    [SerializeField] private Gradient skyColor;
    [SerializeField] private Gradient equatorColor;
    [SerializeField] private Gradient sunColor;

    private void OnValidate()
    {
        if (sun != null)
        {
            UpdateSunRotation();
            UpdateLighting();
        }
    }

    void Start()
    {
        sunlight = sun.GetComponent<Light>();
        fadeObject = Cloud.GetComponent<FadeObject>();

        if (fadeObject == null)
        {
            Debug.LogError("FadeObject script is not attached to the Cloud GameObject.");
        }

        TurningMorning();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha8)) TurningMorning();
        else if (Input.GetKeyDown(KeyCode.Alpha9)) TurningTwilight();
        else if (Input.GetKeyDown(KeyCode.Alpha0)) TurningEvening();

        if (timeChangeProgress < timeToChange)
        {
            timeChangeProgress += Time.deltaTime;
            timeOfDay = Mathf.Lerp(timeOfDay, targetTimeOfDay, timeChangeProgress / timeToChange);
            if (timeChangeProgress >= timeToChange)
            {
                timeOfDay = targetTimeOfDay;
            }
        }

        timeOfDay += Time.deltaTime * sunRotationSpeed;
        if (timeOfDay > 24f) timeOfDay -= 24f;

        UpdateSunRotation();
        UpdateLighting();
    }

    public void TurningMorning()
    {
        StartFadeTransition(daySkyBox);
        //RenderSettings.skybox = daySkyBox;
        StartChangeTimeOfDay(11f);
    }

    public void TurningTwilight()
    {
        StartFadeTransition(twilightSkyBox);
        //RenderSettings.skybox = twilightSkyBox;
        StartChangeTimeOfDay(17f);
    }

    public void TurningEvening()
    {
        StartFadeTransition(nightSkyBox);
        //RenderSettings.skybox = nightSkyBox;
        StartChangeTimeOfDay(24f);
    }

    private void StartFadeTransition(Material skyboxMaterial)
    {
        sunlight = sun.GetComponent<Light>();
        if (fadeObject != null)
        {
            sunlight.intensity = 1.5f;
            fadeObject.StartFadeIn(); // Start fade-in before changing
            StartCoroutine(WaitForFadeOut(skyboxMaterial));
        }
    }

    private IEnumerator WaitForFadeOut(Material skyboxMaterial)
    {
        sunlight = sun.GetComponent<Light>();
        yield return new WaitForSeconds(fadeObject.fadeDuration); // Wait for fade-in to complete
        RenderSettings.skybox = skyboxMaterial;
        fadeObject.StartFadeOut(); // Start fade-out after changing
        sunlight.intensity = 1.0f;
    }

    private void StartChangeTimeOfDay(float newTime)
    {
        targetTimeOfDay = newTime;
        timeChangeProgress = 0f;
    }

    private void UpdateSunRotation()
    {
        float sunRotation = Mathf.Lerp(-90, 270, timeOfDay / 24f);
        sun.transform.rotation = Quaternion.Euler(sunRotation, 0f, 0f);
    }

    private void UpdateLighting()
    {
        sunlight = sun.GetComponent<Light>();

        float timeFraction = timeOfDay / 24f;
        RenderSettings.ambientEquatorColor = equatorColor.Evaluate(timeFraction);
        RenderSettings.ambientSkyColor = skyColor.Evaluate(timeFraction);
        sunlight.color = sunColor.Evaluate(timeFraction);
    }
}
