using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;


// public class PlayerMovement : MonoBehaviour
// {
//     [Header("Movement")]
//     [Tooltip("The force pushing character to walk, doesn't represent actual speed")]
//     public float moveSpeed;
//     
//     [Tooltip("The force pushing character to sprint, doesn't represent actual speed")]
//     public float sprintSpeed;
//
//     [SerializeField, Tooltip("READ ONLY. Controls the max walking speed on a level surface. Vmax=moveSpeed/groundDrag")]
//     private float slideSpeedThreshold;
//
//     [Tooltip("Start slide if current speed larger than slideSpeedThreshold + slideStartSpeedOffset.")]
//     public float slideStartSpeedOffset;
//     
//     [Tooltip("Start paragliding if current speed larger than slideSpeedThreshold + paraglidingStartSpeedOffset.")]
//     public float paraglidingStartSpeedOffset;
//     
//     [Tooltip("Limit the max walking speed, Vmax=moveSpeed/groundDrag")]
//     public float groundDrag;
//     
//     [Tooltip("Limit the max sprint speed, Vmax=moveSpeed/groundDragSprint")]
//     public float groundDragSprint;
//
//     [Tooltip("When the user has no direction input, applies the friction")]
//     public float friction;
//     
//     private PhysicMaterial physicMaterial;
//
//     [Tooltip("Limit the horizontal max fly speed. Vmax_fly=moveSpeed/airDragHorizontal")] 
//     public float airDragHorizontal;
//     
//     [Tooltip("Only limit the max downward velocity speed. Vmax_drop=9.81/airDragVertical")] 
//     public float airDragVertical;
//
//     [Tooltip("Resistance Force while sliding")] 
//     public float slideResistance;
//     
//     [Tooltip("Instantaneous force exerted during a jump")]
//     public float jumpForce;
//     
//     [Tooltip("Used to avoid double jump")]
//     public float jumpCooldown;
//     
//     [Tooltip("Used to reduces movement speed in the air")]
//     public float airMultiplier;
//     
//     [Tooltip("Continuous force applied during flight")]
//     public float flightForce;
//     
//     [Tooltip("Used to determine the player direction")]
//     public Transform orientation;
//     
//     
//     [Header("Stamina Settings")]
//     [Tooltip("Maximum stamina for flight and sprint")]
//     public float maxFlightStamina;
//     
//     [Tooltip("Stamina consumed by jumping")]
//     public float jumpStaminaConsume;
//     
//     [SerializeField, Tooltip("Current stamina")] 
//     private float flightStamina;
//     
//     [Tooltip("Stamina consumed per second of flight")]
//     public float flightStaminaDecreaseSpeed;
//     
//     [Tooltip("Stamina consumed per second of springting")]
//     public float sprintStaminaDecreaseSpeed;
//
//     [Tooltip("Could not start sprinting if stamina low that this value")] 
//     public float sprintMinStamina;
//     
//     [Tooltip("Stamina regained per second while on the ground and not sprinting")]
//     public float flightStaminaRefillSpeed;
//
//     [Header("Input")]
//     private PlayerControls controls; 
//
//     [SerializeField] private InputAction directionInput;
//
//     [SerializeField] private InputAction jumpInput;
//     
//     [SerializeField] private InputAction sprintInput;
//
//     [SerializeField] private InputAction switchSprintInput;
//     
//     
//     //public KeyCode jumpKey = KeyCode.Space;
//
//     //public KeyCode sprintKey = KeyCode.LeftShift;
//
//     //[Tooltip("Activate the ability to fly and sprint")]
//     //public KeyCode cheatKey = KeyCode.C;
//     
//     private float horizontalInput;
//     
//     private float verticalInput;
//
//     [Header("Ground Check")]
//     
//     [Tooltip("Positions to perform ground detect")]
//     public Transform[] groundDetections = new Transform[2];
//     private RaycastHit[] groundHits = new RaycastHit[2];
//     private Vector3[] normProject = new Vector3[2];
//
//     [Tooltip("Distance of ground check")]
//     public float groundCheckDistance;
//
//     [Tooltip("Keep player on the ground if the angular change of slope is less than this value")]
//     public float slopAngularChangeTolerance;
//     
//     [Tooltip("Set to everything is OK")]
//     public LayerMask groundMask;
//     
//     [Header("Private States")]
//     [SerializeField] 
//     private bool grounded;
//
//     [SerializeField]
//     private bool grabGround;
//     
//     [SerializeField]
//     private bool readyToJump;
//     
//     [SerializeField] 
//     private bool requestJump;
//
//     [SerializeField] 
//     private bool preparingJump;
//     
//     [SerializeField] 
//     private bool sprinting;
//
//     [SerializeField] 
//     private bool keepSprint;
//     
//     [SerializeField] 
//     private bool requestFlight;
//     
//     [SerializeField] 
//     private bool flightActivated;
//     
//     [SerializeField] 
//     private bool sprintActivated;
//     
//     
//     
//     private Vector3 moveDirection;
//
//     private Rigidbody rb;
//
//     [Header("Animation")]
//     [Tooltip("Animation Controller")]
//     public Animator animator;
//
//     [SerializeField, Tooltip("READ ONLY. Variables bind for animation control")]
//     private AnimationVars animationVars;
//
//     [Header("Audio")] 
//     private AudioSource audioSource;
//
//     [Tooltip("Sound of cheating")]
//     public AudioClip switchSound;
//
//     [Header("Debug UI Element")] 
//     public TextMeshProUGUI debugText1;
//     public TextMeshProUGUI debugText2;
//
//     private void Awake()
//     {
//         controls = InputManager.Instance.controls;
//         directionInput = controls.Player.Move;
//         jumpInput = controls.Player.JumpFly;
//         sprintInput = controls.Player.Sprint;
//         switchSprintInput = controls.Player.SwitchSprint;
//     }
//     
//     private void Start()
//     {
//         rb = GetComponent<Rigidbody>();
//         rb.freezeRotation = true;
//
//         readyToJump = true;
//         flightStamina = maxFlightStamina;
//
//         slideSpeedThreshold = moveSpeed / groundDrag;
//
//         physicMaterial = GetComponent<CapsuleCollider>().material;
//
//         // initially do not have special movement
//         flightActivated = false;   
//         sprintActivated = true;
//
//         audioSource = GetComponent<AudioSource>();
//         
//     }
//
//     private void Update()
//     {
//
//         // Collider[] colliders1 = Physics.OverlapSphere(groundDetections[0].position, 0.05f, groundMask);
//         // Collider[] colliders2 = Physics.OverlapSphere(groundDetections[1].position, 0.05f, groundMask);
//         // grounded = false;
//         // foreach (var collider in colliders1)
//         // {
//         //     if (collider.gameObject.layer != LayerMask.NameToLayer("Player") && collider.gameObject.layer != LayerMask.NameToLayer("FakePlayer"))
//         //     {
//         //         grounded = true;
//         //         break;
//         //     }
//         // }
//         // foreach (var collider in colliders2)
//         // {
//         //     if (grounded) break;
//         //     if (collider.gameObject.layer != LayerMask.NameToLayer("Player") && collider.gameObject.layer != LayerMask.NameToLayer("FakePlayer"))
//         //     {
//         //         grounded = true;
//         //         break;
//         //     }
//         // }
//         // grounded = Physics.Raycast(orientation.position, Vector3.down, 0.2f, GroundMask);
//         
//         DealInput();
//         
//         // refill flight stamina when grounded
//         if (grounded && !sprinting)
//         {
//             flightStamina += flightStaminaRefillSpeed * Time.deltaTime;
//             flightStamina = Mathf.Clamp(flightStamina, 0, maxFlightStamina);
//         }
//         else if(sprinting)
//         {
//             flightStamina -= sprintStaminaDecreaseSpeed * Time.deltaTime;
//             flightStamina = Mathf.Clamp(flightStamina, 0, maxFlightStamina);
//         }
//         else if (requestFlight)
//         {
//             flightStamina -= flightStaminaDecreaseSpeed * Time.deltaTime;
//             flightStamina = Mathf.Clamp(flightStamina, 0, maxFlightStamina);
//         }
//         
//         
//
//         SetAnimation();
//         
//         
//         // debug message
//         float horizontalSpeed = new Vector3(rb.velocity.x, 0, rb.velocity.z).magnitude;
//         float overAllSpeed = rb.velocity.magnitude;
//         
//         debugText1.text = normProject[0] + " " + normProject[1] + "angle:" + Vector3.Angle(normProject[0], normProject[1]);
//         debugText2.text = "Velocity:" + rb.velocity + " \nHorizontal Speed:" + horizontalSpeed + " Speed:" + overAllSpeed;
//         
//         // Debug.Log(normProject[0] + " " + normProject[1]);
//         Debug.DrawRay(groundHits[0].point, groundHits[0].normal, Color.blue, 0, false);
//         Debug.DrawRay(groundHits[1].point, groundHits[1].normal, Color.green, 0, false);
//         // cheat
//         if (controls.Debug.ActivateFlight.WasPressedThisFrame())
//         {
//             ActivateFlight();
//             ActivateSprint();
//             audioSource.PlayOneShot(switchSound);
//         }
//     }
//
//     private void FixedUpdate()
//     {
//         // apply two ground normal check
//         for (int i = 0; i < 2; i++)
//         {
//             Physics.Raycast(groundDetections[i].position, Vector3.down, out groundHits[i], Mathf.Infinity,
//                 groundMask);
//         }
//         // project the norm of the ground
//         normProject[0] =
//             animator.transform.InverseTransformDirection(Vector3.ProjectOnPlane(groundHits[0].normal,
//                 animator.transform.right));
//         normProject[1] =
//             animator.transform.InverseTransformDirection(Vector3.ProjectOnPlane(groundHits[1].normal,
//                 animator.transform.right));
//         
//         // determine the ground grab
//         grabGround = normProject[1].z >= normProject[0].z && //normProject[1].z >= 0 && 
//                      Vector3.Angle(normProject[0], normProject[1]) < slopAngularChangeTolerance && grounded &&
//                      !preparingJump && !requestFlight;
//         
//         // if (!grabGround)
//         //     Debug.Log((normProject[1].z >= normProject[0].z) + " " + Vector3.Angle(normProject[0], normProject[1]));
//         
//         Collider[] colliders = Physics.OverlapSphere(orientation.position, 0.1f, groundMask);
//         
//         grounded = colliders.Length > 0; 
//         
//         
//         MovePlayer();
//     }
//     
//     private void OnEnable()
//     {
//         controls.Enable(); 
//     }
//
//     private void OnDisable()
//     {
//         controls.Disable(); 
//     }
//
//     private void DealInput()
//     {
//         // use new input system to get the input value
//         Vector2 movement2D = directionInput.ReadValue<Vector2>();
//         horizontalInput = movement2D.x;
//         verticalInput = movement2D.y;
//         
//
//         // jump input
//         if(jumpInput.WasPressedThisFrame() && readyToJump && grounded && flightStamina > jumpStaminaConsume)
//         {
//             readyToJump = false;
//             Invoke(nameof(ResetJump), jumpCooldown);
//             requestJump = true;
//             preparingJump = true;
//         }
//         
//         // fly input
//         if (flightActivated && !grounded && jumpInput.IsPressed() && flightStamina > 0)
//         {
//             requestFlight = true;
//         }
//         else
//         {
//             requestFlight = false;
//         }
//         
//         // sprint input
//         if (sprintActivated && grounded && (sprintInput.IsPressed() || switchSprintInput.WasPressedThisFrame() || keepSprint) && moveDirection.magnitude > 0.02f)
//         {
//             if (switchSprintInput.WasPressedThisFrame())
//             {
//                 Debug.Log("switch sprint");
//             }
//             if (sprinting)
//             {
//                 sprinting = flightStamina > 0;  // end sprinting if stamina less than 0
//             }
//             else
//             {
//                 sprinting = flightStamina > sprintMinStamina;  // could only start sprint if stamina larger than certain value
//                 if (sprinting && switchSprintInput.WasPressedThisFrame())  // if sprint started by left joystick
//                 {
//                     keepSprint = true;
//                 }
//             }
//         }
//         else
//         {
//             sprinting = false;
//         }
//
//         if (!sprinting) 
//             keepSprint = false;
//     }
//
//     
//     // called in FixedUpdate
//     private void MovePlayer()
//     {
//         // calculate movement direction
//         moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;
//         
//         //set friction if player has no input
//         if (moveDirection.magnitude < 0.05f && grounded)
//         {
//             physicMaterial.dynamicFriction = friction;
//             physicMaterial.staticFriction = friction;
//         }
//         else
//         {
//             physicMaterial.dynamicFriction = 0;
//             physicMaterial.staticFriction = 0;
//         }
//         
//         // begin horizontal movement
//         // on ground
//         if (grounded)
//         {
//             Vector3 moveForce = moveDirection.normalized * (sprinting ? sprintSpeed : moveSpeed) - CalculateResistance();
//             rb.AddForce(moveForce, ForceMode.Force);
//         }
//         // in the air
//         else if (!grounded)
//         {
//             Vector3 moveForce = moveDirection.normalized * (airMultiplier * moveSpeed) - CalculateResistance();
//             rb.AddForce(moveForce, ForceMode.Force);
//             
//             // limit descent speed if player has input
//             if (flightActivated && rb.velocity.y < 0 && moveDirection.magnitude > 0.05f)
//             {
//                 rb.AddForce(airDragVertical * rb.velocity.y * Vector3.down, ForceMode.Force);
//             }
//         }
//         // end horizontal movement
//         
//         // begin vertical movement
//         if (grabGround)
//         {
//             //rb.AddForce(animator.transform.TransformDirection(-0.5f * (groundHits[0].normal + groundHits[0].normal)) * 50, ForceMode.Force);
//             rb.AddForce(50 * -0.5f * (groundHits[0].normal + groundHits[0].normal), ForceMode.Force);
//         }
//         
//         rb.useGravity = true;
//         if (requestJump) // jump control
//         {
//             requestJump = false;
//             Invoke(nameof(Jump), 0.15f);
//             animationVars.requestJump = true;
//             // Invoke(nameof(ResetAnimatorRequestJump), 0.2f);
//         }
//         else if (requestFlight)     // flight control
//         {
//             FlyUp();
//         }
//         
//     }
//
//     private void Jump()
//     {
//         animationVars.requestJump = false;
//         
//         flightStamina -= jumpStaminaConsume;
//         flightStamina = Mathf.Clamp(flightStamina, 0, maxFlightStamina);
//         // reset y velocity
//         rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
//         rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
//     }
//     
//     private void FlyUp()
//     {
//         rb.useGravity = false;
//         rb.AddForce(Vector3.up * flightForce, ForceMode.Acceleration);
//     }
//     
//     
//     private void ResetJump()
//     {
//         readyToJump = true;
//         preparingJump = false;
//     }
//     
//     
//     private Vector3 CalculateResistance()
//     {
//         Vector3 horizontalVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
//         float resistanceMag = 0;
//         if (grounded && !sprinting)  // walking drag
//         {
//             // the resistance increases linearly with horizontal velocity. But has a max value
//             resistanceMag = Mathf.Clamp(horizontalVelocity.magnitude * groundDrag, 0, moveSpeed + slideResistance);
//         }
//         else if (grounded && sprinting)
//         {
//             resistanceMag = Mathf.Clamp(horizontalVelocity.magnitude * groundDragSprint, 0, sprintSpeed*2);
//         }
//         else
//         {
//             resistanceMag = horizontalVelocity.magnitude * airDragHorizontal;
//         }
//         Vector3 resistanceForce = resistanceMag * horizontalVelocity.normalized;
//         return resistanceForce;
//         
//     }
//     
//     private void OnTriggerEnter(Collider other)
//     {
//         if (other.CompareTag("StaminaPowerUp")) // tag name might be changed
//         {
//             //todo get the stamina increase amount from the collider object
//             var staminaIncrease = 3;  // change later
//
//             maxFlightStamina += staminaIncrease;
//             
//             //todo maybe some effect happens
//         }
//     }
//     
//     
//
//     // private void ResetAnimatorRequestJump()
//     // {
//     //     animationVars.requestJump = false;
//     // }
//
//     private void SetAnimation()
//     {
//         animationVars.grounded = grounded;
//         animationVars.sprinting = sprinting;
//         animationVars.horizontalSpeed = new Vector3(rb.velocity.x, 0f, rb.velocity.z).magnitude;
//         animationVars.verticalSpeed = rb.velocity.y;
//         animationVars.horizontalInput = moveDirection.magnitude > 0.05f;
//         if (animationVars.sliding)
//         {
//             animationVars.sliding = animationVars.horizontalSpeed > slideSpeedThreshold && grounded && !sprinting;
//         }
//         else
//         {
//             animationVars.sliding = animationVars.horizontalSpeed > slideSpeedThreshold + slideStartSpeedOffset &&
//                                     grounded && !sprinting;
//         }
//
//         animationVars.paragliding = !grounded && animationVars.verticalSpeed < 0 && animationVars.horizontalInput &&
//                                     animationVars.horizontalSpeed > slideSpeedThreshold + paraglidingStartSpeedOffset;
//         
//         
//         animator.SetBool("grounded", animationVars.grounded);
//         animator.SetFloat("verticalSpeed", animationVars.verticalSpeed);
//         animator.SetFloat("horizontalSpeed", animationVars.horizontalSpeed);
//         animator.SetBool("requestJump", animationVars.requestJump);
//         animator.SetBool("sliding", animationVars.sliding);
//         animator.SetBool("paragliding", animationVars.paragliding);
//         animator.SetBool("horizontalInput", animationVars.horizontalInput);
//         animator.SetBool("sprinting", animationVars.sprinting);
//     }
//
//     public AnimationVars getAnimationVars()
//     {
//         return animationVars;
//     }
//
//     public float GetFlightStamina() {
//         return flightStamina;
//     }
//
//
//     // Could fly after activate
//     public void ActivateFlight()  
//     {
//         flightActivated = true;
//     }
//
//     // Could sprint after activate
//     public void ActivateSprint()
//     {
//         sprintActivated = true;
//     }
//
//     public IEnumerator ActivatePlayer()
//     {
//         yield return new WaitForFixedUpdate();
//         rb.isKinematic = false;
//     }
//     
//     
//     
//     [System.Serializable] 
//     public struct AnimationVars
//     {
//         public bool grounded;
//         public float verticalSpeed;
//         public float horizontalSpeed;
//         public bool horizontalInput; 
//         public bool requestJump;
//         public bool sliding;
//         public bool paragliding;
//         public bool sprinting;
//     }
// }














