using UnityEngine;

public class JoustingAI : MonoBehaviour
{
    public enum JoustState { WaitingAtTilt, Charging, LoweringLance, SlowingDown }
    
    [Header("Current State")]
    [SerializeField] private JoustState currentState = JoustState.WaitingAtTilt;

    [Header("Movement Settings")]
    [SerializeField] private float maxChargeSpeed = 8f;
    [SerializeField] private float acceleration = 3f;
    [SerializeField] private float brakingPower = 4f;

    [Header("6-Waypoint Track Layout")]
    [Tooltip("Assign all 6 waypoints here in numerical order (0=WP_1, 1=WP_2, etc.)")]
    [SerializeField] private Transform[] waypoints; 
    [SerializeField] private Transform playerTransform;

    [Header("Combat Thresholds")]
    [SerializeField] private float lanceLowerDistance = 15f; 

    [Header("Dynamic Hoofbeat Audio")]
    [Tooltip("Drag the AudioSource component carrying the AI horse running loop here.")]
    [SerializeField] private AudioSource hoofbeatSource;
    [Tooltip("The minimum speed before the horse starts making walking sounds.")]
    [SerializeField] private float minSpeedToPlay = 0.2f;
    [Tooltip("The AI horse's top speed (used to cap maximum volume and pitch).")]
    [SerializeField] private float maxGallopSpeed = 8f;
    [SerializeField] private float minPitch = 0.7f;
    [SerializeField] private float maxPitch = 1.3f;

    private Rigidbody rb;
    private Animator horseAnimator;
    private Animator knightAnimator;
    private float currentSpeed = 0f;
    
    // Dynamic path targets updated when a round begins
    private Transform currentTargetWaypoint;
    private Transform currentMidWaypoint;
    private Transform currentEndWaypoint;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        horseAnimator = GetComponent<Animator>();
        knightAnimator = GetComponentInChildren<Animator>();
        
        // Safety initialization using your first path variables
        if (waypoints != null && waypoints.Length >= 3)
        {
            currentMidWaypoint = waypoints[1];
            currentEndWaypoint = waypoints[2];
            currentTargetWaypoint = currentMidWaypoint;
        }

