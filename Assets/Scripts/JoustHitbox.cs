using UnityEngine;

// The list of all possible tracking zones
public enum JoustBodyPart { Head, Torso, LeftArm, RightArm, LeftLeg, RightLeg }

public class JoustHitbox : MonoBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private float damageMultiplier = 1f;
    [SerializeField] private JoustBodyPart bodyPartType; 

    [Header("Linked Health System")]
    [SerializeField] private JoustHealth mainHealthSystem;

    void Start()
    {
        if (mainHealthSystem == null)
            mainHealthSystem = GetComponentInParent<JoustHealth>();
    }

    public void ForwardDamage(float baseDamage)
    {
        if (mainHealthSystem != null)
        {
            float finalDamage = baseDamage * damageMultiplier;
            
            mainHealthSystem.TakeDamage(finalDamage, bodyPartType);
            
            Debug.Log($"[HITBOX] Hit {bodyPartType}! Final Damage: {finalDamage}");
        }
    }
}