public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("The force pushing character to walk, doesn't represent actual speed")]
    public float moveSpeed;
    
    [Tooltip("The force pushing character to sprint, doesn't represent actual speed")]
    public float sprintSpeed;

    [SerializeField, Tooltip("READ ONLY. Controls the max walking speed on a level surface. Vmax=moveSpeed/groundDrag")]
    private float slideSpeedThreshold;

    [Tooltip("Start slide if current speed larger than slideSpeedThreshold + slideStartSpeedOffset.")]
    public float slideStartSpeedOffset;
    
    [Tooltip("Start paragliding if current speed larger than slideSpeedThreshold + paraglidingStartSpeedOffset.")]
    public float paraglidingStartSpeedOffset;
    
    [Tooltip("Limit the max walking speed, Vmax=moveSpeed/groundDrag")]
    public float groundDrag;
    
    [Tooltip("Limit the max sprint speed, Vmax=moveSpeed/groundDragSprint")]
    public float groundDragSprint;

    [Tooltip("When the user has no direction input, applies the friction")]
    public float friction;
    
    private PhysicMaterial physicMaterial;

    [Tooltip("Limit the horizontal max fly speed. Vmax_fly=moveSpeed/airDragHorizontal")] 
    public float airDragHorizontal;
    
    [Tooltip("Only limit the max downward velocity speed. Vmax_drop=9.81/airDragVertical")] 
    public float airDragVertical;

    [Tooltip("Resistance Force while sliding")] 
    public float slideResistance;
    
    [Tooltip("Instantaneous force exerted during a jump")]
    public float jumpForce;
    
    [Tooltip("Used to avoid double jump")]
    public float jumpCooldown;
    
    [Tooltip("Used to reduces movement speed in the air")]
    public float airMultiplier;
    
    [Tooltip("Continuous force applied during flight")]
    public float flightForce;
    
    [Tooltip("Used to determine the player direction")]
    public Transform orientation;
    
    
    [Header("Stamina Settings")]
    [Tooltip("Maximum stamina for flight and sprint")]
    public float maxFlightStamina;
    
    [Tooltip("Stamina consumed by jumping")]
    public float jumpStaminaConsume;
    
    [SerializeField, Tooltip("Current stamina")] 
    private float flightStamina;
    
    [Tooltip("Stamina consumed per second of flight")]
    public float flightStaminaDecreaseSpeed;
    
    [Tooltip("Stamina consumed per second of springting")]
    public float sprintStaminaDecreaseSpeed;

    [Tooltip("Could not start sprinting if stamina low that this value")] 
    public float sprintMinStamina;
    
    [Tooltip("Stamina regained per second while on the ground and not sprinting")]
    public float flightStaminaRefillSpeed;

    [Header("Input")]
    private PlayerControls controls; 

    [SerializeField] private InputAction directionInput;

    [SerializeField] private InputAction jumpInput;
    
    [SerializeField] private InputAction sprintInput;

    [SerializeField] private InputAction switchSprintInput;
    
    
    //public KeyCode jumpKey = KeyCode.Space;

    //public KeyCode sprintKey = KeyCode.LeftShift;

    //[Tooltip("Activate the ability to fly and sprint")]
    //public KeyCode cheatKey = KeyCode.C;
    
    private float horizontalInput;
    
    private float verticalInput;

    [Header("Ground Check")]
    
    [Tooltip("Positions to perform ground detect")]
    public Transform[] groundDetections = new Transform[2];
    private RaycastHit[] groundHits = new RaycastHit[2];
    private Vector3[] normProject = new Vector3[2];

    [Tooltip("Distance of ground check")]
    public float groundCheckDistance;

    [Tooltip("Keep player on the ground if the angular change of slope is less than this value")]
    public float slopAngularChangeTolerance;
    
    [Tooltip("Set to everything is OK")]
    public LayerMask groundMask;
    
    [Header("Private States")]
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
    private bool keepSprint;
    
    [SerializeField] 
    private bool requestFlight;
    
    [SerializeField] 
    private bool flightActivated;
    
    [SerializeField] 
    private bool sprintActivated;

    [SerializeField] 
    private bool paraglidingActivated;
    
    
    
    private Vector3 moveDirection;

    private Rigidbody rb;

    [Header("Animation")]
    [Tooltip("Animation Controller")]
    public Animator animator;

    [SerializeField, Tooltip("READ ONLY. Variables bind for animation control")]
    private AnimationVars animationVars;

    [Header("Audio")] 
    private AudioSource audioSource;

    [Tooltip("Sound of cheating")]
    public AudioClip switchSound;

    [Header("Debug UI Element")] 
    public TextMeshProUGUI debugText1;
    public TextMeshProUGUI debugText2;

    private void Awake()
    {
        controls = InputManager.Instance.controls;
        directionInput = controls.Player.Move;
        jumpInput = controls.Player.JumpFly;
        sprintInput = controls.Player.Sprint;
        switchSprintInput = controls.Player.SwitchSprint;
    }
    
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        readyToJump = true;
        flightStamina = maxFlightStamina;

        slideSpeedThreshold = moveSpeed / groundDrag;

        physicMaterial = GetComponent<CapsuleCollider>().material;

        // initially do not have special movement
        flightActivated = false;
        paraglidingActivated = false;
        sprintActivated = true;

        audioSource = GetComponent<AudioSource>();
        
    }

    private void Update()
    {
        DealInput();
        
        // refill flight stamina when grounded
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
        
        
        // debug message
        float horizontalSpeed = new Vector3(rb.velocity.x, 0, rb.velocity.z).magnitude;
        float overAllSpeed = rb.velocity.magnitude;
        
        debugText1.text = normProject[0] + " " + normProject[1] + "angle:" + Vector3.Angle(normProject[0], normProject[1]);
        debugText2.text = "Velocity:" + rb.velocity + " \nHorizontal Speed:" + horizontalSpeed + " Speed:" + overAllSpeed;
        
        // Debug.Log(normProject[0] + " " + normProject[1]);
        Debug.DrawRay(groundHits[0].point, groundHits[0].normal, Color.blue, 0, false);
        Debug.DrawRay(groundHits[1].point, groundHits[1].normal, Color.green, 0, false);
        // cheat
        if (controls.Debug.ActivateFlight.WasPressedThisFrame())
        {
            ActivateFlight();
            ActivateSprint();
            ActivateParagliding();
            audioSource.PlayOneShot(switchSound);
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
        
        grounded = colliders.Length > 0; 
        
        
        MovePlayer();
    }
    
    private void OnEnable()
    {
        controls.Enable(); 
    }

    private void OnDisable()
    {
        controls.Disable(); 
    }

    private void DealInput()
    {
        // use new input system to get the input value
        Vector2 movement2D = directionInput.ReadValue<Vector2>();
        horizontalInput = movement2D.x;
        verticalInput = movement2D.y;
        

        // jump input
        if(jumpInput.WasPressedThisFrame() && readyToJump && grounded && flightStamina > jumpStaminaConsume)
        {
            readyToJump = false;
            Invoke(nameof(ResetJump), jumpCooldown);
            requestJump = true;
            preparingJump = true;
        }
        
        // fly input
        if (flightActivated && !grounded && jumpInput.IsPressed() && flightStamina > 0)
        {
            requestFlight = true;
        }
        else
        {
            requestFlight = false;
        }
        
        // sprint input
        if (sprintActivated && grounded && (sprintInput.IsPressed() || switchSprintInput.WasPressedThisFrame() || keepSprint) && moveDirection.magnitude > 0.02f && !sliding)
        {
            if (sprinting)
            {
                sprinting = flightStamina > 0;  // end sprinting if stamina less than 0
            }
            else
            {
                sprinting = flightStamina > sprintMinStamina;  // could only start sprint if stamina larger than certain value
                if (sprinting && switchSprintInput.WasPressedThisFrame())  // if sprint started by left joystick
                {
                    keepSprint = true;
                }
            }
        }
        else
        {
            sprinting = false;
        }

        if (!sprinting) 
            keepSprint = false;
        
        if (sliding)
        {
            sliding = new Vector3(rb.velocity.x, 0f, rb.velocity.z).magnitude > slideSpeedThreshold && grounded && !sprinting;
        }
        else
        {
            sliding = new Vector3(rb.velocity.x, 0f, rb.velocity.z).magnitude > slideSpeedThreshold + slideStartSpeedOffset &&
                      grounded && !sprinting;
        }
        
    }

    
    // called in FixedUpdate
    private void MovePlayer()
    {
        // calculate movement direction
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;
        
        //set friction if player has no input
        if (moveDirection.magnitude < 0.05f && grounded)
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
            Vector3 moveForce = moveDirection.normalized * (sprinting ? sprintSpeed : moveSpeed) - CalculateResistance();
            rb.AddForce(moveForce, ForceMode.Force);
        }
        // in the air
        else if (!grounded)
        {
            Vector3 moveForce = moveDirection.normalized * (airMultiplier * moveSpeed) - CalculateResistance();
            rb.AddForce(moveForce, ForceMode.Force);
            
            // limit descent speed if player has input
            if (paraglidingActivated && rb.velocity.y < 0 && moveDirection.magnitude > 0.05f)
            {
                rb.AddForce(airDragVertical * rb.velocity.y * Vector3.down, ForceMode.Force);
            }
        }
        // end horizontal movement
        
        // begin vertical movement
        if (grabGround)
        {
            //rb.AddForce(animator.transform.TransformDirection(-0.5f * (groundHits[0].normal + groundHits[0].normal)) * 50, ForceMode.Force);
            rb.AddForce(50 * -0.5f * (groundHits[0].normal + groundHits[0].normal), ForceMode.Force);
        }
        
        rb.useGravity = true;
        if (requestJump) // jump control
        {
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

    private void Jump()
    {
        animationVars.requestJump = false;
        
        flightStamina -= jumpStaminaConsume;
        flightStamina = Mathf.Clamp(flightStamina, 0, maxFlightStamina);
        // reset y velocity
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }
    
    private void FlyUp()
    {
        rb.useGravity = false;
        rb.AddForce(Vector3.up * flightForce, ForceMode.Acceleration);
    }
    
    
    private void ResetJump()
    {
        readyToJump = true;
        preparingJump = false;
    }
    
    
    private Vector3 CalculateResistance()
    {
        Vector3 horizontalVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        float resistanceMag = 0;
        if (grounded && !sprinting)  // walking drag
        {
            // the resistance increases linearly with horizontal velocity. But has a max value
            resistanceMag = Mathf.Clamp(horizontalVelocity.magnitude * groundDrag, 0, moveSpeed + slideResistance);
        }
        else if (grounded && sprinting)
        {
            resistanceMag = Mathf.Clamp(horizontalVelocity.magnitude * groundDragSprint, 0, sprintSpeed*2);
        }
        else
        {
            resistanceMag = horizontalVelocity.magnitude * airDragHorizontal;
        }
        Vector3 resistanceForce = resistanceMag * horizontalVelocity.normalized;
        return resistanceForce;
        
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("StaminaPowerUp")) // tag name might be changed
        {
            //todo get the stamina increase amount from the collider object
            var staminaIncrease = 3;  // change later

            maxFlightStamina += staminaIncrease;
            
            //todo maybe some effect happens
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
        
        animator.SetBool("grounded", animationVars.grounded);
        animator.SetFloat("verticalSpeed", animationVars.verticalSpeed);
        animator.SetFloat("horizontalSpeed", animationVars.horizontalSpeed);
        animator.SetBool("requestJump", animationVars.requestJump);
        animator.SetBool("sliding", animationVars.sliding);
        animator.SetBool("paragliding", animationVars.paragliding);
        animator.SetBool("horizontalInput", animationVars.horizontalInput);
        animator.SetBool("sprinting", animationVars.sprinting);
    }

    public AnimationVars getAnimationVars()
    {
        return animationVars;
    }

    public float GetFlightStamina() {
        return flightStamina;
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
}
