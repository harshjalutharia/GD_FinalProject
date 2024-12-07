using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkyboxController : MonoBehaviour
{
    [Header("=======SkyBoxPreset=======")]
    // Variables to hold the different skyboxes
    [SerializeField] private Material daySkyBox;
    [SerializeField] private Material twilightSkyBox;
    [SerializeField] private Material nightSkyBox;

    [Header("=======Control for Time=======")]
    // Variable to store the light source (GameObject)
    [SerializeField] private GameObject sun;

    private Light sunlight; // Light component of the sun GameObject

    // Time of the day
    [SerializeField, Range(0, 24)] private float timeOfDay;

    // Variable to store the speed of sun rotation
    [SerializeField] private float sunRotationSpeed;

    //controlling time
    private float targetTimeOfDay;
    [SerializeField] private float timeToChange = 3f; // Duration to change time smoothly (in seconds)
    private float timeChangeProgress = 0f; // Progress of the time change

    // Lighting
    [Header("=======LightingPreset=======")]
    [SerializeField] private Gradient skyColor;
    [SerializeField] private Gradient equatorColor;
    [SerializeField] private Gradient sunColor;

    // Function to see the change in editor
    private void OnValidate()
    {
        if (sun != null) // Ensure sun is assigned before updating
        {
            UpdateSunRotation();
            UpdateLighting();
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        sunlight = sun.GetComponent<Light>();
        // Optionally, set the default skybox
        TurningMorning();
    }

    // Update is called once per frame
    void Update()
    {
        // Check for key presses and change skybox
        if (Input.GetKeyDown(KeyCode.Alpha8)) // Key 8
        {
            TurningMorning();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha9)) // Key 9
        {
            TurningTwilight();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha0)) // Key 0
        {
            TurningEvening();
        }

        // Gradually change the time of day
        if (timeChangeProgress < timeToChange)
        {
            timeChangeProgress += Time.deltaTime;
            timeOfDay = Mathf.Lerp(timeOfDay, targetTimeOfDay, timeChangeProgress / timeToChange);
            if (timeChangeProgress >= timeToChange)
            {
                timeOfDay = targetTimeOfDay; // Ensure we hit the exact target when done
            }
        }

        // Rotate the sun smoothly over time
        timeOfDay += Time.deltaTime * sunRotationSpeed;
        if (timeOfDay > 24f) timeOfDay -= 24f;

        UpdateSunRotation();
        UpdateLighting();
    }


    public void TurningMorning(){
        StartChangeTimeOfDay(11f); // Gradually change to noon
        RenderSettings.skybox = daySkyBox;
    }

    public void TurningTwilight(){
        StartChangeTimeOfDay(17f); // Gradually change to twilight
        RenderSettings.skybox = twilightSkyBox;
    }

    public void TurningEvening(){
        StartChangeTimeOfDay(24f); // Gradually change to midnight
        RenderSettings.skybox = nightSkyBox;
    }
    



    // Function to start the time of day change
    private void StartChangeTimeOfDay(float newTime)
    {
        targetTimeOfDay = newTime;
        
        timeChangeProgress = 0f; // Reset progress for smooth transition
    }


    // Function to update sun rotation
    private void UpdateSunRotation()
    {
        // this is for rotation of the light,
        // the -90 to 270 is the rotation cycle of the light 
        float sunRotation = Mathf.Lerp(-90, 270, timeOfDay / 24f);
        sun.transform.rotation = Quaternion.Euler(sunRotation, 0f, 0f);
    }

    // Function to update the lighting
    private void UpdateLighting()
    {

        sunlight = sun.GetComponent<Light>();

        float timeFraction = timeOfDay / 24f;

        // Update ambient lighting
        RenderSettings.ambientEquatorColor = equatorColor.Evaluate(timeFraction);
        RenderSettings.ambientSkyColor = skyColor.Evaluate(timeFraction);

        // Update sunlight color
        sunlight.color = sunColor.Evaluate(timeFraction);
    }
}
