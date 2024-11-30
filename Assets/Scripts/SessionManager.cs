using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Extensions;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class SessionManager : MonoBehaviour
{
    public static SessionManager current;

    [Header("=== Player Settings ===")]
    [SerializeField, Tooltip("The player themselves")]          private GameObject m_player;
    [SerializeField, Tooltip("Player camera")]                  private CameraFader m_playerCameraFader;
    [SerializeField, Tooltip("The player's start position")]    private Vector3 m_playerStartPosition = Vector3.zero;
    [SerializeField, Tooltip("The player's end position")]      private Vector3 m_playerEndPosition = Vector3.zero;
    public Vector3 playerStartPosition => m_playerStartPosition;
    public Vector3 playerEndPosition => m_playerEndPosition;

    [Header("=== Necessary Generators/Maps/Managers ===")]
    [SerializeField, Tooltip("The noise map that generates terrain.")]          private NoiseMap m_terrainGenerator;
    [SerializeField, Tooltip("Specific reference to the voronoi map used")]     private VoronoiMap m_voronoiMap;

    [Header("=== Optional Generators/Maps/Managers ===")]
    [SerializeField, Tooltip("The Fustrum Manager that manages fustrum chunks")]                private FustrumManager m_fustrumManager;
    [SerializeField, Tooltip("The landmark generator that places buildings")]                   private LandmarkGenerator m_landmarkGenerator;
    [SerializeField, Tooltip("The vegetation generator to place trees")]                        private VegetationGenerator m_vegetationGenerator;
    [SerializeField, Tooltip("The grass generator to place grass")]                             private VegetationGenerator m_grassGenerator;
    [SerializeField, Tooltip("The gem generator to place gems")]                                private GemGenerator m_gemGenerator;

    [Header("=== Menus ===")]
    [SerializeField, Tooltip("The gem camera")]                                                 private Camera m_gemCamera;

    [Header("=== Loading Slideshow Settings ===")]
    [SerializeField, Tooltip("Image component for displaying slides")] private Image m_slideshowImage;
    [SerializeField, Tooltip("List of slides to display")] private List<Sprite> m_slides;
    [SerializeField, Tooltip("Time to display each slide")] private float m_slideDisplayTime = 2f;
    [SerializeField, Tooltip("Transition time between slides")] private float m_slideTransitionTime = 1f;

    private void Awake() {
        current = this;
    }

    private void Start() {
        // At the start, we expect to be able to read the seed info from SessionMemory. We then apply that seed to ALL randomization engines.
        // SetSeed() will do this for us automatically.
        SetSeed(SessionMemory.current.seed);

        // Show the loading menu
        StartCoroutine(PlaySlideshowCoroutine());
        CameraController.current.ToggleLoadingCamera(true, true);
        CameraController.current.ToggleThirdPersonCamera(false, false);
        CameraController.current.ToggleFirstPersonCamera(false, false);
        m_gemCamera.gameObject.SetActive(false);
        
        // Initialize terrain generation
        m_terrainGenerator.GenerateMap();

        // Designate the start and end positions of the player.
        // The start and end positions rely on a representative voronoi map to let us know which regions are safe.
        GenerateStartAndEnd(
            m_terrainGenerator, m_voronoiMap, 
            300f, 20, 
            out m_playerStartPosition, out int playerStartPositionIndex, 
            out m_playerEndPosition, out int playerEndPositionIndex
        );

        // Teleport the player to the start postiion
        m_player.transform.position = m_playerStartPosition;

        // After generating the terrain, we can now initiate a bunch of other managers, generators, etc.
        //if (m_fustrumManager != null)       m_fustrumManager.Initialize();                                                                                  // Fustrum culling
        if (m_landmarkGenerator != null)    m_landmarkGenerator.GenerateLandmarks(m_playerEndPosition, m_playerStartPosition, m_terrainGenerator);          // Landmark generation
        if (m_vegetationGenerator != null)  m_vegetationGenerator.GenerateVegetation();                                                                     // Vegetation generation                                                                        // Rock generation
        if (m_grassGenerator != null)       m_grassGenerator.GenerateVegetation();                                                                          // Grass generation
        // Gem generation
        if (m_gemGenerator != null) {
            m_gemGenerator.GenerateSmallGems();
            m_gemGenerator.GenerateDestinationGem(m_playerEndPosition);
        }
        // Game tracker
        StartCoroutine(m_player.GetComponent<PlayerMovement>().ActivatePlayer());        
        Invoke(nameof(InitializePlayerView), 5f);

    }

    private IEnumerator PlaySlideshowCoroutine()
    {

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

    private void InitializePlayerView() {
        // Hide the loading menu
        CanvasController.current.ToggleLoadingScreen(false);
        CameraController.current.enabled = true;
        m_gemCamera.gameObject.SetActive(true);

        // Start playing BGM
        SoundManager.current.PlayBGM();

        // Let the camera fade in
        m_playerCameraFader.FadeIn();       

        // If a gem generator exists, toggle the view cam
        if (m_gemGenerator != null) m_gemGenerator.ToggleViewCheck(true);

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

    private void GenerateStartAndEnd(NoiseMap terrainMap, VoronoiMap vMap, float minDistance, int numAttempts, out Vector3 startPos, out int startIndex, out Vector3 endPos, out int endIndex) {
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
    }

    public void DebugFinishGeneration(string generatorName) {
        Debug.Log($"Finished Generating {generatorName}");
    }

}
