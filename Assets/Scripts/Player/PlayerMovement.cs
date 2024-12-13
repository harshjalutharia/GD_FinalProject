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
    
    [Tooltip("The impulse pushing character to start boost")]
    public float boostImpulse;
    
    [Tooltip("The force pushing character to boost")]
    public float boostForce;
    
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
    public float walkSpeed;
    
    [Tooltip("Threshold for starting sliding")]
    public float slideSpeedThreshold;

    [Tooltip("If player's dropping speed on ground larger than this, start sliding")]
    public float slidingDropThreshold;
    
    [Tooltip("Controls the max sprint speed on a level surface")]
    public float maxSprintSpeed;
    
    [Tooltip("Controls the max boosting speed on a level surface")]
    public float maxBoostingSpeed;

    [Tooltip("Stop sliding if current speed less than slideStopSpeed.")]
    public float slideStopSpeed;
    
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
    
    [SerializeField, Tooltip("READ ONLY. Limit the horizontal max boosting speed.")] 
    private float boostingDrag;
    
    
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

    [Tooltip("Stamina consumed per second of boosting")]
    public float boostingStaminaDecreaseSpeed;
    
    [Tooltip("The minimum Stamina for starting boosting")]
    public float boostingStartMinStamina;

    [Tooltip("Could not start sprinting if stamina low that this value")] 
    public float sprintMinStamina;
    
    [Tooltip("Stamina regained per second while on the ground and not sprinting")]
    public float flightStaminaRefillSpeed;

    [Tooltip("when refill boost activated, how many times the recovery speed is provided")] 
    public float refillBoost;
    
    [Tooltip("if the refill speed up is activated")] 
    public bool staminaBoosting;

    [Tooltip("Provides refill boost when the current region's gem collection progress reaches a certain percentage.")]
    public float refillBoostTriggerPercentage;

    
    [Header("Input Reference Binds")]
    [SerializeField, Tooltip("The action input of player jumping.")]
    private InputActionReference jumpActionReference;
    
    [SerializeField, Tooltip("The action input of player flying.")]
    private InputActionReference flyActionReference;
    
    [SerializeField, Tooltip("The action input of player sprinting.")]
    private InputActionReference sprintActionReference;
    
    [SerializeField, Tooltip("The action input of player toggle to boost mode.")]
    private InputActionReference boostSwitchActionReference;
    
    [SerializeField, Tooltip("The action input of player boosting.")]
    private InputActionReference boostActionReference;
    
    private PlayerControls controls; 

    private InputAction directionInput;
    
    private float horizontalInput;
    
    private float verticalInput;
    
    private Vector3 moveDirection;

    
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

    [SerializeField] 
    private bool accelerating;

    [SerializeField] 
    private bool boosting;
    
    [SerializeField] 
    private bool requestFlight;
    
    [SerializeField] 
    private Vector3 horizontalVelocity;

    [SerializeField] 
    public int largeGemCollected = 0;
    
    [SerializeField] 
    private bool flightActivated;
    
    [SerializeField] 
    private bool sprintActivated;

    [SerializeField] 
    private bool paraglidingActivated;

    [SerializeField] 
    private bool boostingActivated;
    
    [SerializeField] 
    private bool accelerationActivated; // acceleration provided by arch

    [SerializeField] 
    private bool risingUpActivated;


    [SerializeField, Tooltip("Private state of SFX")]
    private SoundFXState sfxState;

    private Rigidbody rb;

    
    [Header("Animation")]
    [Tooltip("Animation Controller")]
    public Animator animator;

    [SerializeField, Tooltip("READ ONLY. Variables bind for animation control")]
    private AnimationVars animationVars;

    
    [Header("Accelerator Settings")] 
    [Tooltip("Force that propels player to move")]
    public float acceleratorForce;
    
    [Tooltip("how long the force is applied.")]
    public float acceleratorDuration;

    [Tooltip("The trail Object")] 
    public GameObject acceleratorTrail;

    private Vector3 acceleratorOriginalDirection; // record the initial horizontal direction 

    private Coroutine currentAccelerationCoroutine = null;


    [Header("Uprising Acceleration Settings")] 
    [SerializeField, Tooltip("uprising ready")]
    public bool uprisingReady;
    
    [Tooltip("speed of rising")]
    public float uprisingSpeed;
    
    [Tooltip("the height should be reached")]
    public float uprisingHeight;
    
    

    
    [Header("Others")] 
    [Tooltip("Used to determine the player direction")]
    public Transform orientation;

    [Tooltip("player turn speed")]
    public float rotationSpeed;
    
    [Tooltip("particle system of collecting gem")]
    public ParticleSystem collectGemParticles;

    [Tooltip("if activate cape")] 
    public bool usingCape = false;
    
    [Tooltip("game object of the cape")] 
    public SimulateCapeController cape;
    
    [Tooltip("The boosting trail TrailRenderer componet")] 
    public TrailRenderer boostingTrail;

    public event Action OnLanding;
    
    private Transform playerObj;
    
    
    [Header("Debug UI Element")] 
    public TextMeshProUGUI debugText1;
    public TextMeshProUGUI debugText2;

    
    [Header("Tutorial Variables")]
    public TutorialIconManager iconManager;
    private bool hasMoved = false;
    private bool hasJumped = false;
    // private bool hasSprinted = false;
    private bool hasBoosted = false;
    public bool moveTutorialCompleted = false;
    public bool jumpTutorialCompleted = false;
    public bool boostTutorialCompleted = false;
    [HideInInspector] public bool canMove = false;
    [HideInInspector]public bool canJump = false;
    [HideInInspector]public bool canSprint = true;

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
        uprisingReady = true;
        maxAccessibleStamina = maxFlightStamina;
        flightStamina = maxFlightStamina;
        
        groundDrag = moveForce / walkSpeed;
        groundDragSprint = sprintForce / maxSprintSpeed;
        airDragHorizontal = flightForce / maxFlightSpeed;
        airDragParaglidingHorizontal = flightForce / maxParaglidingSpeed;
        airDragVertical = Physics.gravity.magnitude / paraglidingFallingSpeed;
        boostingDrag = boostForce / maxBoostingSpeed;

        physicMaterial = GetComponent<CapsuleCollider>().material;
        
        acceleratorTrail.SetActive(false);

        // initially do not have special movement
        flightActivated = false;
        paraglidingActivated = false;
        sprintActivated = true;
        boostingActivated = false;
        accelerationActivated = false;
        risingUpActivated = false;
        
        if (iconManager == null)
        {
            iconManager = GetComponentInChildren<TutorialIconManager>();
            if (iconManager == null)
            {
                Debug.LogWarning("TutorialIconManager not assigned in PlayerMovement!");
            }
        }
    }

    private void Update()
    {
        DealInput();  // actually it now deals with different states rather than input
        
        // Refill stamina when grounded
        if (grounded /*&& !sprinting*/ && !boosting)
        {
            if (Voronoi.current != null) {
                Region region = Voronoi.current.playerRegion;
                // if collect enough gems in current region, provide stamina refill boost
                staminaBoosting = (float)region.collectedGems.Count / region.smallGems.Count >
                                  refillBoostTriggerPercentage;
                flightStamina += flightStaminaRefillSpeed * Time.deltaTime * (staminaBoosting ? refillBoost : 1);
            }
            else {
                flightStamina += flightStaminaRefillSpeed * Time.deltaTime;
            }
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
        if(boosting)
        {
            flightStamina -= boostingStaminaDecreaseSpeed * Time.deltaTime;
            flightStamina = Mathf.Clamp(flightStamina, 0, maxFlightStamina);
        }
        
        SetAnimation();

        SetSound();
        
        // debug message
        float overAllSpeed = rb.velocity.magnitude;

        if (debugText1 != null && debugText2 != null)
        {
            debugText1.text = normProject[0] + " " + normProject[1] + "slop: " + GetSlopAngle();
            debugText2.text = "Velocity:" + rb.velocity + " \nHorizontal Speed:" + horizontalVelocity + " Speed:" + overAllSpeed;
        }
        
        // cheat
        if (controls.Debug.ActivateFlight.WasPressedThisFrame())
        {
            ActivateFlight();
            ActivateSprint();
            ActivateParagliding();
            ActivatePBoosting();
            SoundManager.current.PlaySFX("Cheat");
            
            // start acceleration
            if (currentAccelerationCoroutine != null)
            {
                StopCoroutine(currentAccelerationCoroutine);
            }
            currentAccelerationCoroutine = StartCoroutine(ExtraAccelerate());
            
            //StartCoroutine(UprisingAcceleration());
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
        sprintActionReference.action.performed += OnSprintInput;
        sprintActionReference.action.canceled += OffSprintInput;
        sprintActionReference.action.Enable();
        boostSwitchActionReference.action.started += OnBoostingInput;
        boostSwitchActionReference.action.Enable();
        boostActionReference.action.performed += OnBoostingInput;
        boostActionReference.action.canceled += OffBoostingInput;
        boostActionReference.action.Enable();
    }

    private void OnDisable()
    {
        controls.Disable(); 
        jumpActionReference.action.started -= OnJumpInput;
        flyActionReference.action.performed -= OnFlightInput;   
        flyActionReference.action.canceled -= OffFlightInput;  
        sprintActionReference.action.performed -= OnSprintInput;
        sprintActionReference.action.canceled -= OffSprintInput;
        boostSwitchActionReference.action.started -= OnBoostingInput;
        boostActionReference.action.performed -= OnBoostingInput;
        boostActionReference.action.canceled -= OffBoostingInput;
    }

    private void DealInput()
    {
        // ====== update state of sliding
        if (sliding)
        {
            sliding = (horizontalVelocity.magnitude > slideStopSpeed ||
                       rb.velocity.y < -1 * slidingDropThreshold) && grounded /*&& !sprinting*/;
        }
        else
        {
            sliding = (horizontalVelocity.magnitude >
                          slideSpeedThreshold || rb.velocity.y < -1 * slidingDropThreshold) &&
                      grounded /*&& !sprinting*/;
        }


        // ======== 56use new input system to get the input value
        Vector2 movement2D = directionInput.ReadValue<Vector2>();
        if (directionInput.activeControl != null && directionInput.activeControl.device.name != "Keyboard")
        {
            // if using gamepad, determine sprint by the amount of joystick displacement
            if (movement2D.magnitude > 0.9f)    
            {
                OnSprintInput(new InputAction.CallbackContext());
            }
            else
            {
                OffSprintInput(new InputAction.CallbackContext());
            }
            //Debug.Log(movement2D.magnitude + directionInput.activeControl.device.name + " ");
        }

        if (!canMove)
        {
            // If not allowed to move yet, forcibly set input to zero
            movement2D = Vector2.zero;
        }

        horizontalInput = movement2D.x;
        verticalInput = movement2D.y;

        if (!hasMoved && (horizontalInput != 0 || verticalInput != 0))
        {
            hasMoved = true;
            if (SessionManager2.current.ringTutorialCompleted && !moveTutorialCompleted)
            {
                moveTutorialCompleted = true;
                Debug.Log("FINISHED MOVE");
            }
        }

        
        // ======== update the sprinting state
        if (sprintActivated && grounded && canSprint && sprinting && moveDirection.magnitude > 0.02f && !sliding)
        {
            //sprinting = flightStamina > 0;  // end sprinting if stamina less than 0
        }
        else
        {
            sprinting = false;
        }

        
        // ======== update the boosting state
        if (boosting && flightStamina <= 0)
        {
            OffBoostingInput(new InputAction.CallbackContext());
        }
        if (boostActionReference.action.phase == InputActionPhase.Performed)
        {
            OnBoostingInput(new InputAction.CallbackContext());
        }
        
        // ==== update flight state
        if (requestFlight && flightStamina <= 0)
        {
            OffFlightInput(new InputAction.CallbackContext());
        }
    }

    //------------------ Event for the inputs---------------------
    // event of jump input triggered
    private void OnJumpInput(InputAction.CallbackContext ctx)
    {
        if(canJump && readyToJump && grounded && flightStamina > jumpStaminaConsume)
        {
            readyToJump = false;
            Invoke(nameof(ResetJump), jumpCooldown);
            requestJump = true;
            preparingJump = true;

            if (!jumpTutorialCompleted)
            {
                jumpTutorialCompleted = true;
                Debug.Log("JUMP FINISH");
            }
        }
    }

    // event of flight input triggered
    private void OnFlightInput(InputAction.CallbackContext ctx)
    {
        if (flightActivated && !grounded && flightStamina > 0)
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
        if (sprintActivated && grounded && moveDirection.magnitude > 0.02f && !sliding)
        {
            /*if (sprinting)
            {
                sprinting = flightStamina > 0;  // end sprinting if stamina less than 0
            }
            else
            {
                sprinting = flightStamina > sprintMinStamina;  // could only start sprint if stamina larger than certain value
            }*/
            // now sprint dont consume stamina
            sprinting = true;
        }
    }

    private void OffSprintInput(InputAction.CallbackContext ctx)
    {
        sprinting = false;
    }

    private void OnBoostingInput(InputAction.CallbackContext ctx)
    {
        if (boostingActivated && flightStamina > boostingStartMinStamina && moveDirection.magnitude > 0.05f && !boosting)
        {
            boosting = true;
            boostingTrail.time = 1;
            StartCoroutine(BoostOneShot()); // Applying an instantaneous acceleration
            hasBoosted = true;
        }
    }
    
    private void OffBoostingInput(InputAction.CallbackContext ctx)
    {
        if (!boosting)
            return;
        if (!boostTutorialCompleted)
        {
            boostTutorialCompleted = true;
            Debug.Log("BOOST FINISH");
        }
        boosting = false;
        StartCoroutine(FadeoutBoostTrail());
    }
    
    //------------------ End Event for the inputs---------------------
    
    // called in FixedUpdate
    private void MovePlayer()
    {
        // calculate input movement direction
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;
        
        // rotate the player according to the velocity
        if (rb.velocity.magnitude >= 0.05f)
        {
            //playerObj.forward = Vector3.Slerp(playerObj.forward, new Vector3(rb.velocity.x, 0, rb.velocity.z).normalized, Time.fixedDeltaTime * rotationSpeed);
            // rotate the player according to input direction
            playerObj.forward = Vector3.Slerp(playerObj.forward, moveDirection.normalized, Time.deltaTime * rotationSpeed);
        }
        
        
        //set friction if player has no input
        if (moveDirection.magnitude < 0.05f && grounded && GetSlopAngle() <= frictionSlopAngle && !accelerating)
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
        
        // boost movement
        if (boosting)
        {
            Vector3 moveForce = moveDirection.normalized * boostForce - CalculateBoostingResistance();
            rb.AddForce(moveForce, ForceMode.Force);
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
        if (hasMoved) hasJumped = true;
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
        else if (grounded && sprinting)  // sprinting drag
        {
            resistanceMag = Mathf.Clamp(horizontalVelocity.magnitude * groundDragSprint, 0, sprintForce*2);
        }
        else if (!grounded && !animationVars.paragliding)  // free-fall drag
        {
            resistanceMag = Mathf.Clamp(horizontalVelocity.magnitude * airDragHorizontal, 0, flightForce*2);
        }
        else  // !grounded && animationVars.paragliding  // paragliding drag
        {
            resistanceMag = Mathf.Clamp(horizontalVelocity.magnitude * airDragParaglidingHorizontal, 0, flightForce*2);
        }
        Vector3 resistanceForce = resistanceMag * horizontalVelocity.normalized;
        return resistanceForce;
        
    }

    private Vector3 CalculateBoostingResistance()
    {
        float resistanceMag = Mathf.Clamp(rb.velocity.magnitude * boostingDrag, 0, 1.5f * boostForce);
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
            flightStamina += staminaPerGem;
            
            SoundManager.current.PlaySFX("Collect Gem");
            collectGemParticles.Play();
            if (usingCape)
                cape.CapePowerUp(3f);
            return;
        }
        
        // in case of accelerator collider
        if (other.CompareTag("Accelerator"))
        {
            if (accelerationActivated && currentAccelerationCoroutine != null)
            {
                StopCoroutine(currentAccelerationCoroutine);
            }
            currentAccelerationCoroutine = StartCoroutine(ExtraAccelerate());
            return;
        }
        
        // large gem
        if (other.CompareTag("LargeGem"))
        {
            SoundManager.current.PlaySFX("Collect Gem");
            largeGemCollected++;
            if (largeGemCollected >= 3)
            {
                ActivateFlight();
                ActivateParagliding();
                ActivateRisingUp();
                ActivatePBoosting();
                ActivateAcceleration();
            }
            if (largeGemCollected >= 2)
            {
                ActivateParagliding();
                ActivateRisingUp();
                ActivatePBoosting();
                ActivateAcceleration();
            }
            else if (largeGemCollected >= 1)
            {
                ActivatePBoosting();
                ActivateAcceleration();
                StartCoroutine(BoostTutorialSequence());
            }
            return;
        }
        
        // bell tower
        if (other.CompareTag("BellTower"))
        {
            if (risingUpActivated && uprisingReady && Voronoi.current.playerRegion.destinationCollected)
            {
                uprisingReady = false;
                Debug.Log("startUPPpp");
                StartCoroutine(UprisingAcceleration());
            }
            return;
        }
    }

    
    // apply uprising acceleration provide by bell tower
    private IEnumerator UprisingAcceleration()
    {
        Debug.Log("upppppping");
        acceleratorTrail.SetActive(true);
        TrailRenderer trailRenderer = acceleratorTrail.GetComponent<TrailRenderer>();
        trailRenderer.time = 1;
        
        float initialY = transform.position.y;
        rb.isKinematic = true; // forbid rigid body, controls the transform.position directly
        while (transform.position.y - initialY <= uprisingHeight)
        {
            yield return new WaitForFixedUpdate();
            transform.position = new Vector3(transform.position.x,
                transform.position.y + Time.fixedDeltaTime * uprisingSpeed, transform.position.z);
            
        }
        uprisingReady = true;
        rb.isKinematic = false;
        rb.velocity = new Vector3(0, uprisingSpeed, 0);
        StartCoroutine(FadeoutAccelerationTrail());

    }
    
    

    // apply acceleration of arch ruins
    private IEnumerator ExtraAccelerate()
    {
        // set the accelerator direction based on input direction
        acceleratorOriginalDirection = moveDirection.normalized;
        // in case player is not moving
        while (acceleratorOriginalDirection.magnitude < 0.05f)
        {
            yield return new WaitForFixedUpdate();
            // if player has no input but is moving, using moving direction instead or wait until got a input
            if (horizontalVelocity.magnitude > 0.05f)
            {
                acceleratorOriginalDirection = horizontalVelocity.normalized;
            }
            else
            {
                acceleratorOriginalDirection = moveDirection.normalized;
            }
        }
        
        acceleratorTrail.SetActive(true);
        accelerating = true;

        TrailRenderer trailRenderer = acceleratorTrail.GetComponent<TrailRenderer>();
        trailRenderer.time = 1;
        
        float startTime = Time.time;
        while (Time.time - startTime <= acceleratorDuration)
        {
            yield return new WaitForFixedUpdate();
            
            // Apply acceleration parallel to the slope
            // Ground Normal Direction
            Vector3 groundNormal = (0.5f * (groundHits[0].normal + groundHits[0].normal)).normalized;
            // the normal of the plane formed by acceleration direction and world.up
            Vector3 accDirNormal = Vector3.Cross(Vector3.up, acceleratorOriginalDirection);
            // project ground Nomral to the plane
            Vector3 projectedGroundNormal = Vector3.ProjectOnPlane(groundNormal, accDirNormal).normalized;
            // get the right accelerate direction 
            float angle = Vector3.SignedAngle(Vector3.up, projectedGroundNormal, accDirNormal); 
            Vector3 direction = Quaternion.AngleAxis(angle, accDirNormal) * acceleratorOriginalDirection;

            float t = (Time.time - startTime) / acceleratorDuration;
            //Debug.DrawRay(transform.position, direction);
            float forceMagnitude = Mathf.Lerp(acceleratorForce, 0, t); // gradually reduce force intensity
            rb.AddForce(forceMagnitude * direction, ForceMode.Force);

            //trailRenderer.time = Mathf.Lerp(1, 0, t);
        }
        accelerating = false;
        //acceleratorTrail.SetActive(false);
        currentAccelerationCoroutine = null;
        StartCoroutine(FadeoutAccelerationTrail());
    }


    // gives an impulse at the beginning of the boost
    private IEnumerator BoostOneShot()
    {
        yield return new WaitForFixedUpdate();
        rb.AddForce(boostImpulse * moveDirection.normalized, ForceMode.Impulse);
    }
    
    private IEnumerator FadeoutBoostTrail()
    {
        float duration = 1;
        float startTime = Time.time;
        while (Time.time - startTime <= duration)
        {
            yield return new WaitForFixedUpdate();
            if (!boosting)
            {
                float t = (Time.time - startTime) / duration;
                boostingTrail.time = Mathf.Clamp(Mathf.Lerp(1, 0, t), 0, 1);
            }
            else
            {
                yield break;
            }
        }
    }
    
    private IEnumerator FadeoutAccelerationTrail()
    {
        TrailRenderer trail = acceleratorTrail.GetComponent<TrailRenderer>();
        float duration = 1;
        float startTime = Time.time;
        while (Time.time - startTime <= duration)
        {
            yield return new WaitForFixedUpdate();
            float t = (Time.time - startTime) / duration;
            trail.time = Mathf.Clamp(Mathf.Lerp(1, 0, t), 0, 1);
        }
        acceleratorTrail.SetActive(false);
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

    public float GetMaxFlightStamina()
    {
        return maxFlightStamina;
    }
    
    public float GetMaxAccessibleFlightStamina() {
        return maxAccessibleStamina;
    }

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

    public void ActivatePBoosting()
    {
        boostingActivated = true;
    }
    
    public void ActivateAcceleration()
    {
        accelerationActivated = true;
    }

    public void ActivateRisingUp()
    {
        risingUpActivated = true;
    }

    public IEnumerator ActivatePlayer()
    {
        yield return new WaitForFixedUpdate();
        rb.isKinematic = false;
        maxAccessibleStamina += GemGenerator2.current.smallGems.Count * staminaPerGem;
        // StartCoroutine(Tutorial());
    }

    // --- Tutorials ---
    public IEnumerator MoveTutorialSequence()
    {
        yield return new WaitForSeconds(1f);
        Debug.Log("Starting MOVE");
        yield return StartCoroutine(TutorialIconManager.current.ShowIconUntilCondition(
            TutorialIconManager.current.ShowMoveIcon,
            () => moveTutorialCompleted
        ));
        Debug.Log("Starting JUMP");
        yield return StartCoroutine(TutorialIconManager.current.ShowIconUntilCondition(
            TutorialIconManager.current.ShowJumpIcon,
            () => jumpTutorialCompleted
        ));

    }

    public IEnumerator BoostTutorialSequence()
    {
        Debug.Log("Starting BOOST");
        yield return StartCoroutine(TutorialIconManager.current.ShowIconUntilCondition(
            TutorialIconManager.current.ShowBoostIcon,
            () => boostTutorialCompleted
        ));

        Debug.Log("All tutorials done!");
        // Do anything else needed after tutorials
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
