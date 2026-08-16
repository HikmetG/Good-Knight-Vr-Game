using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class VRAvatarFollow : MonoBehaviour
{
    [Header("Tracking Targets")]
    [SerializeField] private Transform headTarget;     
    [SerializeField] private Transform saddleAnchor;   
    [SerializeField] private Transform xrOrigin;       
    [SerializeField] private Transform pelvisBone;     
    [SerializeField] private Transform headBone;       

    [Header("VR Locomotion Override")]
    [SerializeField] private MonoBehaviour continuousMoveProvider;

    [Header("Offsets")]
    [SerializeField] private Vector3 walkingOffset = Vector3.zero;
    [SerializeField] private Vector3 eyeOffset = Vector3.zero; 

    [Header("Settings")]
    [SerializeField] private float smoothSpeed = 15f;
    [SerializeField] private bool followRotation = true;

    private Animator knightAnimator; 
    private bool isRiding = false;
    private Vector3 lastPosition;
    [SerializeField] private string speedParameterName = "Speed"; 

    void Start()
    {
        knightAnimator = GetComponentInChildren<Animator>();
        if (knightAnimator != null) knightAnimator.SetBool("IsRiding", false);
        if (xrOrigin == null && headTarget != null) xrOrigin = headTarget.root; 

        // If the slot is empty, search your XR Origin for the movement script
        if (continuousMoveProvider == null && xrOrigin != null)
        {
            continuousMoveProvider = xrOrigin.GetComponentInChildren<ActionBasedContinuousMoveProvider>();
            
            if (continuousMoveProvider == null)
            {
                continuousMoveProvider = xrOrigin.GetComponentInChildren<ContinuousMoveProviderBase>();
            }
        }

        lastPosition = transform.position;
    }

    // Handles RIDING logic in sync with the physics loop to eliminate camera lag/drift
    void FixedUpdate()
    {
        if (headTarget == null || xrOrigin == null || headBone == null) return;

        if (knightAnimator != null) 
        {
            isRiding = knightAnimator.GetBool("IsRiding");
        }

        if (isRiding && saddleAnchor != null && pelvisBone != null)
        {
            if (transform.parent != null) transform.SetParent(null);

            // BACKUP SAFETY CHECK: If locomotion is somehow still running, kill it instantly
            if (continuousMoveProvider != null && continuousMoveProvider.enabled)
            {
                continuousMoveProvider.enabled = false;
            }

            // Sync the Avatar to the Saddle instantly
            Vector3 rootToPelvisOffset = pelvisBone.position - transform.position;
            transform.position = saddleAnchor.position - rootToPelvisOffset;
            transform.rotation = saddleAnchor.rotation;

            // Sync the XR Origin Camera position to the head bone instantly 
            Vector3 headsetLocalOffset = headTarget.position - xrOrigin.position;
            Vector3 targetHeadWorldPos = headBone.position + eyeOffset;
            Vector3 targetOriginPos = targetHeadWorldPos - headsetLocalOffset;
            
            // Hard snap ensures pixel-perfect tracking inside your physical helmet mesh
            xrOrigin.position = targetOriginPos;
            
            // Handle riding rotation alignment inside the physics loop
            if (followRotation)
            {
                Quaternion targetRotation = Quaternion.Euler(0, saddleAnchor.eulerAngles.y, 0);
                xrOrigin.rotation = Quaternion.Slerp(xrOrigin.rotation, targetRotation, Time.fixedDeltaTime * smoothSpeed);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * smoothSpeed);
            }

            if (knightAnimator != null) knightAnimator.SetFloat(speedParameterName, 0f);
        }
    }

    // Handles ON-FOOT logic during the standard rendering update loop
    void LateUpdate()
    {
        if (headTarget == null || xrOrigin == null || headBone == null) return;

        // Skip LateUpdate entirely if we are riding
        if (isRiding) return;

        // ON FOOT POSITIONING LOGIC 
        float floorY = xrOrigin.position.y;
        Vector3 targetPosition = new Vector3(headTarget.position.x, floorY, headTarget.position.z) + walkingOffset;
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);

        CalculateAndApplySpeed();

        // ON FOOT ROTATION HANDLING
        if (followRotation)
        {
            Quaternion targetRotation = Quaternion.Euler(0, headTarget.eulerAngles.y, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smoothSpeed);
        }
    }

    private void CalculateAndApplySpeed()
    {
        if (knightAnimator == null) return;
        Vector3 movementDelta = transform.position - lastPosition;
        movementDelta.y = 0;
        float currentSpeed = movementDelta.magnitude / Time.deltaTime;
        knightAnimator.SetFloat(speedParameterName, currentSpeed);
        lastPosition = transform.position;
    }

    // This handles switching your joystick context smoothly
    public void SetRiding(bool riding)
    {
        isRiding = riding;
        if (knightAnimator != null) knightAnimator.SetBool("IsRiding", riding);

        // Turn off on-foot walking when on the horse, turn it back on when dismounting
        if (continuousMoveProvider != null)
        {
            continuousMoveProvider.enabled = !riding;
        }
    }
}