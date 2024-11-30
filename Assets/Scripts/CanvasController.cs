using UnityEngine;
using UnityEngine.InputSystem;

public class CanvasController : MonoBehaviour
{
    public static CanvasController current;

    [Header("=== Settings ===")]
    [SerializeField, Tooltip("Should we enable each input on start?")] private bool m_enableOnStart = true;

    [Header("=== Loading Canvas ===")]
    [SerializeField, Tooltip("Reference to the canvas group for the menu screen")] private CanvasGroup m_loadingCanvasGroup;
    [SerializeField, Tooltip("Is the loading canvas active?")] private bool m_loadingCanvasActive = false;
    public bool loadingCanvasActive => m_loadingCanvasActive;

    [Header("=== Win Canvas ===")]
    [SerializeField, Tooltip("Reference to the canvas group for the win screen")] private CanvasGroup m_winCanvasGroup;
    [SerializeField, Tooltip("Is the win canvas active?")] private bool m_winCanvasActive = false;
    public bool winCanvasActive => m_winCanvasActive;
    
    [Header("=== Menu Canvas ===")]
    [SerializeField, Tooltip("Input action ref. for the menu button")] private InputActionReference m_menuActionReference;
    [SerializeField, Tooltip("Reference to the canvas group for the menu screen")] private CanvasGroup m_menuCanvasGroup;
    [SerializeField, Tooltip("Is the menu canvas active?")] private bool m_menuCanvasActive = false;
    public bool menuCanvasActive => m_menuCanvasActive;

    [Header("=== Debug Movement ===")]
    [SerializeField, Tooltip("Input action ref. for the debug movement")]   private InputActionReference m_movementActionReference;
    [SerializeField, Tooltip("Reference to the movement canvas group")]     private CanvasGroup m_movementCanvasGroup;
    [SerializeField, Tooltip("Is the movement canvas active?")]             private bool m_movementCanvasActive = false;
    public bool movementCanvasActive => m_movementCanvasActive;

    [Header("=== Debug FPS ===")]
    [SerializeField, Tooltip("Input action ref. for the debug FPS")]    private InputActionReference m_fpsActionReference;
    [SerializeField, Tooltip("Reference to the fps canvas group")]      private CanvasGroup m_fpsCanvasGroup;
    [SerializeField, Tooltip("Is the fps canvas active?")]              private bool m_fpsCanvasActive = false;
    public bool fpsCanvasActive => m_fpsCanvasActive;

    private void Awake() {
        // Set current reference
        current = this;
        
        // Check all canvas active states.
        m_loadingCanvasActive = m_loadingCanvasGroup.alpha > 0f;
        m_winCanvasActive = m_winCanvasGroup.alpha > 0f;
        m_menuCanvasActive = m_menuCanvasGroup.alpha > 0f;
        m_movementCanvasActive = m_movementCanvasGroup.alpha > 0f;
        m_fpsCanvasActive = m_fpsCanvasGroup.alpha > 0f;

        // Enable all actions if toggled
        if (m_enableOnStart) {
            m_menuActionReference.action.Enable();
            m_movementActionReference.action.Enable();
            m_fpsActionReference.action.Enable();
        }
    }

    private void OnEnable() {
        m_menuActionReference.action.started += ToggleMenuAction;
        m_movementActionReference.action.started += ToggleMovementAction;
        m_fpsActionReference.action.started += ToggleFPSAction;
    }

    private void OnDisable() {
        m_menuActionReference.action.started -= ToggleMenuAction;
        m_movementActionReference.action.started -= ToggleMovementAction;
        m_fpsActionReference.action.started -= ToggleFPSAction;
    }

    public void ToggleLoadingScreen(bool setTo) {
        m_loadingCanvasActive = setTo;
        SetCanvasGroupAlpha(m_loadingCanvasGroup, setTo);
    }

    public void ToggleWinScreen(bool setTo) {
        m_winCanvasActive = setTo;
        ToggleCanvasGroup(m_winCanvasGroup, setTo);
    }

    public void ToggleMenuAction(InputAction.CallbackContext ctx) { ToggleMenu(!m_menuCanvasActive); }
    public void ToggleMenu(bool setTo) {
        m_menuCanvasActive = setTo;
        ToggleCanvasGroup(m_menuCanvasGroup, setTo);
        Cursor.lockState = (setTo) 
            ? CursorLockMode.None
            : CursorLockMode.Locked;
    }

    public void ToggleMovementAction(InputAction.CallbackContext ctx) { ToggleMovement(!m_movementCanvasActive); }
    public void ToggleMovement(bool setTo) {
        m_movementCanvasActive = setTo;
        SetCanvasGroupAlpha(m_movementCanvasGroup, setTo);
    }
    
    public void ToggleFPSAction(InputAction.CallbackContext ctx) { ToggleFPS(!m_fpsCanvasActive); }
    public void ToggleFPS(bool setTo) {
        m_fpsCanvasActive = setTo;
        SetCanvasGroupAlpha(m_fpsCanvasGroup, setTo);
    }
    
    public void ToggleAllCanvases(bool setTo) {
        ToggleLoadingScreen(setTo);
        ToggleWinScreen(setTo);
        ToggleMenu(setTo);
        ToggleMovement(setTo);
        ToggleFPS(setTo);
    }

    // === STATIC CLASS FUNCTIONS === //

    public static void ToggleCanvasGroup(CanvasGroup group, bool setTo) {
        float setToFloat = setTo ? 1f : 0f;
        group.alpha = setTo ? 1f : 0f;
        group.interactable = setTo;
        group.blocksRaycasts = setTo;
    }
    public static void SetCanvasGroupAlpha(CanvasGroup group, float setTo) {
        group.alpha = setTo;
    }
    public static void SetCanvasGroupAlpha(CanvasGroup group, bool setTo) {
        group.alpha = setTo ? 1f : 0f;
    }
    public static void SetCanvasGroupInteractable(CanvasGroup group, bool setTo) {
        group.interactable = setTo;
        group.blocksRaycasts = setTo;
    }
}
