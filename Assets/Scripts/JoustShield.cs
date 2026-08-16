using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;             
using UnityEngine.XR.Interaction.Toolkit.Interactables; 

public class JoustShield : MonoBehaviour
{
    [Header("Shield Components")]
    [SerializeField] private Rigidbody shieldRigidbody;
    [SerializeField] private AudioSource blockSound; 

    private bool isDropped = false;

    void Start()
    {
        if (shieldRigidbody == null)
            shieldRigidbody = GetComponent<Rigidbody>();
    }

    public void BlockAttackAndDrop()
    {
        if (isDropped) return;
        
        Vector3 explosionForce = -transform.forward * 4f + Vector3.up * 3f + transform.right * 1f;
        ExecutePhysicalDrop(explosionForce, 15f);
    }

    public void DropFromBrokenArm()
    {
        if (isDropped) return;
        
        Vector3 fallForce = -transform.forward * 0.5f + Vector3.up * 0.5f;
        ExecutePhysicalDrop(fallForce, 2f);
    }

    private void ExecutePhysicalDrop(Vector3 force, float torqueValue)
    {
        isDropped = true;
        if (blockSound != null) blockSound.Play();

        // Turns off the XR Grab script component to instantly break your hand's grip
        XRGrabInteractable grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable != null)
        {
            grabInteractable.enabled = false; 
        }

        if (shieldRigidbody != null)
        {
            transform.parent = null; 
            shieldRigidbody.isKinematic = false;
            shieldRigidbody.useGravity = true;

            shieldRigidbody.AddForce(force, ForceMode.Impulse);
            shieldRigidbody.AddTorque(Random.insideUnitSphere * torqueValue, ForceMode.Impulse);
        }
    }
}