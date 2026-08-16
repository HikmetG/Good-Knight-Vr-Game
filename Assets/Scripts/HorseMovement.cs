using UnityEngine;
using UnityEngine.InputSystem;

public class HorseMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float turnSpeed = 90f;
    
    [SerializeField] private float reverseSpeedMultiplier = 0.3f; 

    [Header("Realistic Momentum")]
    [SerializeField] private float brakingPower = 2.5f; 
    [SerializeField] private float accelerationPower = 4f;

    [Header("Animation Settings")]
    [SerializeField] private float animationDampTime = 0.15f; 

    [Header("VR Input Actions")]
    [SerializeField] private InputActionProperty moveAction; 

    [Header("Dynamic Hoofbeat Audio")]
    [Tooltip("Drag the AudioSource component carrying your horse running loop here.")]
    [SerializeField] private AudioSource hoofbeatSource;
    [Tooltip("The minimum speed before the horse starts making walking sounds.")]
    [SerializeField] private float minSpeedToPlay = 0.2f;
    [Tooltip("Your horse's top galloping speed (used to cap maximum volume/pitch).")]
    [SerializeField] private float maxGallopSpeed = 12f;
    [SerializeField] private float minPitch = 0.7f;
    [SerializeField] private float maxPitch = 1.3f;

    private Rigidbody rb;
    private VRAvatarFollow avatarScript;
    private Animator horseAnimator; 
    private bool playerIsRiding = false;
    private float currentPhysicalForwardSpeed = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        avatarScript = FindFirstObjectByType<VRAvatarFollow>();
        horseAnimator = GetComponent<Animator>(); 
        
        if (moveAction.action != null) moveAction.action.Enable();

        if (hoofbeatSource != null)
        {
            hoofbeatSource.loop = true;
            hoofbeatSource.Stop();
        }
    }

    void FixedUpdate()
    {
        if (JoustGameManager.Instance != null && 
           (JoustGameManager.Instance.currentState == JoustState.Paused || 
            JoustGameManager.Instance.currentState == JoustState.MainMenu))
        {
            currentPhysicalForwardSpeed = 0f;
            if (hoofbeatSource != null && hoofbeatSource.isPlaying)
            {
                hoofbeatSource.Stop();
            }
            return; 
        }

        if (avatarScript != null)
        {
            Animator knightAnim = avatarScript.GetComponentInChildren<Animator>();
            playerIsRiding = knightAnim != null && knightAnim.GetBool("IsRiding");
        }

        if (!playerIsRiding) 
        {
            currentPhysicalForwardSpeed = Mathf.MoveTowards(currentPhysicalForwardSpeed, 0f, Time.fixedDeltaTime * brakingPower * 2);
            if (horseAnimator != null)
            {
                horseAnimator.SetFloat("HorseSpeed", 0f, animationDampTime, Time.fixedDeltaTime);
            }

            UpdateHoofbeatAudio(Mathf.Abs(currentPhysicalForwardSpeed));
            return;
        }

        Vector2 input = moveAction.action != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;

        // Handle Steering
        float turnFactor = Mathf.Clamp01(Mathf.Abs(currentPhysicalForwardSpeed) / 1f); 
        float turnAmount = input.x * turnSpeed * turnFactor * Time.fixedDeltaTime;
        Quaternion turnRotation = Quaternion.Euler(0f, turnAmount, 0f);
        rb.MoveRotation(rb.rotation * turnRotation);

        // CALCULATE REALISTIC MOMENTUM WITH REVERSE LIMITER
        float targetSpeed = input.y * moveSpeed;

        // If input is negative (pulling joystick back), nerf the target speed down!
        if (input.y < -0.05f)
        {
            targetSpeed = input.y * (moveSpeed * reverseSpeedMultiplier);
        }

        float currentPowerRate = (Mathf.Abs(targetSpeed) > Mathf.Abs(currentPhysicalForwardSpeed)) ? accelerationPower : brakingPower;
        currentPhysicalForwardSpeed = Mathf.MoveTowards(currentPhysicalForwardSpeed, targetSpeed, Time.fixedDeltaTime * currentPowerRate);

        // Move the Rigidbody
        Vector3 forwardMovement = transform.forward * currentPhysicalForwardSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + forwardMovement);

        // Sync the Blend Tree
        if (horseAnimator != null)
        {
            float speedPercentage = Mathf.Abs(currentPhysicalForwardSpeed) / moveSpeed; 
            float animationVelocityTarget = speedPercentage * 5f; 

            if (currentPhysicalForwardSpeed < -0.05f)
            {
                animationVelocityTarget = Mathf.Min(animationVelocityTarget, 2.0f);
            }

            horseAnimator.SetFloat("HorseSpeed", animationVelocityTarget);
        }

        // Dynamic Audio Update
        UpdateHoofbeatAudio(Mathf.Abs(currentPhysicalForwardSpeed));
    }

    private void UpdateHoofbeatAudio(float speed)
    {
        if (hoofbeatSource == null) return;

        // If moving faster than threshold, play and scale the audio properties
        if (speed > minSpeedToPlay)
        {
            if (!hoofbeatSource.isPlaying)
            {
                hoofbeatSource.Play();
            }

            // Calculate current speed factor relative to max expected speed (0.0 to 1.0)
            float speedFactor = Mathf.Clamp01(speed / maxGallopSpeed);

            // Smoothly lerp volume and pitch to match speed!
            hoofbeatSource.volume = Mathf.Lerp(0.15f, 1.0f, speedFactor);
            hoofbeatSource.pitch = Mathf.Lerp(minPitch, maxPitch, speedFactor);
        }
        else
        {
            // If the horse is completely stationary, stop the audio loop
            if (hoofbeatSource.isPlaying)
            {
                hoofbeatSource.Stop();
            }
        }
    }
}