using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkyboxController : MonoBehaviour
{
    public static SkyboxController current;
    
    [Header("=======SkyBoxPreset=======")]
    [SerializeField, Tooltip("Skybox Present for the Day")] private Material daySkyBox;
    [SerializeField, Tooltip("Skybox Present for the Twilight")] private Material twilightSkyBox;
    [SerializeField, Tooltip("Skybox Present for the Night")] private Material nightSkyBox;

    [Header("=======CloudlyFading Preset=======")]
    [SerializeField, Tooltip("Transition Skybox")] private GameObject Cloud; // Reference to the Cloud GameObject
    private FadeObject fadeObject; // Reference to the FadeObject script on Cloud

    [Header("=======Control for Time=======")]
    [SerializeField, Tooltip("Directional Light for sun")] private GameObject sun;
    private Light sunlight;
    [SerializeField, Range(0, 24), Tooltip("Control for Day of Time, it associates with directional light")] private float timeOfDay;
    [SerializeField] private float sunRotationSpeed;

    private float targetTimeOfDay;
    [SerializeField] private float timeToChange = 3f;
    private float timeChangeProgress = 0f;
    
    private enum Daytime
    {
        Morning1,
        Morning2,
        Twilight,
        Evening
    }
    [SerializeField] private Daytime daytime = Daytime.Morning1;

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
    
    private void Awake()
    {
        current = this;
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
        StartCoroutine(StartFadeTransition(daySkyBox, 11f));
        //RenderSettings.skybox = daySkyBox;
        if (RainController.current != null) RainController.current.ToggleRain(false, false); // stop the rain
        if (SoundManager.current != null) SoundManager.current.PlayBGM(0);
        if (SoundManager.current != null) SoundManager.current.ToggleRainSound(false);
        if (SoundManager.current != null) SoundManager.current.ToggleThunderSFX(false);
    }

    public void TurningTwilight()
    {
        StartCoroutine(StartFadeTransition(twilightSkyBox, 17f));
        //RenderSettings.skybox = twilightSkyBox;
        if (RainController.current != null) RainController.current.ToggleRain(true, false); // start to rain, but slow
        if (SoundManager.current != null) SoundManager.current.PlayBGM(1);
        if (SoundManager.current != null) SoundManager.current.ToggleRainSound(true, false);
        if (SoundManager.current != null) SoundManager.current.ToggleThunderSFX(false);
    }

    public void TurningEvening()
    {
        StartCoroutine(StartFadeTransition(nightSkyBox, 24f));
        //RenderSettings.skybox = nightSkyBox;
        if (RainController.current != null) RainController.current.ToggleRain(true, true); // start to rain, but fast
        if (SoundManager.current != null) SoundManager.current.PlayBGM(2);
        if (SoundManager.current != null) SoundManager.current.ToggleRainSound(true, true);
        if (SoundManager.current != null) SoundManager.current.ToggleThunderSFX(true);
    }

    public void TimeChangeAuto()
    {
        if (daytime == Daytime.Morning1){
            daytime = Daytime.Morning2;  // do not change time for the first time
           
        }
        else if (daytime == Daytime.Morning2)
        {
            TurningTwilight();
            daytime = Daytime.Twilight;
           
        }
        else if (daytime == Daytime.Twilight)
        {
            TurningEvening();
            daytime = Daytime.Evening;
            
        }
        else if (daytime == Daytime.Evening)
        {
            TurningMorning();
            daytime = Daytime.Morning1;
            
        }
    }

    private IEnumerator StartFadeTransition(Material skyboxMaterial, float timeTo)
    {
        sunlight = sun.GetComponent<Light>();
        if (fadeObject != null)
        {
            
            yield return new WaitForSeconds(2.5f);
            Debug.Log("Wait for 2.5 sec");
            sunlight.intensity = 1.5f;
            StartChangeTimeOfDay(timeTo);
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
