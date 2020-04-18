using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(CharacterController), typeof(PlayerInputHandler), typeof(AudioSource))]
public class PlayerCharacterController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the player transform")]
    public Transform orientation;
    [Tooltip("Reference to the main camera used for the player")]
    public GameObject CameraGameObject;
    public Camera playerCamera;
    [Tooltip("Audio source for footsteps, jump, etc...")]
    public AudioSource audioSource;
    [Tooltip("Stance HUD element")]
    public StanceHUD stanceHUD;
    //[Tooltip("Character Animator")]
    //public Animator animator;

    [Header("General")]
    [Tooltip("Force applied downward when in the air")]
    public float gravityDownForce = 25f;
    [Tooltip("Physic layers checked to consider the player grounded")]
    public LayerMask groundCheckLayers = -1;
    [Tooltip("distance from the bottom of the character controller capsule to test for grounded")]
    public float groundCheckDistance = 0.05f;

    [Header("Basic Movement")]
    [Tooltip("Max movement speed when grounded (when not sprinting)")]
    public float maxSpeedOnGround = 13f;
    [Tooltip("Sharpness for the movement when grounded, a low value will make the player accelerate and decelerate slowly, a high value will do the opposite")]
    public float movementSharpnessOnGround = 15;
    [Tooltip("Max movement speed when crouching")]
    [Range(0, 1)]
    public float maxSpeedCrouchedRatio = 0.5f;
    [Tooltip("Max movement speed when not grounded")]
    public float maxSpeedInAir = 20f;
    [Tooltip("Acceleration speed when in the air")]
    public float accelerationSpeedInAir = 25f;
    [Tooltip("Multiplicator for the sprint speed (based on grounded speed)")]
    public float sprintSpeedModifier = 2f;
    [Tooltip("Height at which the player dies instantly when falling off the map")]
    public float killHeight = -50f;

    [Space(10)]
    [Header("Sliding")]
    [Tooltip("The decay rate of the speed when sliding")]
    public float slideSpeedDecayRate = 6f;
    [Tooltip("The starting speed when sliding")]
    public float slidespeed = 3f;
    [Tooltip("The speed of camera tilting up")]
    public float slidingTiltTime = 0.3f;
    public float slidingTiltAngle = 12.5f;
    [Space(5)]
    public float slopeSlideSpeedModifier = 2f;
    public float slopeAngle = 20f;

    [Space(10)]
    [Header("Wall Running")]
    //[Tooltip("The starting speed when sliding")]
    public float wallRunTiltTime = 0.3f;
    public float wallRunTiltAngle = 12.5f;
    public float wallRunTriggerDistance = 1f;
    public float wallRunMinTriggerSpeed = 8f;
    public float wallRunCooldown = 0.2f;
    public float jumpFromWallForce = 10f;
    public float jumpFromWallMinAngle = 10f;
    public float wallRunGravityModifier = 0.5f;
    private float nextWallrun = 0;
    private Vector3 wallNormal;

    [Header("Rotation")]
    [Tooltip("Rotation speed for moving the camera")]
    public float rotationSpeed = 200f;
    [Range(0.1f, 1f)]
    [Tooltip("Rotation speed multiplier when aiming")]
    public float aimingRotationMultiplier = 0.4f;

    [Header("Jump")]
    [Tooltip("Force applied upward when jumping")]
    public float jumpForce = 12f;

    [Header("Stance")]
    [Tooltip("Ratio (0-1) of the character height where the camera will be at")]
    public float cameraHeightRatio = 0.9f;
    [Tooltip("Height of character when standing")]
    public float capsuleHeightStanding = 1.8f;
    [Tooltip("Height of character when crouching")]
    public float capsuleHeightCrouching = 0.9f;
    [Tooltip("Speed of crouching transitions")]
    public float crouchingSharpness = 10f;

    [Header("Audio")]
    [Tooltip("Amount of footstep sounds played when moving one meter")]
    public float footstepSFXFrequency = 1f;
    [Tooltip("Amount of footstep sounds played when moving one meter while sprinting")]
    public float footstepSFXFrequencyWhileSprinting = 1f;
    [Tooltip("Sound played for footsteps")]
    public AudioClip footstepSFX;
    [Tooltip("Sound played when jumping")]
    public AudioClip jumpSFX;
    [Tooltip("Sound played when landing")]
    public AudioClip landSFX;
    [Tooltip("Sound played when taking damage froma fall")]
    public AudioClip fallDamageSFX;

    [Header("Fall Damage")]
    [Tooltip("Whether the player will recieve damage when hitting the ground at high speed")]
    public bool recievesFallDamage;
    [Tooltip("Minimun fall speed for recieving fall damage")]
    public float minSpeedForFallDamage = 10f;
    [Tooltip("Fall speed for recieving th emaximum amount of fall damage")]
    public float maxSpeedForFallDamage = 30f;
    [Tooltip("Damage recieved when falling at the mimimum speed")]
    public float fallDamageAtMinSpeed = 10f;
    [Tooltip("Damage recieved when falling at the maximum speed")]
    public float fallDamageAtMaxSpeed = 50f;

    public bool isGrappling=false;
    public Vector3 grappleVel = Vector3.zero;


    public UnityAction<bool> onStanceChanged;

    public Vector3 characterVelocity { get; set; }
    public bool isGrounded { get; private set; }
    public bool hasJumpedThisFrame { get; private set; }
    public bool isDead { get; private set; }
    public bool isCrouching { get; private set; }
    public bool isSliding { get; private set; }
    public bool isSlidingSlope { get; private set; }
    public bool isSprinting { get; private set; }
    public bool isWallRunning { get; private set; }
    public bool isJumping { get; private set; }
    public bool isOnSlope { get; private set; }
    public float RotationMultiplier
    {
        get
        {
            if (m_WeaponsManager.isAiming)
            {
                return aimingRotationMultiplier;
            }

            return 1f;
        }
    }

    private float slidingTime = 0;

    private Vector3 wallRunDirection;

    Health m_Health;
    PlayerInputHandler m_InputHandler;
    CharacterController m_Controller;
    PlayerWeaponsManager m_WeaponsManager;

    Actor m_Actor;

    Vector3 m_GroundNormal;
    Vector3 m_LatestImpactSpeed;
    float m_LastTimeJumped = 0f;
    float m_CameraVerticalAngle = 0f;
    float m_CameraHorizontalAngle = 0f;
    float m_footstepDistanceCounter;
    float m_TargetCharacterHeight;


    const float k_JumpGroundingPreventionTime = 0.2f;
    const float k_GroundCheckDistanceInAir = 0.07f;
    void Start()
    {
        // fetch components on the same gameObject
        m_Controller = GetComponent<CharacterController>();
        DebugUtility.HandleErrorIfNullGetComponent<CharacterController, PlayerCharacterController>(m_Controller, this, gameObject);

        m_InputHandler = GetComponent<PlayerInputHandler>();
        DebugUtility.HandleErrorIfNullGetComponent<PlayerInputHandler, PlayerCharacterController>(m_InputHandler, this, gameObject);

        m_WeaponsManager = GetComponent<PlayerWeaponsManager>();
        DebugUtility.HandleErrorIfNullGetComponent<PlayerWeaponsManager, PlayerCharacterController>(m_WeaponsManager, this, gameObject);

        m_Health = GetComponent<Health>();
        DebugUtility.HandleErrorIfNullGetComponent<Health, PlayerCharacterController>(m_Health, this, gameObject);

        m_Actor = GetComponent<Actor>();
        DebugUtility.HandleErrorIfNullGetComponent<Actor, PlayerCharacterController>(m_Actor, this, gameObject);

        m_Controller.enableOverlapRecovery = true;

        m_Health.onDie += OnDie;

        stanceHUD = GameObject.Find("GameManager/GameHUD").GetComponent<StanceHUD>();

        CameraGameObject = GameObject.FindGameObjectWithTag("MainCamera");

        playerCamera = CameraGameObject.GetComponent<Camera>();

        // force the crouch state to false when starting
        SetCrouchingState(false, false, true);
        UpdateCharacterHeight(true);
    }
    void Update()
    {

        // check for Y kill
        if (!isDead && transform.position.y < killHeight)
        {
            m_Health.Kill();
        }

        hasJumpedThisFrame = false;

        bool wasGrounded = isGrounded;
        GroundCheck();

        // landing
        if (isGrounded && !wasGrounded)
        {
            // Fall damage
            float fallSpeed = -Mathf.Min(characterVelocity.y, m_LatestImpactSpeed.y);
            float fallSpeedRatio = (fallSpeed - minSpeedForFallDamage) / (maxSpeedForFallDamage - minSpeedForFallDamage);
            if (recievesFallDamage && fallSpeedRatio > 0f)
            {
                float dmgFromFall = Mathf.Lerp(fallDamageAtMinSpeed, fallDamageAtMaxSpeed, fallSpeedRatio);
                m_Health.TakeDamage(dmgFromFall, null);

                // fall damage SFX
                audioSource.PlayOneShot(fallDamageSFX);
            }
            else
            {
                // land SFX
                audioSource.PlayOneShot(landSFX);
            }
        }

        // crouching or sliding
        SetCrouchingState(m_InputHandler.GetCrouchInputHeld() && isGrounded && !isGrappling, isCrouching, false);
        //animator.SetBool("IsCrouching", isCrouching);

        //if crouching or sliding
        UpdateCharacterHeight(false);

        // handle cursor movement and wasd with chracter controller 
        HandleCharacterMovement();

        if(!isGrappling)HandleWallRun();

        //update HUD stance
        UpdateStance();

    }

    void HandleWallRun()
    {
        Debug.DrawRay(CameraGameObject.transform.position, -transform.right * wallRunTriggerDistance, Color.green);
        Debug.DrawRay(CameraGameObject.transform.position, transform.right * wallRunTriggerDistance, Color.green);

        //stop wallrun if crouch
        if (isWallRunning && (m_InputHandler.GetCrouchInputHeld() || Vector3.Dot(characterVelocity, wallRunDirection) <= 3 || !Physics.Raycast(orientation.transform.position, -wallNormal, wallRunTriggerDistance)))
            StopWallrun();


        // if cant wallrun
        if (isGrounded || !isSprinting || isWallRunning)
            return;

        RaycastHit hit;

        ////confirm that is wallrunning
        //if (isWallRunning && (!Physics.Raycast(orientation.transform.position, -wallNormal, out hit, wallRunTriggerDistance) || Vector3.Dot(hit.normal, Vector3.up) != 0 || Math.Abs(Vector3.Dot(Vector3.Cross(Vector3.up, hit.normal), characterVelocity)) < wallRunMinTriggerSpeed))
        //    StopWallrun();


        //check if near wall
        RaycastHit hitright;

        bool didHitLeft = Physics.Raycast(orientation.transform.position, -transform.right, out hit, wallRunTriggerDistance);
        bool didHitRight = Physics.Raycast(orientation.transform.position, transform.right, out hitright, wallRunTriggerDistance);

        if (!didHitLeft && !didHitRight)
            return;
        else if (didHitRight && didHitLeft)
            hit = hit.distance > hitright.distance ? hitright : hit;
        else if (didHitRight && !didHitLeft)
            hit = hitright;

        //if (hit.collider.tag == "Wall") and the speed in the wall direction is more than minimum
        if (Vector3.Dot(hit.normal, Vector3.up) == 0 && Math.Abs(Vector3.Dot(Vector3.Cross(Vector3.up, hit.normal), characterVelocity)) >= wallRunMinTriggerSpeed && Time.time >= nextWallrun)
        {

            //stop falling
            if (characterVelocity.y < 0)
                characterVelocity = new Vector3(characterVelocity.x, 0, characterVelocity.z);

            wallNormal = hit.normal;
            isWallRunning = true;

            wallRunDirection = Vector3.Cross(Vector3.up, hit.normal).normalized;

            //find if running left or right
            float angleToWall = Vector3.Angle(characterVelocity, wallRunDirection);
            float wallRuntiltDirection = 1f;

            //if wall is to the left
            if (angleToWall > 90)
            {
                wallRunDirection *= -1;
                wallRuntiltDirection *= -1;
            }

            var rotateOut = LeanTween.rotateLocal(CameraGameObject, new Vector3(m_CameraVerticalAngle, m_CameraHorizontalAngle, wallRunTiltAngle * wallRuntiltDirection), wallRunTiltTime).setEase(LeanTweenType.easeInOutCubic);
        }


    }

    void OnDie()
    {
        isDead = true;
        // Tell the weapons manager to switch to a non-existing weapon in order to lower the weapon
        m_WeaponsManager.SwitchToWeaponIndex(-1, true);
    }

    void GroundCheck()
    {
        // Make sure that the ground check distance while already in air is very small, to prevent suddenly snapping to ground
        float chosenGroundCheckDistance = isGrounded ? (m_Controller.skinWidth + groundCheckDistance) : k_GroundCheckDistanceInAir;

        // reset values before the ground check
        isGrounded = false;
        //animator.SetBool("IsGrounded", false);
        m_GroundNormal = Vector3.up;

        // only try to detect ground if it's been a short amount of time since last jump; otherwise we may snap to the ground instantly after we try jumping
        if (Time.time >= m_LastTimeJumped + k_JumpGroundingPreventionTime)
        {
            // if we're grounded, collect info about the ground normal with a downward capsule cast representing our character capsule
            if (Physics.CapsuleCast(GetCapsuleBottomHemisphere(), GetCapsuleTopHemisphere(m_Controller.height), m_Controller.radius, Vector3.down, out RaycastHit hit, chosenGroundCheckDistance, groundCheckLayers, QueryTriggerInteraction.Ignore))
            {
                // storing the upward direction for the surface found
                m_GroundNormal = hit.normal;

                // Only consider this a valid ground hit if the ground normal goes in the same direction as the character up
                // and if the slope angle is lower than the character controller's limit
                if (Vector3.Dot(hit.normal, transform.up) > 0f &&
                    IsNormalUnderSlopeLimit(m_GroundNormal))
                {
                    isGrounded = true;
                    //animator.SetBool("IsGrounded", true);
                    

                    // handle snapping to the ground
                    if (hit.distance > m_Controller.skinWidth)
                    {
                        m_Controller.Move(Vector3.down * hit.distance);
                    }
                    isOnSlope = (Vector3.Angle(transform.up, m_GroundNormal) >= slopeAngle);
                }

                //animator.SetFloat("GroundDistance", hit.distance);

            }
        }
    }

    void HandleCharacterMovement()
    {
        // horizontal character rotation
        {
            transform.Rotate(new Vector3(0f, m_InputHandler.GetLookInputsHorizontal() * rotationSpeed * RotationMultiplier, 0f), Space.Self);
        }

        // vertical camera rotation
        {
            // add vertical inputs to the camera's vertical angle
            m_CameraVerticalAngle += m_InputHandler.GetLookInputsVertical() * rotationSpeed * RotationMultiplier;

            // limit the camera's vertical angle to min/max
            m_CameraVerticalAngle = Mathf.Clamp(m_CameraVerticalAngle, -89f, 89f);

            // apply the vertical angle as a local rotation to the camera transform along its right axis (makes it pivot up and down)
            playerCamera.transform.localEulerAngles = new Vector3(m_CameraVerticalAngle, 0, playerCamera.transform.rotation.eulerAngles.z);
        }

        // character movement handling
        {
            //check if running
            isSprinting = m_InputHandler.GetSprintInputHeld() && !isSliding && !isCrouching && !isGrappling;
            //animator.SetBool("IsSprinting", isSprinting);

            float speedModifier = isSprinting ? sprintSpeedModifier : 1f;

            //check if jumping
            isJumping = m_InputHandler.GetJumpInputDown() && !isGrappling;

            // converts move input to a worldspace vector based on our character's transform orientation
            Vector3 worldspaceMoveInput = transform.TransformVector(m_InputHandler.GetMoveInput());

            // handle grounded movement
            if (isGrounded)
            {
                //stop if wallruning
                if (isWallRunning)
                    StopWallrun();

                //handle slope sliding
                else if (isSlidingSlope)
                {
                    speedModifier = slopeSlideSpeedModifier;
                }
                //handle normal sliding
                else if (isSliding)
                {
                    speedModifier = slidespeed - (Time.time - slidingTime) * slideSpeedDecayRate;
                    if (speedModifier < 0)
                    {
                        //isSliding = false;
                        speedModifier = 0;
                    }

                }

                // calculate the desired velocity from inputs, max speed, and current slope
                Vector3 targetVelocity = worldspaceMoveInput * maxSpeedOnGround * speedModifier;

                // reduce speed if crouching by crouch speed ratio
                if (isCrouching)
                    targetVelocity *= maxSpeedCrouchedRatio;
                targetVelocity = GetDirectionReorientedOnSlope(targetVelocity.normalized, m_GroundNormal) * targetVelocity.magnitude;

                // smoothly interpolate between our current velocity and the target velocity based on acceleration speed
                characterVelocity = Vector3.Lerp(characterVelocity, targetVelocity, movementSharpnessOnGround * Time.deltaTime);


                // jumping
                if (isJumping)
                {
                    // force the crouch state to false
                    if (SetCrouchingState(false, false, false))
                    {
                        // start by canceling out the vertical component of our velocity
                        characterVelocity = new Vector3(characterVelocity.x, 0f, characterVelocity.z);

                        // then, add the jumpSpeed value upwards
                        characterVelocity += Vector3.up * jumpForce;

                        // play sound
                        audioSource.PlayOneShot(jumpSFX);

                        // remember last time we jumped because we need to prevent snapping to ground for a short time
                        m_LastTimeJumped = Time.time;
                        hasJumpedThisFrame = true;

                        // Force grounding to false
                        isGrounded = false;
                        //animator.SetBool("IsGrounded", false);
                        m_GroundNormal = Vector3.up;
                    }
                }

                // footsteps sound
                float chosenFootstepSFXFrequency = (isSprinting ? footstepSFXFrequencyWhileSprinting : footstepSFXFrequency);
                if (m_footstepDistanceCounter >= 1f / chosenFootstepSFXFrequency && !isSliding)
                {
                    m_footstepDistanceCounter = 0f;
                    audioSource.PlayOneShot(footstepSFX);
                }

                // keep track of distance traveled for footsteps sound
                m_footstepDistanceCounter += characterVelocity.magnitude * Time.deltaTime;
            }
            // handle air movement
            else
            {
                // add air acceleration
                characterVelocity += worldspaceMoveInput * accelerationSpeedInAir * Time.deltaTime;

                // limit air speed to a maximum, but only horizontally
                float verticalVelocity = characterVelocity.y;
                Vector3 horizontalVelocity = Vector3.ProjectOnPlane(characterVelocity, Vector3.up);
                horizontalVelocity = Vector3.ClampMagnitude(horizontalVelocity, maxSpeedInAir * speedModifier);
                characterVelocity = horizontalVelocity + (Vector3.up * verticalVelocity);


                if (isWallRunning)
                {
                    
                    //trying to jump from wall
                    if (isJumping)
                    {

                        //manage character movement
                        characterVelocity = jumpFromWallForce * Vector3.up;
                        characterVelocity += (wallNormal.normalized + wallRunDirection.normalized).normalized * horizontalVelocity.magnitude;
                        //characterVelocity = Vector3.ClampMagnitude(characterVelocity, 2*maxSpeedInAir);

                        //stop wallrunnning
                        StopWallrun();
                    }
                    else
                    {
                        //if wallrunning run parallel to wall
                        characterVelocity = wallRunDirection * horizontalVelocity.magnitude;
                        // apply the gravity to the velocity
                        characterVelocity += Vector3.down * gravityDownForce * wallRunGravityModifier * Time.deltaTime;
                        characterVelocity += verticalVelocity * Vector3.up;

                        // footsteps sound
                        float chosenFootstepSFXFrequency = (isSprinting ? footstepSFXFrequencyWhileSprinting : footstepSFXFrequency);
                        if (m_footstepDistanceCounter >= 1f / chosenFootstepSFXFrequency && !isSliding)
                        {
                            m_footstepDistanceCounter = 0f;
                            audioSource.PlayOneShot(footstepSFX);
                        }
                        // keep track of distance traveled for footsteps sound
                        m_footstepDistanceCounter += characterVelocity.magnitude * Time.deltaTime;
                    }
                }
                else
                {
                    if (m_InputHandler.GetCrouchInputHeld())
                        characterVelocity += Vector3.down * gravityDownForce * Time.deltaTime;
                    characterVelocity += Vector3.down * gravityDownForce * Time.deltaTime;
                }


            }
        }

        // apply the final calculated velocity value as a character movement
        Vector3 capsuleBottomBeforeMove = GetCapsuleBottomHemisphere();
        Vector3 capsuleTopBeforeMove = GetCapsuleTopHemisphere(m_Controller.height);

        if (isGrappling)
        {
            characterVelocity = grappleVel;
        }
        m_Controller.Move(characterVelocity * Time.deltaTime);

        // detect obstructions to adjust velocity accordingly
        m_LatestImpactSpeed = Vector3.zero;
        if (Physics.CapsuleCast(capsuleBottomBeforeMove, capsuleTopBeforeMove, m_Controller.radius, characterVelocity.normalized, out RaycastHit hit, characterVelocity.magnitude * Time.deltaTime, -1, QueryTriggerInteraction.Ignore))
        {
            // We remember the last impact speed because the fall damage logic might need it
            m_LatestImpactSpeed = characterVelocity;

            characterVelocity = Vector3.ProjectOnPlane(characterVelocity, hit.normal);
        }
    }

    private static float map(float value, float fromLow, float fromHigh, float toLow, float toHigh)
    {
        return (value - fromLow) * (toHigh - toLow) / (fromHigh - fromLow) + toLow;
    }

    // Returns true if the slope angle represented by the given normal is under the slope angle limit of the character controller
    bool IsNormalUnderSlopeLimit(Vector3 normal)
    {
        return Vector3.Angle(transform.up, normal) <= m_Controller.slopeLimit;
    }

    // Gets the center point of the bottom hemisphere of the character controller capsule    
    Vector3 GetCapsuleBottomHemisphere()
    {
        return transform.position + (transform.up * m_Controller.radius);
    }

    // Gets the center point of the top hemisphere of the character controller capsule    
    Vector3 GetCapsuleTopHemisphere(float atHeight)
    {
        return transform.position + (transform.up * (atHeight - m_Controller.radius));
    }

    // Gets a reoriented direction that is tangent to a given slope
    public Vector3 GetDirectionReorientedOnSlope(Vector3 direction, Vector3 slopeNormal)
    {
        Vector3 directionRight = Vector3.Cross(direction, transform.up);
        return Vector3.Cross(slopeNormal, directionRight).normalized;
    }

    void UpdateCharacterHeight(bool force)
    {
        // Update height instantly
        if (force)
        {
            m_Controller.height = m_TargetCharacterHeight;
            m_Controller.center = Vector3.up * m_Controller.height * 0.5f;
            playerCamera.transform.localPosition = Vector3.up * m_TargetCharacterHeight * cameraHeightRatio;
            m_Actor.aimPoint.transform.localPosition = m_Controller.center;
        }
        // Update smooth height
        else if (m_Controller.height != m_TargetCharacterHeight)
        {
            // resize the capsule and adjust camera position
            m_Controller.height = Mathf.Lerp(m_Controller.height, m_TargetCharacterHeight, crouchingSharpness * Time.deltaTime);
            m_Controller.center = Vector3.up * m_Controller.height * 0.5f;
            playerCamera.transform.localPosition = Vector3.Lerp(playerCamera.transform.localPosition, Vector3.up * m_TargetCharacterHeight * cameraHeightRatio, crouchingSharpness * Time.deltaTime);
            m_Actor.aimPoint.transform.localPosition = m_Controller.center;
        }
    }

    // returns false if there was an obstruction
    bool SetCrouchingState(bool crouched, bool wasCrouched, bool ignoreObstructions)
    {
        // set appropriate heights
        if (crouched)
        {
            //if sliding slope
            if (isOnSlope)
            {
                isSlidingSlope = true;
                return false;
            }
            //stop sliding slope if on level ground
            else if (isSlidingSlope)
            {
                isSlidingSlope = false;
            }

            m_TargetCharacterHeight = capsuleHeightCrouching;
            //if need to slide
            if (m_InputHandler.GetSprintInputHeld() && !wasCrouched && !isSliding)
            {
                var rotateOut = LeanTween.rotateLocal(CameraGameObject, new Vector3(-slidingTiltAngle, m_CameraHorizontalAngle, 0), slidingTiltTime).setEase(LeanTweenType.easeInOutCubic);

                isSliding = true;
                slidingTime = Time.time;
                StartCoroutine("slideCameraRotation", slidingTiltTime);
            }
            else if (!isSliding)
            {
                isCrouching = true;
            }
        }
        else
        {
            // Detect obstructions
            if (!ignoreObstructions)
            {
                Collider[] standingOverlaps = Physics.OverlapCapsule(
                    GetCapsuleBottomHemisphere(),
                    GetCapsuleTopHemisphere(capsuleHeightStanding),
                    m_Controller.radius,
                    -1,
                    QueryTriggerInteraction.Ignore);
                foreach (Collider c in standingOverlaps)
                {
                    if (c != m_Controller)
                    {
                        return false;
                    }
                }
            }

            m_TargetCharacterHeight = capsuleHeightStanding;
            isCrouching = false;
            isSliding = false;
            slidingTime = 0;
        }
        //animator.SetBool("IsCrouching", isCrouching);
        return true;
    }
    public enum Stance
    {
        Standing = 0,
        Sprinting,
        Crouching,
        Sliding,
        Wallrun,
        Jumping
    }

    //updates the stance image in the HUD
    void UpdateStance()
    {
        if (!isGrounded)
            stanceHUD.OnStanceChanged((int)Stance.Jumping);
        else if (isCrouching)
            stanceHUD.OnStanceChanged((int)Stance.Crouching);
        else if (isSliding)
            stanceHUD.OnStanceChanged((int)Stance.Sliding);
        else if (isSprinting)
            stanceHUD.OnStanceChanged((int)Stance.Sprinting);
        else
            stanceHUD.OnStanceChanged((int)Stance.Standing);

    }

    IEnumerator slideCameraRotation(float time)
    {
        yield return new WaitForSeconds(time - 1); //Count is the amount of time in seconds that you want to wait.
                                                   //And here goes your method of resetting the game...
        m_CameraVerticalAngle = -slidingTiltAngle;
        yield return null;
    }

    void StopWallrun()
    {
        isWallRunning = false;
        //set cooldown
        nextWallrun = Time.time + wallRunCooldown;
        //tilt back to ground
        var rotateOut = LeanTween.rotateLocal(CameraGameObject, new Vector3(m_CameraVerticalAngle, m_CameraHorizontalAngle, 0), wallRunTiltTime).setEase(LeanTweenType.easeInOutCubic);
    }

}