using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed;

    [Tooltip("Friction with ground")]
    public float groundDrag;

    [Tooltip("Friction in the air")] public float airDrag;

    public float jumpForce;
    
    [Tooltip("Used to avoid double jump")]
    public float jumpCooldown;
    
    [Tooltip("Used to reduces movement speed in the air")]
    public float airMultiplier;
    
    [Tooltip("Ascending force")]
    public float flightForce;
    
    private bool readyToJump;
    
    private bool requestJump;
    
    [SerializeField] 
    private bool requestFlight;
    
    [Tooltip("Max stamina used for fly")]
    public float maxFlightStamina;
    
    [Tooltip("How much stamina consumed when jump")]
    public float JumpStaminaConsume;
    
    [SerializeField] 
    private float flightStamina;
    
    [FormerlySerializedAs("flyStaminaDecreaseSpeed")] [Tooltip("Stamina consumption speed when flying")]
    public float flightStaminaDecreaseSpeed;
    
    [FormerlySerializedAs("flyStaminaRefillSpeed")] [Tooltip("Stamina refill speed when grounded")]
    public float flightStaminaRefillSpeed; 

    [Header("Input")]
    public KeyCode jumpKey = KeyCode.Space;
    
    private float horizontalInput;
    
    private float verticalInput;

    [Header("Ground Check")]
    public LayerMask GroundMask;
    
    [SerializeField] 
    private bool grounded;
    
    [Tooltip("Used to determine the player direction")]
    public Transform orientation;
    
    private Vector3 moveDirection;

    private Rigidbody rb;

    [Header("Animation")]
    public Animator animator;

    [SerializeField, Tooltip("Variables bind for animation control")]
    private AnimationVars animationVars;
    

    [Header("Debug UI Element")] 
    public TextMeshProUGUI debugText1;
    public TextMeshProUGUI debugText2;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        readyToJump = true;
        flightStamina = maxFlightStamina;
    }

    private void Update()
    {
        // ground check
        // Collider[] colliders1 = Physics.OverlapSphere(leftFoot.position, detectionRadius, GroundMask);
        // Collider[] colliders2 = Physics.OverlapSphere(rightFoot.position, detectionRadius, GroundMask);
        // grounded = colliders1.Length > 0 || colliders2.Length > 0;
        grounded = Physics.Raycast(orientation.position, Vector3.down, 0.1f, GroundMask);
        
        DealInput();
        
        // refill flight stamina when grounded
        if (grounded)
        {
            flightStamina += flightStaminaRefillSpeed * Time.deltaTime;
            flightStamina = Mathf.Clamp(flightStamina, 0, maxFlightStamina);
        }

        SetAnimation();
        
        // debug message
        float horizontalSpeed = new Vector3(rb.velocity.x, 0, rb.velocity.z).magnitude;
        float overAllSpeed = rb.velocity.magnitude;
        debugText1.text = "Stamina: " + flightStamina;
        debugText2.text = "Velocity:" + rb.velocity + " \nHorizontal Speed:" + horizontalSpeed + " Speed:" + overAllSpeed;
    }

    private void FixedUpdate()
    {
        MovePlayer();
        SpeedControl();
    }

    private void DealInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        // jump input
        if(Input.GetKey(jumpKey) && readyToJump && grounded && flightStamina > JumpStaminaConsume)
        {
            readyToJump = false;
            Invoke(nameof(ResetJump), jumpCooldown);
            requestJump = true;
        }
        
        // fly input
        if (!grounded && Input.GetKey(jumpKey) && flightStamina > 0)
        {
            requestFlight = true;
        }
        else
        {
            requestFlight = false;
        }
        
    }

    
    // called in FixedUpdate
    private void MovePlayer()
    {
        // handle drag
        if (grounded)
            rb.drag = groundDrag;
        else
            rb.drag = airDrag;
        
        // calculate movement direction
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;
        
        // begin horizontal movement
        // on ground
        if (grounded)
        {
            rb.AddForce(moveDirection.normalized * moveSpeed, ForceMode.Force);
        }
        // in air
        else if(!grounded)
            rb.AddForce(moveSpeed * airMultiplier * moveDirection.normalized, ForceMode.Force);
        // end horizontal movement
        
        // begin vertical movement
        rb.useGravity = true;
        if (requestJump)
        {
            Invoke(nameof(Jump), 0.5f);
            flightStamina -= JumpStaminaConsume;
            flightStamina = Mathf.Clamp(flightStamina, 0, maxFlightStamina);
            requestJump = false; 
            animationVars.requestJump = true;
            Invoke(nameof(ResetAnimatorRequestJump), 0.2f);
        }
        else if (requestFlight)
        {
            FlyUp();
            flightStamina -= flightStaminaDecreaseSpeed * Time.fixedDeltaTime;
            flightStamina = Mathf.Clamp(flightStamina, 0, maxFlightStamina);
        }
        
    }

    private void SpeedControl()
    {
        Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        // limit velocity if needed
        if(flatVel.magnitude > moveSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * moveSpeed;
            rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
        }
    }

    private void Jump()
    {
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
    }

    private void ResetAnimatorRequestJump()
    {
        animationVars.requestJump = false;
    }

    private void SetAnimation()
    {
        animationVars.grounded = grounded;
        animationVars.horizontalSpeed = new Vector3(rb.velocity.x, 0f, rb.velocity.z).magnitude;
        animationVars.verticalSpeed = rb.velocity.y;
        
        animator.SetBool("grounded", animationVars.grounded);
        animator.SetFloat("verticalSpeed", animationVars.verticalSpeed);
        animator.SetFloat("horizontalSpeed", animationVars.horizontalSpeed);
        animator.SetBool("requestJump", animationVars.requestJump);
    }
    
    
    [System.Serializable] 
    public struct AnimationVars
    {
        public bool grounded;
        public float verticalSpeed;
        public float horizontalSpeed;
        public bool requestJump;
    }
}