        // Initialize the audio source properties cleanly on launch
        if (hoofbeatSource != null)
        {
            hoofbeatSource.loop = true;
            hoofbeatSource.Stop();
        }
    }

    void FixedUpdate()
    {
        switch (currentState)
        {
            case JoustState.WaitingAtTilt:
                HandleWaiting();
                break;
            case JoustState.Charging:
                HandleCharge();
                CheckDistanceForLance();
                break;
            case JoustState.LoweringLance:
                HandleCharge(); 
                CheckForPass();
                break;
            case JoustState.SlowingDown:
                HandleBraking();
                break;
        }

        if (currentState != JoustState.WaitingAtTilt && currentTargetWaypoint != null)
        {
            Vector3 targetDir = (currentTargetWaypoint.position - transform.position).normalized;
            targetDir.y = 0; 
            
            rb.MovePosition(rb.position + targetDir * currentSpeed * Time.fixedDeltaTime);
            
            if (targetDir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(targetDir);
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, Time.fixedDeltaTime * 5f));
            }
        }

        SyncAnimations();

        // Dynamic hoofbeat calculation based on physical speed
        UpdateHoofbeatAudio(currentSpeed);
    }

    private void HandleWaiting()
    {
        currentSpeed = 0f;
    }

    private void HandleCharge()
    {
        currentSpeed = Mathf.MoveTowards(currentSpeed, maxChargeSpeed, Time.fixedDeltaTime * acceleration);

        if (currentMidWaypoint != null && Vector3.Distance(transform.position, currentMidWaypoint.position) < 2f)
        {
            currentTargetWaypoint = currentEndWaypoint;
        }
    }

    private void CheckDistanceForLance()
    {
        if (playerTransform == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        if (distanceToPlayer <= lanceLowerDistance)
        {
            currentState = JoustState.LoweringLance;
            if (knightAnimator != null)
            {
                knightAnimator.SetBool("LowerLance", true); 
            }
        }
    }

    private void CheckForPass()
    {
        if (currentEndWaypoint != null && Vector3.Distance(transform.position, currentEndWaypoint.position) < 5f)
        {
            currentState = JoustState.SlowingDown;
            if (knightAnimator != null)
            {
                knightAnimator.SetBool("LowerLance", false); 
            }
        }
    }

    private void HandleBraking()
    {
        currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, Time.fixedDeltaTime * brakingPower);
        
        if (currentSpeed <= 0.05f)
        {
            currentState = JoustState.WaitingAtTilt;
            
            // Ping the referee manager that the horse has finished this run pass safely
            if (JoustGameManager.Instance != null)
            {
                JoustGameManager.Instance.NotifyAIArrival();
            }
        }
    }

    private void SyncAnimations()
    {
        if (horseAnimator != null)
        {
            float animValue = (currentSpeed / maxChargeSpeed) * 5f; 
            horseAnimator.SetFloat("HorseSpeed", animValue);
        }
    }

    // Smooth volume/pitch scaler that links directly with the AI horse's movement speed
    private void UpdateHoofbeatAudio(float speed)
    {
        if (hoofbeatSource == null) return;

        if (JoustGameManager.Instance != null && 
           (JoustGameManager.Instance.currentState == global::JoustState.Paused || 
            JoustGameManager.Instance.currentState == global::JoustState.MainMenu))
        {
            if (hoofbeatSource.isPlaying) hoofbeatSource.Stop();
            return;
        }

        if (speed > minSpeedToPlay)
        {
            if (!hoofbeatSource.isPlaying)
            {
                hoofbeatSource.Play();
            }

            // Normalizes current speed against top speed to calculate relative volume/pitch scale
            float speedFactor = Mathf.Clamp01(speed / maxGallopSpeed);

            hoofbeatSource.volume = Mathf.Lerp(0.15f, 1.0f, speedFactor);
            hoofbeatSource.pitch = Mathf.Lerp(minPitch, maxPitch, speedFactor);
        }
        else
        {
            if (hoofbeatSource.isPlaying)
            {
                hoofbeatSource.Stop();
            }
        }
    }

    // Activated directly by the central manager referee
    public void StartJoustCharge(int roundNumber)
    {
        if (currentState == JoustState.WaitingAtTilt && waypoints != null && waypoints.Length >= 6)
        {
            if (roundNumber % 2 != 0)
            {
                currentMidWaypoint = waypoints[1]; 
                currentEndWaypoint = waypoints[2]; 
            }
            else
            {
                currentMidWaypoint = waypoints[4]; 
                currentEndWaypoint = waypoints[5]; 
            }

            currentTargetWaypoint = currentMidWaypoint;
            currentState = JoustState.Charging;
        }
    }

    public void PositionForReturnRound()
    {
        if (waypoints != null && waypoints.Length >= 6 && waypoints[3] != null)
        {
            transform.position = new Vector3(waypoints[3].position.x, transform.position.y, waypoints[3].position.z);
            
            if (waypoints[4] != null)
            {
                Vector3 lookTarget = new Vector3(waypoints[4].position.x, transform.position.y, waypoints[4].position.z);
                transform.LookAt(lookTarget);
            }
        }
    }

    public void PositionForForwardRound()
    {
        if (waypoints != null && waypoints.Length >= 6 && waypoints[0] != null)
        {
            transform.position = new Vector3(waypoints[0].position.x, transform.position.y, waypoints[0].position.z);
            
            if (waypoints[1] != null)
            {
                Vector3 lookTarget = new Vector3(waypoints[1].position.x, transform.position.y, waypoints[1].position.z);
                transform.LookAt(lookTarget);
            }
        }
    }
}