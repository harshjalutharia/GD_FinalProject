using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Control how fast player moves, doesn't represent actual speed")]
    public float moveSpeed;

    [SerializeField, Tooltip("READ ONLY. Controls the max walking speed on a level surface. Vmax=moveSpeed/groundDrag")]
    private float slideSpeedThreshold;

    [Tooltip("Start slide if current speed larger than slideSpeedThreshold + slideStartSpeedOffset.")]
    public float slideStartSpeedOffset;
    
    [Tooltip("Limit the max walking speed, Vmax=moveSpeed/groundDrag")]
    public float groundDrag;

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
    
    private bool readyToJump;
    
    private bool requestJump;
    
    [SerializeField] 
    private bool requestFlight;
    
    [Tooltip("Maximum stamina for flight")]
    public float maxFlightStamina;
    
    [Tooltip("Stamina consumed by jumping")]
    public float jumpStaminaConsume;
    
    [SerializeField, Tooltip("Current stamina")] 
    private float flightStamina;
    
    [Tooltip("Stamina consumed per second of flight")]
    public float flightStaminaDecreaseSpeed;
    
    [Tooltip("Stamina regained per second while on the ground")]
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
    [Tooltip("Animation Controller")]
    public Animator animator;

    [SerializeField, Tooltip("READ ONLY. Variables bind for animation control")]
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

        slideSpeedThreshold = moveSpeed / groundDrag;

        physicMaterial = GetComponent<CapsuleCollider>().material;
    }

    private void Update()
    {
        // ground check
        Collider[] colliders = Physics.OverlapSphere(orientation.position, 0.05f, GroundMask);
        int count = 0;
        foreach (var colider in colliders)
        {
            if (colider.gameObject.transform != this.transform)
            {
                count++;
            }
        }
        grounded = count > 0 ;
        // grounded = Physics.Raycast(orientation.position, Vector3.down, 0.2f, GroundMask);
        
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
        // SpeedControl();
    }

    private void DealInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        // jump input
        if(Input.GetKey(jumpKey) && readyToJump && grounded && flightStamina > jumpStaminaConsume)
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
        // if (grounded)
        //     rb.drag = groundDrag;
        // else
        //     rb.drag = airDrag;
        
        // calculate movement direction
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;
        
        //set friction if player has no input
        if (moveDirection.magnitude < 0.05f)
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
            Vector3 moveForce = moveDirection.normalized * moveSpeed - CalculateResistance(groundDrag);
            rb.AddForce(moveForce, ForceMode.Force);
        }
        // in air
        else if (!grounded)
        {
            Vector3 moveForce = moveDirection.normalized * moveSpeed - CalculateResistance(airDragHorizontal);
            rb.AddForce(airMultiplier * moveForce, ForceMode.Force);
            
            // limit descent speed if player has input
            if (rb.velocity.y < 0 && moveDirection.magnitude > 0.05f)
            {
                rb.AddForce(airDragVertical * rb.velocity.y * Vector3.down, ForceMode.Force);
            }
        }
        // end horizontal movement
        
        // begin vertical movement
        rb.useGravity = true;
        if (requestJump)
        {
            Jump();
            flightStamina -= jumpStaminaConsume;
            flightStamina = Mathf.Clamp(flightStamina, 0, maxFlightStamina);
            requestJump = false; 
            animationVars.requestJump = true;
            Invoke(nameof(ResetAnimatorRequestJump), 0.5f);
        }
        else if (requestFlight)
        {
            FlyUp();
            flightStamina -= flightStaminaDecreaseSpeed * Time.fixedDeltaTime;
            flightStamina = Mathf.Clamp(flightStamina, 0, maxFlightStamina);
        }
        
    }

    // private void SpeedControl()
    // {
    //     Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
    //
    //     // limit velocity if needed
    //     if(flatVel.magnitude > moveSpeed)
    //     {
    //         Vector3 limitedVel = flatVel.normalized * moveSpeed;
    //         rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
    //     }
    // }

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
    
    
    private Vector3 CalculateResistance(float drag)
    {
        // the resistance increases linearly with horizontal velocity. But has a max value
        Vector3 horizontalVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        float resistanceMag = Mathf.Clamp(horizontalVelocity.magnitude * drag, 0, moveSpeed + slideResistance);
        Vector3 resistanceForce = resistanceMag * horizontalVelocity.normalized;
        return resistanceForce;
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
        animationVars.horizontalInput = moveDirection.magnitude > 0.05f;
        if (animationVars.sliding)
        {
            animationVars.sliding = animationVars.horizontalSpeed > slideSpeedThreshold && grounded;
        }
        else
        {
            animationVars.sliding = animationVars.horizontalSpeed > slideSpeedThreshold + slideStartSpeedOffset && grounded;
        }
        
        
        animator.SetBool("grounded", animationVars.grounded);
        animator.SetFloat("verticalSpeed", animationVars.verticalSpeed);
        animator.SetFloat("horizontalSpeed", animationVars.horizontalSpeed);
        animator.SetBool("requestJump", animationVars.requestJump);
        animator.SetBool("sliding", animationVars.sliding);
        animator.SetBool("horizontalInput", animationVars.horizontalInput);
    }

    public float GetFlightStamina() {
        return flightStamina;
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
    }

    
}
