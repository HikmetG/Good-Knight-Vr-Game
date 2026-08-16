using UnityEngine;

public class AILanceCombatIK : MonoBehaviour
{
    [Header("Tracking Targets")]
    [SerializeField] private Transform playerTarget;      
    [SerializeField] private Transform rightHandEffector; 
    [SerializeField] private Transform rightHandBone;     

    [Header("Lance Aim Settings")]
    [SerializeField] private float aimSpeed = 5f;
    [SerializeField] private Vector3 lanceAimOffset = new Vector3(0, -0.3f, 0); 

    [Header("Predictive Aiming")]
    [SerializeField] private float leadTime = 0.15f;

    [Header("Gallop Bobbing (Wave Settings)")]
    [SerializeField] private float waveAmplitude = 0.15f;
    [SerializeField] private float waveFrequency = 14f; 

    private Vector3 lastPlayerPosition;
    private JoustingAI aiController;

    void Start()
    {
        aiController = GetComponentInParent<JoustingAI>();

        if (playerTarget != null)
        {
            lastPlayerPosition = playerTarget.position;
        }
    }

    void LateUpdate()
    {
        if (rightHandEffector == null || rightHandBone == null) return;

        // Keep the IK effector anchored to the hand bone position
        rightHandEffector.position = rightHandBone.position;

        // Only track the player if the AI is Charging or Lowering its Lance
        bool isCurrentlyJousting = aiController != null && 
            (aiController.GetComponent<JoustingAI>().enabled && 
            (GetCurrentAIState() == "Charging" || GetCurrentAIState() == "LoweringLance"));

        if (isCurrentlyJousting && playerTarget != null)
        {
            // ACTIVE TRACKING MODE
            
            // Calculate the Player's real-time velocity vector
            Vector3 playerVelocity = (playerTarget.position - lastPlayerPosition) / Time.deltaTime;
            lastPlayerPosition = playerTarget.position;

            // Generate horse bouncing motion
            float verticalWave = Mathf.Sin(Time.time * waveFrequency) * waveAmplitude;
            Vector3 gallopWaveOffset = new Vector3(0, verticalWave, 0);

            Vector3 predictedChestPosition = playerTarget.position + (playerVelocity * leadTime) + lanceAimOffset + gallopWaveOffset;

            Vector3 targetDirection = (predictedChestPosition - rightHandBone.position).normalized;
            if (targetDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
                rightHandEffector.rotation = Quaternion.Slerp(rightHandEffector.rotation, targetRotation, Time.deltaTime * aimSpeed);
            }
        }
        else
        {
            // REST/PARKING MODE
            Quaternion restRotation = transform.parent.rotation; 
            rightHandEffector.rotation = Quaternion.Slerp(rightHandEffector.rotation, restRotation, Time.deltaTime * 3f);
            
            if (playerTarget != null) lastPlayerPosition = playerTarget.position;
        }
    }

    private string GetCurrentAIState()
    {
        System.Object state = aiController.GetType().GetField("currentState", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(aiController);
        return state.ToString();
    }
}