using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;


public class PlayerMovement : MonoBehaviour
{
    public static PlayerMovement current;
    
    [Header("Force Settings")]
    [Tooltip("The force pushing character to walk")]
    public float moveForce;
    
    [Tooltip("The force pushing character to sprint")]
    public float sprintForce;
    
    [Tooltip("Continuous horizontal force applied during flight")]
    public float flightForce;
    
    [Tooltip("Continuous vertical force applied during flight")]
    public float flightAscendForce;
    
    [Tooltip("Instantaneous force exerted during a jump")]
    public float jumpForce;
    
    [Tooltip("Used to avoid double jump")]
    public float jumpCooldown;
    
    
    [Header("Speed threshold Settings")]
    [Tooltip("Controls the max walking speed on a level surface. Vmax=moveSpeed/groundDrag")]
    public float slideSpeedThreshold;

    [Tooltip("If player's dropping speed on ground larger than this, start sliding")]
    public float slidingDropThreshold;
    
    [Tooltip("Controls the max sprint speed on a level surface")]
    public float maxSprintSpeed;

    [Tooltip("Start slide if current speed larger than slideSpeedThreshold + slideStartSpeedOffset.")]
    public float slideStartSpeedOffset;
    
    [Tooltip("Max speed of flight.")]
    public float maxFlightSpeed;
    
    [Tooltip("Max horizontal speed of paragliding.")]
    public float maxParaglidingSpeed;

    [Tooltip("Max vertical speed of paragliding.")]
    public float paraglidingFallingSpeed;
    
    [Tooltip("Start paragliding if current speed larger than slideSpeedThreshold + paraglidingStartSpeedOffset.")]
    public float paraglidingStartSpeedOffset;
    
    [Header("(READ ONLY) Drag Coefficients")]
    [SerializeField, Tooltip("READ ONLY. Limit the max walking speed, Vmax=moveSpeed/groundDrag")]
    private float groundDrag;
    
    [SerializeField, Tooltip("READ ONLY. Limit the max sprint speed, Vmax=moveSpeed/groundDragSprint")]
    private float groundDragSprint;
    
    [SerializeField, Tooltip("READ ONLY. Limit the horizontal max fly speed. Vmax_fly=moveSpeed/airDragHorizontal")] 
    private float airDragHorizontal;

    [SerializeField, Tooltip("READ ONLY. Limit the horizontal max paragliding speed.")] 
    private float airDragParaglidingHorizontal;
    
    [SerializeField, Tooltip("Only limit the max downward velocity speed. Vmax_drop=g/airDragVertical")] 
    private float airDragVertical;
    
    
    [Header("Other Resistance")]
    [Tooltip("When the user has no direction input, applies the friction")]
    public float friction;
    
    [Tooltip("Do not apply friction if the angle of the slop is larger than this value")]
    public float frictionSlopAngle;
    
    private PhysicMaterial physicMaterial;
    
    [Tooltip("Resistance Force while sliding")] 
    public float slideResistance;


    [Header("Stamina Settings")] 
    [Tooltip("READ ONLY. Maximum accessible stamina"), SerializeField]
    public float maxAccessibleStamina;

    [Tooltip("The stamina each gem gives")]
    public float staminaPerGem;
    
    [Tooltip("Maximum stamina for flight and sprint at current moment")]
    public float maxFlightStamina;
    
    [Tooltip("Stamina consumed by jumping")]
    public float jumpStaminaConsume;
    
    [SerializeField, Tooltip("Current stamina")] 
    private float flightStamina;
    
    [Tooltip("Stamina consumed per second of flight")]
    public float flightStaminaDecreaseSpeed;
    
    [Tooltip("Stamina consumed per second of sprinting")]
    public float sprintStaminaDecreaseSpeed;

    [Tooltip("Could not start sprinting if stamina low that this value")] 
    public float sprintMinStamina;
    
    [Tooltip("Stamina regained per second while on the ground and not sprinting")]
    public float flightStaminaRefillSpeed;

    
    [Header("Input Reference Binds")]
    [SerializeField, Tooltip("The action input of player jumping.")]
    private InputActionReference jumpActionReference;
    
    [SerializeField, Tooltip("The action input of player flying.")]
    private InputActionReference flyActionReference;
    
    [SerializeField, Tooltip("The action input of player sprinting.")]
    private InputActionReference SprintActionReference;
    
