using UnityEngine;

public class PlayerLance : MonoBehaviour
{
    private Rigidbody playerRb;

    void Start()
    {
        // Find the Rigidbody on your main player horse to track your actual galloping velocity
        // Assumes your player horse has a Rigidbody attached up the hierarchy chain
        playerRb = GetComponentInParent<Rigidbody>();
    }

    // A public function the AI shield can call to ask how fast you were moving during the impact
    public float GetStrikeSpeed()
    {
        if (playerRb != null)
        {
            // Returns your exact physical speed in meters per second
            return playerRb.linearVelocity.magnitude; 
        }
        
        // Fallback speed if no Rigidbody is found
        return 5f; 
    }
}