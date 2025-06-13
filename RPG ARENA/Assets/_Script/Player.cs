using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    private const string ANIM_PARAM_WALKING = "Velocity";
    private const string ANIM_PARAM_JUMP = "Jump";
    private const string ANIM_PARAM_ISGROUNDED = "IsGrounded";
    private const string ANIM_PARAM_ISATTACK = "IsAttack";

    [Header("PlayerMovement Variable")]
    [SerializeField] private float playerSpeed = 10f;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float jumpPower = 5;
    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float lowJumpMultiplier = 2f;


    [Header("Slope Detection & Ground Detection")]
    [SerializeField] private float slopeRayDistance = 0.5f;
    [SerializeField] private float slideSpeed = 5f;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private float maxAngleSlope = 45;


    [Header("Player Reference")]
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private Transform slopeDetectionPoint;
    [SerializeField] private LayerMask groundLayer;
    private Animator animator;
    private GameInput gameInput;
    private Rigidbody rb;
    private Transform cameraTransform;

    //Interntal Variable
    Vector3 moveDir;
    Vector3 currentSlopeNormal = Vector3.up;
    bool isGrounded;
    bool wasGrounded;
    float currentSlopeAngle = 0f;
    RaycastHit slopeHit;

    private void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;

        rb = GetComponent<Rigidbody>();
        gameInput = GetComponent<GameInput>();
        cameraTransform = Camera.main.transform;
        animator = GetComponent<Animator>();

        wasGrounded = Physics.CheckSphere(groundCheckPoint.position, groundCheckRadius, groundLayer);
    }

    private void Start()
    {
        GameInput.Instance.OnJumpButtonClick += GameInput_OnJumpButtonClick;
        GameInput.Instance.OnAttackButtonClick += GameInput_OnAttackButtonClick;
    }

    private void Update()
    {
        HandleMovementInputAndRotation();
    }

    private void FixedUpdate()
    {
        wasGrounded = isGrounded;

        DetectGroundAndSlope();
        UpdateAnimation();

        if(isGrounded && !wasGrounded)
        {
            //Player landed after a jump
        }

        ApplyVelocityMovement();
        ApplyVariableGravity();
    }

    private void GameInput_OnJumpButtonClick(object sender, EventArgs e)
    {
        if (isGrounded && !IsSteepSlope())
        {
            rb.AddForce(Vector3.up * jumpPower, ForceMode.Impulse);

            if (animator != null)
            {
                animator.SetTrigger(ANIM_PARAM_JUMP);
            }
        }
    }

    private void GameInput_OnAttackButtonClick(object sender, EventArgs e)
    {
        animator.SetTrigger(ANIM_PARAM_ISATTACK);
    }

    private void ApplyVariableGravity()
    {
        if (rb.linearVelocity.y < 0)
        {
            rb.linearVelocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }
        else if (rb.linearVelocity.y > 0 && !GameInput.Instance.GetJumpButtonHeld())
        {
            rb.linearVelocity += Vector3.up * Physics.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
        }
    }

    private void HandleMovementInputAndRotation()
    {
        Vector2 moveInput = gameInput.GetMovementVectorNormalize();

        Vector3 cameraForward = cameraTransform.forward;
        cameraForward.y = 0;
        cameraForward.Normalize();

        Vector3 cameraRight = cameraTransform.right;
        cameraRight.y = 0;
        cameraRight.Normalize();

        moveDir = (cameraForward * moveInput.y + cameraRight * moveInput.x);

        // Use a small threshold to avoid issues with near-zero input
        if (moveDir.magnitude > 0.01f)
        {
            moveDir.Normalize();
        }
        else
        {
            // No significant input, set to zero
            moveDir = Vector3.zero;
        }

        if (moveDir.magnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void ApplyVelocityMovement()
    {
        Vector3 horizontalVelocity = Vector3.zero;

        //Player Movement in Ground
        if (isGrounded)
        {
            if (IsSteepSlope())
            {
                //Player Slide down if bigger angle than the max slope angle
                horizontalVelocity = Vector3.ProjectOnPlane(Vector3.down, currentSlopeNormal).normalized * slideSpeed;
            }
            else //On walkable ground and gentle slop
            {

                if (moveDir.magnitude > 0.01f)
                {
                    //Move player based on the angle
                    horizontalVelocity = Vector3.ProjectOnPlane(moveDir, currentSlopeNormal).normalized * playerSpeed;
                }   
            }
        }
        else
        {
            //Player Movement in Air
            horizontalVelocity = moveDir * playerSpeed;
        }
        //Horizontal Movement
        rb.linearVelocity = new Vector3(horizontalVelocity.x, rb.linearVelocity.y, horizontalVelocity.z);
    }

    private void DetectGroundAndSlope()
    {
        isGrounded = Physics.CheckSphere(groundCheckPoint.position, groundCheckRadius, groundLayer);
        currentSlopeNormal = Vector3.up;
        currentSlopeAngle = 0f;

        if (Physics.Raycast(slopeDetectionPoint.position, Vector3.down, out slopeHit, slopeRayDistance, groundLayer))
        {
            currentSlopeNormal = slopeHit.normal;
            currentSlopeAngle = Vector3.Angle(Vector3.up, currentSlopeNormal);
        }
    }

    private bool IsSteepSlope()
    {
        return currentSlopeAngle > maxAngleSlope && currentSlopeAngle < 90f;
    }

    private void UpdateAnimation()
    {
        animator.SetFloat(ANIM_PARAM_WALKING, moveDir.magnitude * playerSpeed);
        animator.SetBool(ANIM_PARAM_ISGROUNDED, isGrounded);
    }

    // --- Gizmos for Debugging ---
    private void OnDrawGizmosSelected()
    {
        if (groundCheckPoint)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckRadius);
        }

        if (slopeDetectionPoint)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(slopeDetectionPoint.position, slopeDetectionPoint.position + Vector3.down * slopeRayDistance);

            if (Physics.Raycast(slopeDetectionPoint.position, Vector3.down, out RaycastHit hit, slopeRayDistance, groundLayer))
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(hit.point, 0.05f);
                Gizmos.DrawRay(hit.point, hit.normal);
            }
        }
    }
}