    [SerializeField, Tooltip("The action input of player toggle to sprint.")]
    private InputActionReference SprintSwitchActionReference;
    
    
    private PlayerControls controls; 

    private InputAction directionInput;
    
    //private InputAction jumpInput;
    
    //private InputAction sprintInput;

    //private InputAction switchSprintInput;
    
    private float horizontalInput;
    
    private float verticalInput;

    [Header("Ground Check")]
    
    [Tooltip("Positions to perform ground detect")]
    public Transform[] groundDetections = new Transform[2];
    
    private RaycastHit[] groundHits = new RaycastHit[2];
    
    private Vector3[] normProject = new Vector3[2];   // the normal projected to player

    [Tooltip("Keep player on the ground if the angular change of slope is less than this value")]
    public float slopAngularChangeTolerance;
    
    [Tooltip("Set to everything is OK")]
    public LayerMask groundMask;
    
    [Header("Private States of movement")]
    [SerializeField] 
    private bool grounded;

    [SerializeField]
    private bool grabGround;
    
    [SerializeField]
    private bool readyToJump;
    
    [SerializeField] 
    private bool requestJump;

    [SerializeField] 
    private bool preparingJump;
    
    [SerializeField] 
    private bool sprinting;

    [SerializeField] 
    private bool sliding;

    // [SerializeField] 
    // private bool keepSprint;
    
    [SerializeField] 
    private bool requestFlight;

    //[SerializeField] 
    //private bool holdingMap;
    [SerializeField] 
    private Vector3 horizontalVelocity;
    
    [SerializeField] 
    private bool flightActivated;
    
    [SerializeField] 
    private bool sprintActivated;

    [SerializeField] 
    private bool paraglidingActivated;

    [SerializeField, Tooltip("Private state of SFX")]
    private SoundFXState sfxState;
    
    private Vector3 moveDirection;

    private Rigidbody rb;

    [Header("Animation")]
    [Tooltip("Animation Controller")]
    public Animator animator;

    [SerializeField, Tooltip("READ ONLY. Variables bind for animation control")]
    private AnimationVars animationVars;

    [Header("Others")] 
    [Tooltip("Used to determine the player direction")]
    public Transform orientation;

    [Tooltip("player turn speed")]
    public float rotationSpeed;

    [Tooltip("Gem Generator GameObject")] 
    public GemGenerator gemGenerator;
    
    [Tooltip("particle system of collecting gem")]
    public ParticleSystem collectGemParticles;

    [Tooltip("if activate cape")] 
    public bool usingCape = false;
    
    [Tooltip("game object of the cape")] 
    public SimulateCapeController cape;

    //[Tooltip("GameObject of the map")] 
    //public GameObject mapObj;
    
    //[Tooltip("GameObject of the compass")] 
    //public GameObject compassObj;

    public event Action OnLanding;
    
    private Transform playerObj;
    
    [Header("Debug UI Element")] 
    public TextMeshProUGUI debugText1;
    public TextMeshProUGUI debugText2;

    private void Awake()
    {
        current = this;
        controls = InputManager.Instance.controls;
        directionInput = controls.Player.Move;
    }
    
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        playerObj = animator.transform;
        //mapObj.SetActive(false);
        //compassObj.SetActive(false);

        readyToJump = true;
        maxAccessibleStamina = maxFlightStamina;
        flightStamina = maxFlightStamina;
        
        groundDrag = moveForce / slideSpeedThreshold;
        groundDragSprint = sprintForce / maxSprintSpeed;
        airDragHorizontal = flightForce / maxFlightSpeed;
        airDragParaglidingHorizontal = flightForce / maxParaglidingSpeed;
        airDragVertical = Physics.gravity.magnitude / paraglidingFallingSpeed;

        physicMaterial = GetComponent<CapsuleCollider>().material;

