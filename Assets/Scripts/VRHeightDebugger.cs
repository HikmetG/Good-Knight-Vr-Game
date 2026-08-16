using UnityEngine;

public class VRHeightDebugger : MonoBehaviour
{
    [SerializeField] private Transform headTarget;
    [SerializeField] private Transform xrOrigin;

    void Start()
    {
        // Wait 1 second after loading so the VR tracking has fully initialized
        Invoke(nameof(LogHeights), 1.0f);
    }

    void LogHeights()
    {
        Debug.Log("==== VR HEIGHT DIAGNOSTICS ====");
        
        if (xrOrigin != null)
        {
            Debug.Log($"1. XR Origin Global Y Position: {xrOrigin.position.y} (Should match your world floor)");
        }
        else
        {
            Debug.LogError("1. XR Origin reference is MISSING on the debugger!");
        }

        if (headTarget != null)
        {
            Debug.Log($"2. Head Target (Camera) Global Y Position: {headTarget.position.y}");
            
            if (xrOrigin != null)
            {
                float localHeadHeight = headTarget.position.y - xrOrigin.position.y;
                Debug.Log($"3. Calculated Player Height (Head minus Origin): {localHeadHeight} meters");
                
                if (localHeadHeight < 0.5f)
                {
                    Debug.LogWarning("⚠️ WARNING: Your tracking system thinks your head is practically on the floor. Check your VR room boundaries/calibration!");
                }
            }
        }
        else
        {
            Debug.LogError("2. Head Target reference is MISSING on the debugger!");
        }

        // Check the Knight's current starting position
        Debug.Log($"4. Knight Avatar Global Y Position: {transform.position.y}");
        Debug.Log("==================================");
    }
}