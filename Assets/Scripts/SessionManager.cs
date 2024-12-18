using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Extensions;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Random = UnityEngine.Random;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SessionManager : MonoBehaviour
{
    public static SessionManager current;

    [Header("=== Player Settings ===")]
    [SerializeField, Tooltip("The player themselves")]          private GameObject m_player;
    [SerializeField, Tooltip("Player camera")]                  private CameraFader m_playerCameraFader;
    [SerializeField, Tooltip("The radius from map center where we place the destination and start points for the player")]
                                                                private float m_spawnRadius = 100f;
    [SerializeField, Tooltip("Given a spawn point for either player or destination, the radius of checking for flattest area")]
                                                                private float m_spawnFlatRadius = 10f;
    [SerializeField, Tooltip("The player's start position")]    private Vector3 m_playerStartPosition = Vector3.zero;
    [SerializeField, Tooltip("The player's end position")]      private Vector3 m_playerEndPosition = Vector3.zero;
    public Vector3 playerStartPosition => m_playerStartPosition;
    public Vector3 playerEndPosition => m_playerEndPosition;
    [SerializeField, Tooltip("The player's view has been initialized")] private bool m_playerViewInitialized = false;

    [Header("=== Necessary Generators/Maps/Managers ===")]
    [SerializeField, Tooltip("The noise map that generates terrain.")]          private NoiseMap m_terrainGenerator;
    public NoiseMap terrainGenerator => m_terrainGenerator;
    [SerializeField, Tooltip("Layer Mask for the terrain")]                     private LayerMask m_terrainGroundMask;
    [SerializeField, Tooltip("Specific reference to the voronoi map used")]     private VoronoiMap m_voronoiMap;

    [Header("=== Optional Generators/Maps/Managers ===")]
    [SerializeField, Tooltip("The Fustrum Manager that manages fustrum chunks")]                private FustrumManager m_fustrumManager;
    [SerializeField, Tooltip("The landmark generator that places buildings")]                   private LandmarkGenerator m_landmarkGenerator;
    [SerializeField, Tooltip("The vegetation generator to place trees")]                        private VegetationGenerator m_vegetationGenerator;
    [SerializeField, Tooltip("The grass generator to place grass")]                             private VegetationGenerator m_grassGenerator;
    [SerializeField, Tooltip("The gem generator to place gems")]                                private GemGenerator m_gemGenerator;

    [Header("=== Loading Menu Elements ===")]
    [SerializeField, Tooltip("The input action reference that allows the player to skip the initial cutscene")] private InputActionReference m_skipCutsceneAction;
    [SerializeField, Tooltip("The input action reference that allows the player to  jump")]                     private InputActionReference m_playerJumpAction;
    [SerializeField, Tooltip("Did the terrain finish generating?")]   private bool m_terrainGenerated = false;
    [SerializeField, Tooltip("Did the trees finish generating?")]   private bool m_treesGenerated = false;
    [SerializeField, Tooltip("Did the trees finish generating?")]   private bool m_gemsGenerated = false;
    private bool m_allGeneratorsCompleted => m_terrainGenerated && m_treesGenerated && m_gemsGenerated;
    [Space]
    [SerializeField, Tooltip("The gem camera")]                                                 private Camera m_gemCamera;

    [Header("=== Loading Slideshow Settings ===")]
    [SerializeField, Tooltip("Check to determine that the slideshow has active")]    private bool m_slideshowActive = false;
    private IEnumerator m_slideshowCoroutine;
    [SerializeField, Tooltip("Image component for displaying slides")] private Image m_slideshowImage;
    [SerializeField, Tooltip("List of slides to display")] private List<Sprite> m_slides;
    [SerializeField, Tooltip("Time to display each slide")] private float m_slideDisplayTime = 2f;
    [SerializeField, Tooltip("Transition time between slides")] private float m_slideTransitionTime = 1f;

    #if UNITY_EDITOR
    [SerializeField] private bool m_drawGizmos = false;
    private void OnDrawGizmos() {
        if (m_terrainGenerator == null) return;
        Vector2 debugMapCenter = m_terrainGenerator.mapCenter;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(m_terrainGenerator.transform.position + new Vector3(debugMapCenter.x, 50f, debugMapCenter.y), m_spawnRadius);

    }
    #endif

    private void Awake() {
        current = this;
        m_skipCutsceneAction.action.performed += InitializePlayerViewAction;
    }

    private void Start() {
        // At the start, we expect to be able to read the seed info from SessionMemory. We then apply that seed to ALL randomization engines.
        // SetSeed() will do this for us automatically.
        SetSeed(SessionMemory.current.seed);
        StartCoroutine(StartSession());
    }

    private IEnumerator StartSession() {
        // Disable the player jump
        m_playerJumpAction.action.Disable();

        // Show the loading menu
        m_slideshowCoroutine = PlaySlideshowCoroutine();
        StartCoroutine(m_slideshowCoroutine);
        CameraController.current.ToggleLoadingCamera(true, true);
        CameraController.current.ToggleThirdPersonCamera(false, false);
        CameraController.current.ToggleFirstPersonCamera(false, false);
        m_gemCamera.gameObject.SetActive(false);
        
        // Initialize terrain generation
        yield return m_terrainGenerator.GenerateMapCoroutine();

        // Designate the start and end positions of the player.
        GenerateStartAndEnd(out m_playerStartPosition, out m_playerEndPosition);

        // Teleport the player to the start postiion
        m_player.transform.position = m_playerStartPosition;

        // After generating the terrain, we can now initiate a bunch of other managers, generators, etc.
        //if (m_fustrumManager != null)       m_fustrumManager.Initialize();                                                                                  // Fustrum culling
        if (m_landmarkGenerator != null) m_landmarkGenerator.GenerateLandmarks();//(m_playerEndPosition, m_playerStartPosition, m_terrainGenerator);          // Landmark generation
        if (m_vegetationGenerator != null)  m_vegetationGenerator.GenerateVegetation();                                                                     // Vegetation generation                                                                        // Rock generation
        if (m_grassGenerator != null)       m_grassGenerator.GenerateVegetation();                                                                          // Grass generation
        // Gem generation
        if (m_gemGenerator != null) {
            m_gemGenerator.GenerateSmallGems();
            m_gemGenerator.GenerateDestinationGem(m_playerEndPosition);
        }
        // Game tracker
        StartCoroutine(PlayerMovement.current.ActivatePlayer());
    }

    private IEnumerator PlaySlideshowCoroutine() {

        // Initialize the boolean that indicates that the slideshow has started
        m_slideshowActive = true;

        // Initialize the slideshow image alpha to 0
        Color color = m_slideshowImage.color;
        color.a = 0f;
        m_slideshowImage.color = color;

        // For each slide
        for (int i = 0; i < m_slides.Count; i++)
        {
            // Set the sprite
            m_slideshowImage.sprite = m_slides[i];

            // Fade in the image
            yield return StartCoroutine(FadeImage(m_slideshowImage, 0f, 1f, m_slideTransitionTime));

            // Wait for display time
            yield return new WaitForSeconds(m_slideDisplayTime);

            // Fade out the image
            yield return StartCoroutine(FadeImage(m_slideshowImage, 1f, 0f, m_slideTransitionTime));
        }

        // After finishing the slideshow:
        m_slideshowActive = false;
        // If everything else is done, just enable skip instead of auto-initializing.
        if (m_allGeneratorsCompleted && !m_playerViewInitialized)
        {
            // Enable the skip action so the user must press it
            m_skipCutsceneAction.action.Enable();
        }


    }

    private IEnumerator FadeImage(Image image, float startAlpha, float endAlpha, float duration)
    {
        float time = 0f;
        Color color = image.color;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            color.a = alpha;
            image.color = color;
            yield return null;
        }
        color.a = endAlpha;
        image.color = color;
    }

    private void InitializePlayerViewAction(InputAction.CallbackContext ctx) { InitializePlayerView(); }
    private void InitializePlayerView() {
        // Disable the input action to prevent double-clicking
        m_skipCutsceneAction.action.Disable();
        m_playerJumpAction.action.Enable();
        m_playerViewInitialized = true;

        // Stop the slideshow coroutine, if it hasn't completed yet.
        if (m_slideshowActive) StopCoroutine(m_slideshowCoroutine);

        // Hide the loading menu
        CanvasController.current.ToggleLoadingScreen(false, false);
        CameraController.current.enabled = true;
        m_gemCamera.gameObject.SetActive(true);
        
        // Show the stamina UI
        CanvasController.current.ToggleGameplay(true);

        // Start playing BGM
        SoundManager.current.PlayBGM(0);

        // Let the camera fade in
        m_playerCameraFader.FadeIn();       

        // If a gem generator exists, toggle the view cam
        if (m_gemGenerator != null) m_gemGenerator.ToggleViewCheck(true);
        // StartCoroutine(PlayerMovement.current.Tutorial());
    }

    public void SetSeed(string newSeed) {
        int validNewSeed;
        if (newSeed.Length > 0 && int.TryParse(newSeed, out validNewSeed)) {
            SetSeed(validNewSeed);
            return;
        }
        SetSeed(Random.Range(0, 1000001));
    }
    public void SetSeed(int newSeed) {
        Random.InitState(newSeed);
        m_terrainGenerator.SetSeed(newSeed);
        if (m_vegetationGenerator != null)  m_vegetationGenerator.SetSeed(newSeed);
        if (m_grassGenerator != null)       m_grassGenerator.SetSeed(newSeed);
        if (m_gemGenerator != null)         m_gemGenerator.SetSeed(newSeed);
    }

    public void ReturnToStart() {   StartCoroutine(ReturnToStartCoroutine());   }
    private IEnumerator ReturnToStartCoroutine() {
        // Fade out the player and their controls
        m_playerCameraFader.FadeOut();
        
        // Fade out menues
        CanvasController.current.ToggleAllCanvases(false);
        yield return null;

        // Migrate to start
        SceneManager.LoadScene(0);
    }

    private IEnumerator ToggleCanvasGroupCoroutine(CanvasGroup group, bool setTo, float transitionTime) {
        float endAlpha = 1f, startAlpha = 0f, timePassed = 0f;
        if (setTo) {
            startAlpha = 0f;
            endAlpha = 1f;
        } else {
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

    private void GenerateStartAndEnd(out Vector3 startPos, out Vector3 endPos) {
        // Step 1: Generate a random angle between 0 and 360. Also determine the map center
        float randomAngle = Random.Range(0f,360f);
        Vector3 mapCenter = m_terrainGenerator.mapCenter3D;

        // Step 2. Determine the direction where the direction is heading based on the provided angle
        Vector3 direction = Quaternion.Euler(0f, randomAngle, 0f) * Vector3.right * m_spawnRadius;

        // Step 3. Given the direction, Determine the flat end point
        Vector3 tempDestination = mapCenter + direction;
        endPos = new Vector3(
            tempDestination.x,
            m_terrainGenerator.QueryHeightAtWorldPos(tempDestination.x, tempDestination.z),
            tempDestination.z
        );

        // Step 3.5. Determine the optimal place to put the destination point - aka the flattest point.
        int minDX = Mathf.RoundToInt(tempDestination.x - m_spawnFlatRadius);
        int maxDX = Mathf.RoundToInt(tempDestination.x + m_spawnFlatRadius);
        int minDY = Mathf.RoundToInt(tempDestination.z - m_spawnFlatRadius);
        int maxDY = Mathf.RoundToInt(tempDestination.z + m_spawnFlatRadius);
        float destinationNormalDot = -1f;
        for(int dx = minDX; dx <= maxDX; dx++) {
            for(int dy = minDY; dy <= maxDY; dy++) {
                Vector3 destNormal = m_terrainGenerator.QueryMapNormalAtWorldPos(dx, dy, m_terrainGroundMask, out int coordDX, out int coordDY, out float queryDestY);
                float destDot = Vector3.Dot(Vector3.up, destNormal);
                if (destDot > destinationNormalDot) {
                    destinationNormalDot = destDot;
                    endPos = new Vector3(coordDX, queryDestY, coordDY);
                }
            }
        }

        // Step 4. Given the direction, Determine the flat end point
        Vector3 tempStart = mapCenter - direction;
        startPos = new Vector3(
            tempStart.x,
            m_terrainGenerator.QueryHeightAtWorldPos(tempStart.x, tempStart.z),
            tempStart.z
        );
        // Step 3.5. Determine the optimal place to put the destination point - aka the flattest point.
        int minSX = Mathf.RoundToInt(tempStart.x - m_spawnFlatRadius);
        int maxSX = Mathf.RoundToInt(tempStart.x + m_spawnFlatRadius);
        int minSY = Mathf.RoundToInt(tempStart.z - m_spawnFlatRadius);
        int maxSY = Mathf.RoundToInt(tempStart.z + m_spawnFlatRadius);
        float startNormalDot = -1f;
        for(int sx = minSX; sx <= maxSX; sx++) {
            for(int sy = minSY; sy <= maxSY; sy++) {
                Vector3 startNormal = m_terrainGenerator.QueryMapNormalAtWorldPos(sx, sy, m_terrainGroundMask, out int coordSX, out int coordSY, out float queryStartY);
                float startDot = Vector3.Dot(Vector3.up, startNormal);
                if (startDot > startNormalDot) {
                    startNormalDot = startDot;
                    startPos = new Vector3(coordSX, queryStartY, coordSY);
                }
            }
        }

        
        /*
        VoronoiMap.VoronoiSegment[] segments = vMap.voronoiSegments;
        Vector2Int[] centroids = vMap.voronoiCentroids;

        startIndex = 0;
        endIndex = segments.Length-1;
        int curAttempts = 0;
        while(curAttempts < numAttempts) {
            startIndex = vMap.GetRandomSegmentIndex(true);
            endIndex = vMap.GetRandomSegmentIndex(true);
            if (segments[startIndex].isBorder || segments[endIndex].isBorder) {
                curAttempts += 1;
                continue;
            }
            Vector2Int startCentroid = centroids[startIndex];
            Vector2Int endCentroid = centroids[endIndex];
            if (Vector2Int.Distance(startCentroid, endCentroid) >= minDistance) break;
            curAttempts += 1;
        }
        Vector3 vStartPos = vMap.GetRandomPositionInSegment(startIndex, true, 100);
        Vector3 vEndPos = vMap.GetRandomPositionInSegment(endIndex, true, 100);
        float startHeight = terrainMap.QueryHeightAtWorldPos(vStartPos.x, vStartPos.z, out int startX, out int startY);
        float endHeight = terrainMap.QueryHeightAtWorldPos(vEndPos.x, vEndPos.z, out int endX, out int endY);
        startPos = new Vector3(vStartPos.x, startHeight, vStartPos.z);
        endPos = new Vector3(vEndPos.x, endHeight, vEndPos.z);
        */
    }

    public void GenerationCompleted(string generatorName) {
        Debug.Log($"Finished Generating {generatorName}");
        switch(generatorName) {
            case "Terrain":
                m_terrainGenerated = true;
                break;
            case "Trees":
                m_treesGenerated = true;
                break;
            case "Gems":
                m_gemsGenerated = true;
                break;
            default: break;
        }

        if (m_allGeneratorsCompleted)
        {
            Debug.Log("All generators completed!");
            // If slideshow already finished and player view is not initialized, enable skip.
            if (m_slideshowActive)
            {
                CanvasController.current.ToggleLoadingIconsGroup(false);
                m_skipCutsceneAction.action.Enable();
            }
            // Otherwise, we simply hop to initializing the player view
            else if (!m_playerViewInitialized)
            {
                CanvasController.current.ToggleLoadingIconsGroup(false);
                m_skipCutsceneAction.action.Enable();
                InitializePlayerView();
            }
        }

    }

    private void OnDestroy() {
        StopAllCoroutines();
        m_skipCutsceneAction.action.performed -= InitializePlayerViewAction;
    }

}