        // initially do not have special movement
        flightActivated = false;
        paraglidingActivated = false;
        sprintActivated = true;
        

    }

    private void Update()
    {
        DealInput();  // actually it now deals with different states rather than input
        
        // Refill stamina when grounded
        if (grounded && !sprinting)
        {
            flightStamina += flightStaminaRefillSpeed * Time.deltaTime;
            flightStamina = Mathf.Clamp(flightStamina, 0, maxFlightStamina);
        }
        else if(sprinting)
        {
            flightStamina -= sprintStaminaDecreaseSpeed * Time.deltaTime;
            flightStamina = Mathf.Clamp(flightStamina, 0, maxFlightStamina);
        }
        else if (requestFlight)
        {
            flightStamina -= flightStaminaDecreaseSpeed * Time.deltaTime;
            flightStamina = Mathf.Clamp(flightStamina, 0, maxFlightStamina);
        }
        
        SetAnimation();

        SetSound();
        
        // debug message
        float horizontalSpeed = new Vector3(rb.velocity.x, 0, rb.velocity.z).magnitude;
        float overAllSpeed = rb.velocity.magnitude;
        
        debugText1.text = normProject[0] + " " + normProject[1] + "slop: " + GetSlopAngle();
        debugText2.text = "Velocity:" + rb.velocity + " \nHorizontal Speed:" + horizontalSpeed + " Speed:" + overAllSpeed;
        
        // Debug.Log(normProject[0] + " " + normProject[1]);
        //Debug.DrawRay(groundHits[0].point, groundHits[0].normal, Color.blue, 0, false);
        //Debug.DrawRay(groundHits[1].point, groundHits[1].normal, Color.green, 0, false);
        // cheat
        if (controls.Debug.ActivateFlight.WasPressedThisFrame())
        {
            ActivateFlight();
            ActivateSprint();
            ActivateParagliding();
            SoundManager.current.PlaySFX("Cheat");
        }
    }

    private void FixedUpdate()
    {
        // apply two ground normal check
        for (int i = 0; i < 2; i++)
        {
            Physics.Raycast(groundDetections[i].position, Vector3.down, out groundHits[i], Mathf.Infinity,
                groundMask);
        }
        // project the norm of the ground
        normProject[0] =
            animator.transform.InverseTransformDirection(Vector3.ProjectOnPlane(groundHits[0].normal,
                animator.transform.right));
        normProject[1] =
            animator.transform.InverseTransformDirection(Vector3.ProjectOnPlane(groundHits[1].normal,
                animator.transform.right));
        
        // determine the ground grab
        grabGround = normProject[1].z >= normProject[0].z && //normProject[1].z >= 0 && 
                     Vector3.Angle(normProject[0], normProject[1]) < slopAngularChangeTolerance && grounded &&
                     !preparingJump && !requestFlight;
        
        // if (!grabGround)
        //     Debug.Log((normProject[1].z >= normProject[0].z) + " " + Vector3.Angle(normProject[0], normProject[1]));
        
        Collider[] colliders = Physics.OverlapSphere(orientation.position, 0.1f, groundMask);

        bool latestGround = grounded;
        grounded = colliders.Length > 0;
        if (!latestGround && grounded)
        {
            OnLanding?.Invoke();  // invoke landing event
        }
        
        
        MovePlayer();
        horizontalVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
    }
    
    private void OnEnable()
    {
        controls.Enable(); 
        jumpActionReference.action.started += OnJumpInput;      
        jumpActionReference.action.Enable();
        flyActionReference.action.performed += OnFlightInput;   
        flyActionReference.action.canceled += OffFlightInput;   
        flyActionReference.action.Enable();
        SprintActionReference.action.performed += OnSprintInput;
        SprintActionReference.action.canceled += OffSprintInput;
        SprintActionReference.action.Enable();
        SprintSwitchActionReference.action.started += OnSprintSwitchInput;
        SprintSwitchActionReference.action.Enable();
    }

    private void OnDisable()
    {
        controls.Disable(); 
        jumpActionReference.action.started -= OnJumpInput;
        flyActionReference.action.performed -= OnFlightInput;   
        flyActionReference.action.canceled -= OffFlightInput;  
        SprintActionReference.action.performed -= OnSprintInput;
        SprintActionReference.action.canceled -= OffSprintInput;
        SprintSwitchActionReference.action.started -= OnSprintSwitchInput;
    }

    private void DealInput()
    {
        // update state of sliding
        if (sliding)
        {
            sliding = (new Vector3(rb.velocity.x, 0f, rb.velocity.z).magnitude > slideSpeedThreshold ||
                       rb.velocity.y < -1 * slidingDropThreshold) && grounded && !sprinting;
        }
        else
        {
            sliding = (new Vector3(rb.velocity.x, 0f, rb.velocity.z).magnitude >
                          slideSpeedThreshold + slideStartSpeedOffset || rb.velocity.y < -1 * slidingDropThreshold) &&
                      grounded && !sprinting;
        }
        
        // update the map state
        //if (mapInput.IsPressed() && grounded && (!sliding || rb.velocity.magnitude < 2))
        if (CameraController.current.mapInputActive && grounded && (!sliding || rb.velocity.magnitude < 2))
        {
            // if holding map, ignore all the other input
            horizontalInput = 0;
            verticalInput = 0;
            sprinting = false;
            //keepSprint = false;
            return;
        }
        
        if (CameraController.current.firstPersonCameraActive) // if fp camera active, ignore all the input
            return;
        
        // use new input system to get the input value
        Vector2 movement2D = directionInput.ReadValue<Vector2>();
        horizontalInput = movement2D.x;
        verticalInput = movement2D.y;
        
        
        // update the sprinting state
        if (sprintActivated && grounded && sprinting && moveDirection.magnitude > 0.02f && !sliding)
        {
            sprinting = flightStamina > 0;  // end sprinting if stamina less than 0
        }
        else
        {
            sprinting = false;
        }
        
    }

    
    //------------------ Event for the inputs---------------------
    // event of jump input triggered
    private void OnJumpInput(InputAction.CallbackContext ctx)
    {
        if(readyToJump && grounded && flightStamina > jumpStaminaConsume && !CameraController.current.mapInputActive)
        {
            readyToJump = false;
            Invoke(nameof(ResetJump), jumpCooldown);
            requestJump = true;
            preparingJump = true;
        }
    }

    // event of flight input triggered
    private void OnFlightInput(InputAction.CallbackContext ctx)
    {
        if (flightActivated && !grounded && flightStamina > 0 && !CameraController.current.mapInputActive)
        {
            requestFlight = true;
        }
        else
        {
            requestFlight = false;
        }
    }

    private void OffFlightInput(InputAction.CallbackContext ctx)
    {
        requestFlight = false;
    }

    private void OnSprintInput(InputAction.CallbackContext ctx)
    {
        if (sprintActivated && grounded && moveDirection.magnitude > 0.02f && !sliding &&
            !CameraController.current.mapInputActive)
        {
            if (sprinting)
            {
                sprinting = flightStamina > 0;  // end sprinting if stamina less than 0
            }
            else
            {
                sprinting = flightStamina > sprintMinStamina;  // could only start sprint if stamina larger than certain value
            }
        }
    }

    private void OffSprintInput(InputAction.CallbackContext ctx)
    {
        sprinting = false;
    }
    
    private void OnSprintSwitchInput(InputAction.CallbackContext ctx)
    {
        if (sprintActivated && grounded && moveDirection.magnitude > 0.02f && !sliding &&
            !CameraController.current.mapInputActive)
        {
            if (sprinting)
            {
                sprinting = flightStamina > 0;  // end sprinting if stamina less than 0
            }
            else
            {
                sprinting = flightStamina > sprintMinStamina;  // could only start sprint if stamina larger than certain value
                // if (sprinting)
                // {
                //     keepSprint = true;
                // }
            }
        }
    }
    
    //------------------ End Event for the inputs---------------------
    
    // called in FixedUpdate
    private void MovePlayer()
    {
        // rotate the player according to the velocity
        
        if (!CameraController.current.firstPersonCameraActive && rb.velocity.magnitude >= 0.05f)
            playerObj.forward = Vector3.Slerp(playerObj.forward, new Vector3(rb.velocity.x, 0, rb.velocity.z).normalized, Time.fixedDeltaTime * rotationSpeed);
        else if (CameraController.current.firstPersonCameraActive)
        {
            playerObj.forward = Vector3.Slerp(playerObj.forward, orientation.forward, Time.fixedDeltaTime * rotationSpeed);
        }
        // calculate input movement direction
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;
        
        //set friction if player has no input
        if (moveDirection.magnitude < 0.05f && grounded && GetSlopAngle() <= frictionSlopAngle)
        {
            physicMaterial.dynamicFriction = friction;
            physicMaterial.staticFriction = friction;
        }
        else
        {
            physicMaterial.dynamicFriction = 0;
            physicMaterial.staticFriction = 0;
        }
        
        // begin horizontal movement
        // on ground
        if (grounded)
        {
            Vector3 moveForce = moveDirection.normalized * (sprinting ? sprintForce : this.moveForce) - CalculateResistance();
            rb.AddForce(moveForce, ForceMode.Force);
        }
        // in the air
        else if (!grounded)
        {
            Vector3 moveForce = moveDirection.normalized * flightForce - CalculateResistance();
            rb.AddForce(moveForce, ForceMode.Force);
            
            // limit descent speed if player has input
            if (paraglidingActivated && rb.velocity.y < 0 && moveDirection.magnitude > 0.05f)
            {
                rb.AddForce(airDragVertical * rb.velocity.y * Vector3.down, ForceMode.Force);
            }
        }
        // end horizontal movement
        
        // begin vertical movement
        
        if (grabGround)     // stick the player to the ground
        {
            //rb.AddForce(animator.transform.TransformDirection(-0.5f * (groundHits[0].normal + groundHits[0].normal)) * 50, ForceMode.Force);
            rb.AddForce(50 * -0.5f * (groundHits[0].normal + groundHits[0].normal), ForceMode.Force);
        }
        
        rb.useGravity = true;
        if (requestJump) // jump control
        {
            SoundManager.current.PlaySFX("Jump");   // play jump sfx one shot
            requestJump = false;
            Invoke(nameof(Jump), 0.15f);
            animationVars.requestJump = true;
            // Invoke(nameof(ResetAnimatorRequestJump), 0.2f);
        }
        else if (requestFlight)     // flight control
        {
            FlyUp();
        }
        
    }

    // apply force to jump
    private void Jump()
    {
        animationVars.requestJump = false;
        
        flightStamina -= jumpStaminaConsume;
        flightStamina = Mathf.Clamp(flightStamina, 0, maxFlightStamina);
        // reset y velocity
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }
    
    // apply force to fly
    private void FlyUp()
    {
        rb.useGravity = false;
        rb.AddForce(Vector3.up * flightAscendForce, ForceMode.Acceleration);
    }
    
    
    private void ResetJump()
    {
        readyToJump = true;
        preparingJump = false;
    }
    
    
    private Vector3 CalculateResistance()
    {
        
        float resistanceMag = 0;
        if (grounded && !sprinting)  // walking drag
        {
            // the resistance increases linearly with horizontal velocity. But has a max value
            resistanceMag = Mathf.Clamp(horizontalVelocity.magnitude * groundDrag, 0, moveForce + slideResistance);
        }
        else if (grounded && sprinting)
        {
            resistanceMag = Mathf.Clamp(horizontalVelocity.magnitude * groundDragSprint, 0, sprintForce*2);
        }
        else if (!grounded && !animationVars.paragliding)
        {
            resistanceMag = horizontalVelocity.magnitude * airDragHorizontal;
        }
        else  // !grounded && animationVars.paragliding
        {
            resistanceMag = horizontalVelocity.magnitude * airDragParaglidingHorizontal;
        }
        Vector3 resistanceForce = resistanceMag * horizontalVelocity.normalized;
        return resistanceForce;
        
    }

    private float GetSlopAngle()
    {
        float slopAngle = Vector3.Angle((groundHits[0].normal + groundHits[1].normal)/2, Vector3.up);
        return slopAngle;
    }
    
    
    private void OnTriggerEnter(Collider other)
    {
        // detect gem that increases stamina
        if (other.CompareTag("StaminaPowerUp")) // tag name might be changed
        {
            // increase fixed amount of stamina
            maxFlightStamina += staminaPerGem;
            //Destroy(other.gameObject);
            
            //todo maybe some effect happens
            SoundManager.current.PlaySFX("Collect Gem");
            collectGemParticles.Play();
            if (usingCape)
                cape.CapePowerUp(3f);
        }
    }
    

    private void SetAnimation()
    {
        animationVars.grounded = grounded;
        animationVars.sprinting = sprinting;
        animationVars.horizontalSpeed = new Vector3(rb.velocity.x, 0f, rb.velocity.z).magnitude;
        animationVars.verticalSpeed = rb.velocity.y;
        animationVars.horizontalInput = moveDirection.magnitude > 0.05f;
        animationVars.sliding = sliding;
        animationVars.paragliding = !grounded && animationVars.verticalSpeed < 0 && animationVars.horizontalInput &&
                                    animationVars.horizontalSpeed > slideSpeedThreshold + paraglidingStartSpeedOffset && paraglidingActivated;
        //animationVars.holdingMap = holdingMap;
        
        animator.SetBool("grounded", animationVars.grounded);
        animator.SetFloat("verticalSpeed", animationVars.verticalSpeed);
        animator.SetFloat("horizontalSpeed", animationVars.horizontalSpeed);
        animator.SetBool("requestJump", animationVars.requestJump);
        animator.SetBool("sliding", animationVars.sliding);
        animator.SetBool("paragliding", animationVars.paragliding);
        animator.SetBool("horizontalInput", animationVars.horizontalInput);
        animator.SetBool("sprinting", animationVars.sprinting);
        //animator.SetBool("holdingMap", animationVars.holdingMap);
    }

    public AnimationVars getAnimationVars()
    {
        return animationVars;
    }

    private void SetSound()
    {
        if (sprinting && !sfxState.sprintSFXOn)     // start sprint sfx
        {
            SoundManager.current.PlaySFX("Sprint");
            sfxState.sprintSFXOn = true;
        }
        else if (!sprinting && sfxState.sprintSFXOn)    // stop sprint sfx
        {
            SoundManager.current.StopSFX("Sprint");
            sfxState.sprintSFXOn = false;
        }

        if (sliding && !sfxState.slideSFXOn)        // start slide sfx
        {
            SoundManager.current.PlaySFX("Slide");
            sfxState.slideSFXOn = true;
        }
        else if (!sliding && sfxState.slideSFXOn)    // stop slide sfx
        {
            SoundManager.current.StopSFX("Slide");
            sfxState.slideSFXOn = false;
        }

        if (grounded && !sliding && !sprinting && horizontalVelocity.magnitude > 0.8f)
        {
            if (!sfxState.jogSFXOn)     // start jog sfx
            {
                SoundManager.current.PlaySFX("Jog");
                sfxState.jogSFXOn = true;
            }
        }
        else
        {
            if (sfxState.jogSFXOn)      // stop jog sfx
            {
                SoundManager.current.StopSFX("Jog");
                sfxState.jogSFXOn = false;
            }
        }

        // start and stop sfx in the air
        if (animationVars.paragliding && !sfxState.paraglidingSFXOn)
        {
            SoundManager.current.PlaySFX("Paragliding");
            sfxState.paraglidingSFXOn = true;
        }
        else if (!animationVars.paragliding && sfxState.paraglidingSFXOn)
        {
            SoundManager.current.FadeOutSFX("Paragliding", 1);
            sfxState.paraglidingSFXOn = false;
        }
    }

    public float GetFlightStamina() {
        return flightStamina;
    }
    
    /*
    public bool GetHoldingMap() {
        return holdingMap;
    }
    */

    public bool GetGrounded() {
        return grounded;
    }

    public bool GetSliding() {
        return sliding;
    }

    public Vector3 GetVelocity() {
        return rb.velocity;
    }


    // Could fly after activate
    public void ActivateFlight()  
    {
        flightActivated = true;
    }

    // Could sprint after activate
    public void ActivateSprint()
    {
        sprintActivated = true;
    }
    
    public void ActivateParagliding()
    {
        paraglidingActivated = true;
    }

    public IEnumerator ActivatePlayer()
    {
        yield return new WaitForFixedUpdate();
        rb.isKinematic = false;
        maxAccessibleStamina += GetGemCount() * staminaPerGem;
    }


    private int GetGemCount()
    {
        Dictionary<Gem, GemGenerator.GemData> gemData = gemGenerator.gemData;
        int count = 0;
        foreach (var gem in gemData.Values)
        {
            if (!gem.isDestination) count++;
        }

        return count;
    }
    
    [System.Serializable] 
    public struct AnimationVars
    {
        public bool grounded;
        public float verticalSpeed;
        public float horizontalSpeed;
        public bool horizontalInput; 
        public bool requestJump;
        public bool sliding;
        public bool paragliding;
        public bool sprinting;
        //public bool holdingMap;
    }
    
    [System.Serializable] 
    public struct SoundFXState
    {
        public bool slideSFXOn;
        public bool sprintSFXOn;
        public bool jogSFXOn;
        public bool paraglidingSFXOn;
    }
}
