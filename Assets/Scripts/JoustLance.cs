using UnityEngine;

public class JoustLance : MonoBehaviour
{
    [Header("Combat Settings")]
    [SerializeField] private float baseLanceDamage = 30f;
    [SerializeField] private string targetTag = "Enemy";

    private bool hasHitSomethingThisPass = false;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[PHYSICS] Lance tip physically touched: {other.name} | Tag is: {other.tag} TARGET TAG: {targetTag} other.CompareTag(targetTag): {other.CompareTag(targetTag)}, hasHitSomethingThisPass: {hasHitSomethingThisPass}");
        
        if (hasHitSomethingThisPass) return;

        if (other.CompareTag(targetTag))
        {
            JoustShield shield = other.GetComponentInParent<JoustShield>();
            
            if (shield != null)
            {
                shield.BlockAttackAndDrop(); 
                hasHitSomethingThisPass = true; 
                return; 
            }

            JoustHitbox struckHitbox = other.GetComponent<JoustHitbox>();
            if (struckHitbox != null)
            {
                struckHitbox.ForwardDamage(baseLanceDamage);
                hasHitSomethingThisPass = true; 
            }
        }
    }

    public void ResetLanceFlag()
    {
        hasHitSomethingThisPass = false;
    }
}