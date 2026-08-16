using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables; 

public class JoustHealth : MonoBehaviour
{
    [Header("Identity Setting")]
    [Tooltip("Check this ON for your own Knight avatar. Leave it OFF for the AI Knight.")]
    [SerializeField] private bool isPlayer = false; 

    [Header("Main Health Pool")]
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;

    [Header("Limb Health Pools")]
    [SerializeField] private float maxLimbHealth = 30f;
    private float currentLeftArmHealth;
    private float currentRightArmHealth;

    [Header("Physical Disarm Rigging")]
    [SerializeField] private JoustShield shieldScript; 
    [SerializeField] private Rigidbody lanceRigidbody;  
    [SerializeField] private MonoBehaviour lanceIKScript; 

    [Header("Linked UI Canvas")]
    [SerializeField] private JoustUIManager uiCanvasManager;

    [Header("Armor Impact Audio")]
    [Tooltip("Drag the AudioSource component that will play these impact sounds here.")]
    [SerializeField] private AudioSource hitAudioSource;
    [Tooltip("Drag your 4 different metallic body armor hit sound files into this array.")]
    [SerializeField] private AudioClip[] armorHitClips;

    [Header("Helmet Hit Breathing Audio")]
    [Tooltip("Drag the separate AudioSource component that will play the heavy breathing here.")]
    [SerializeField] private AudioSource breathingAudioSource;
    [Tooltip("Drag your heavy breathing sound file here.")]
    [SerializeField] private AudioClip breathingClip;

    private bool isLeftArmBroken = false;
    private bool isRightArmBroken = false;

    public bool IsDead => currentHealth <= 0;

    void Start()
    {
        currentHealth = maxHealth;
        currentLeftArmHealth = maxLimbHealth;
        currentRightArmHealth = maxLimbHealth;

        if (breathingAudioSource != null)
        {
            breathingAudioSource.loop = false;
            breathingAudioSource.Stop();
        }
    }

    public void TakeDamage(float amount, JoustBodyPart hitPart)
    {
        // If someone has already lost the match, stop processing new damage entirely
        if (JoustGameManager.Instance != null && JoustGameManager.Instance.currentState == JoustState.GameOver) return;
        if (IsDead) return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        // Play a random metallic sound if the Torso or Head gets struck
        if (hitPart == JoustBodyPart.Torso || hitPart == JoustBodyPart.Head)
        {
            PlayRandomArmorHitSound();
        }

        // Trigger heavy breathing inside the helmet if the PLAYER gets hit in the head!
        if (isPlayer && hitPart == JoustBodyPart.Head)
        {
            PlayBreathingSound();
        }

        if (hitPart == JoustBodyPart.LeftArm && !isLeftArmBroken)
        {
            currentLeftArmHealth -= amount;
            if (currentLeftArmHealth <= 0)
            {
                isLeftArmBroken = true;
                if (shieldScript != null)
                {
                    shieldScript.DropFromBrokenArm();
                }
            }
        }
        else if (hitPart == JoustBodyPart.RightArm && !isRightArmBroken)
        {
            currentRightArmHealth -= amount;
            if (currentRightArmHealth <= 0)
            {
                isRightArmBroken = true;
                DropLanceAndProcessMatchEnd();
            }
        }

        // Update the VR holographic Visor Dashboard
        if (uiCanvasManager != null)
        {
            uiCanvasManager.UpdateDisplay(currentHealth, maxHealth, hitPart, isPlayer);
        }

        if (currentHealth <= 0)
        {
            TriggerAbsoluteDeath();
        }
    }

    // Safely selects and plays one of your randomized audio clips
    private void PlayRandomArmorHitSound()
    {
        if (hitAudioSource != null && armorHitClips != null && armorHitClips.Length > 0)
        {
            int randomIndex = Random.Range(0, armorHitClips.Length);
            AudioClip randomClip = armorHitClips[randomIndex];

            if (randomClip != null)
            {
                // PlayOneShot allows multiple clanks to layer organically without muting each other
                hitAudioSource.PlayOneShot(randomClip);
            }
        }
    }

    // Plays the heavy breathing clip exactly once on impact
    private void PlayBreathingSound()
    {
        if (breathingAudioSource != null && breathingClip != null)
        {
            // PlayOneShot fires the audio once without looping or cutting off other sounds
            breathingAudioSource.PlayOneShot(breathingClip);
        }
    }

    private void DropLanceAndProcessMatchEnd()
    {
        if (lanceRigidbody != null)
        {
            // If this is the PLAYER, force the VR hand to instantly drop the lance handle
            if (isPlayer)
            {
                XRGrabInteractable grabInteractable = lanceRigidbody.GetComponent<XRGrabInteractable>();
                if (grabInteractable != null) grabInteractable.enabled = false;
            }

            // Unparent and drop weapon
            lanceRigidbody.transform.parent = null;
            lanceRigidbody.isKinematic = false;
            lanceRigidbody.useGravity = true;
            lanceRigidbody.AddForce(transform.forward * 1f + Vector3.up * 3f, ForceMode.Impulse);
        }

        // Disable AI tracking if applicable
        if (!isPlayer && lanceIKScript != null) 
        {
            lanceIKScript.enabled = false;
        }

        // Instantly notify the referee to halt the game and load the defeat/victory screen
        if (JoustGameManager.Instance != null)
        {
            JoustGameManager.Instance.ProcessKnockout(isPlayer);
        }

        
    }

    private void TriggerAbsoluteDeath()
    {

        // Instantly tell the referee that this knight completely ran out of health
        if (JoustGameManager.Instance != null)
        {
            JoustGameManager.Instance.ProcessKnockout(isPlayer);
        }
    }

    // Public getter to query current health pool
    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    // Public getter to check team identity
    public bool IsPlayer()
    {
        return isPlayer;
    }
}