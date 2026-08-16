using UnityEngine;
using System.Collections.Generic;

public class JoustReadyZone : MonoBehaviour
{
    public enum GateType { ForwardGate, ReturnGate }

    [Header("Gate Configuration")]
    [SerializeField] private GateType gateType = GateType.ForwardGate;
    [SerializeField] private GameObject zoneVisual; 

    [Header("Target Tracking")]
    [SerializeField] private string playerTag = "Player";

    // Track all individual active colliders currently inside this zone
    private readonly HashSet<Collider> activeCollidersInZone = new HashSet<Collider>();

    public GateType GetGateType() => gateType;

    public void SetHighlightActive(bool activate)
    {
        if (zoneVisual != null)
        {
            zoneVisual.SetActive(activate);
        }
        
        if (!activate)
        {
            activeCollidersInZone.Clear();
        }
    }

    private void OnDisable()
    {
        activeCollidersInZone.Clear();
    }

    // Helper method to strictly verify that a collider belongs to the Player
    private bool IsActualPlayer(Collider other)
    {
        if (other.CompareTag(playerTag)) return true;

        Rigidbody parentRB = other.GetComponentInParent<Rigidbody>();
        if (parentRB != null && parentRB.CompareTag(playerTag)) return true;

        CharacterController parentCC = other.GetComponentInParent<CharacterController>();
        if (parentCC != null && parentCC.CompareTag(playerTag)) return true;

        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (JoustGameManager.Instance == null) return;

        int currentRound = JoustGameManager.Instance.currentRound;
        bool isOddRound = (currentRound % 2 != 0); 
        bool isCorrectGate = (gateType == GateType.ForwardGate && isOddRound) || 
                             (gateType == GateType.ReturnGate && !isOddRound);

        if (isCorrectGate && IsActualPlayer(other))
        {
            if (!activeCollidersInZone.Contains(other))
            {
                activeCollidersInZone.Add(other);
            }

            JoustGameManager.Instance.playerIsAtGate = true;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (JoustGameManager.Instance == null) return;

        int currentRound = JoustGameManager.Instance.currentRound;
        bool isOddRound = (currentRound % 2 != 0); 
        bool isCorrectGate = (gateType == GateType.ForwardGate && isOddRound) || 
                             (gateType == GateType.ReturnGate && !isOddRound);

        if (isCorrectGate && IsActualPlayer(other))
        {
            if (!activeCollidersInZone.Contains(other))
            {
                activeCollidersInZone.Add(other);
            }

            JoustGameManager.Instance.playerIsAtGate = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (JoustGameManager.Instance == null) return;

        int currentRound = JoustGameManager.Instance.currentRound;
        bool isOddRound = (currentRound % 2 != 0);
        bool isCorrectGate = (gateType == GateType.ForwardGate && isOddRound) || 
                             (gateType == GateType.ReturnGate && !isOddRound);

        if (isCorrectGate && IsActualPlayer(other))
        {
            if (activeCollidersInZone.Contains(other))
            {
                activeCollidersInZone.Remove(other);
            }

            if (activeCollidersInZone.Count == 0)
            {
                JoustGameManager.Instance.playerIsAtGate = false;
            }
        }
    }
}