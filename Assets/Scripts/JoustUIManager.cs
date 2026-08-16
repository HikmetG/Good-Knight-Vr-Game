using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class JoustUIManager : MonoBehaviour
{
    [Header("Enemy HP Display")]
    [SerializeField] private Slider enemyHpSlider;
    [SerializeField] private TextMeshProUGUI enemyHpText;

    [Header("Enemy Paper Doll Ovals")]
    [SerializeField] private Image enemyHead;
    [SerializeField] private Image enemyTorso;
    [SerializeField] private Image enemyLeftArm;
    [SerializeField] private Image enemyRightArm;
    [SerializeField] private Image enemyLeftLeg;   
    [SerializeField] private Image enemyRightLeg;  

    [Header("Player HP Display")]
    [SerializeField] private Slider playerHpSlider;
    [SerializeField] private TextMeshProUGUI playerHpText;

    [Header("Player Paper Doll Ovals")]
    [SerializeField] private Image playerHead;
    [SerializeField] private Image playerTorso;
    [SerializeField] private Image playerLeftArm;
    [SerializeField] private Image playerRightArm;
    [SerializeField] private Image playerLeftLeg;   
    [SerializeField] private Image playerRightLeg; 

    [Header("Flash Settings")]
    [SerializeField] private Color normalColor = new Color(1, 1, 1, 0.4f);
    [SerializeField] private Color flashColor = Color.red;
    [SerializeField] private float fadeSpeed = 2f;

    public void UpdateDisplay(float currentHP, float maxHP, JoustBodyPart hitPart, bool isPlayerHit)
    {
        if (isPlayerHit)
        {
            if (playerHpSlider != null) playerHpSlider.value = currentHP / maxHP;
            if (playerHpText != null) playerHpText.text = $"YOU: {Mathf.CeilToInt(currentHP)}/{Mathf.CeilToInt(maxHP)} HP";

            Image targetImage = GetImageFromEnum(hitPart, true);
            if (targetImage != null) StartCoroutine(FlashPartRoutine(targetImage));
        }
        else
        {
            if (enemyHpSlider != null) enemyHpSlider.value = currentHP / maxHP;
            if (enemyHpText != null) enemyHpText.text = $"FOE: {Mathf.CeilToInt(currentHP)}/{Mathf.CeilToInt(maxHP)} HP";

            Image targetImage = GetImageFromEnum(hitPart, false);
            if (targetImage != null) StartCoroutine(FlashPartRoutine(targetImage));
        }
    }

    private Image GetImageFromEnum(JoustBodyPart part, bool isPlayer)
    {
        if (isPlayer)
        {
            switch (part)
            {
                case JoustBodyPart.Head: return playerHead;
                case JoustBodyPart.Torso: return playerTorso;
                case JoustBodyPart.LeftArm: return playerLeftArm;
                case JoustBodyPart.RightArm: return playerRightArm;
                case JoustBodyPart.LeftLeg: return playerLeftLeg;   
                case JoustBodyPart.RightLeg: return playerRightLeg; 
                default: return null;
            }
        }
        else
        {
            switch (part)
            {
                case JoustBodyPart.Head: return enemyHead;
                case JoustBodyPart.Torso: return enemyTorso;
                case JoustBodyPart.LeftArm: return enemyLeftArm;
                case JoustBodyPart.RightArm: return enemyRightArm;
                case JoustBodyPart.LeftLeg: return enemyLeftLeg;  
                case JoustBodyPart.RightLeg: return enemyRightLeg; 
                default: return null;
            }
        }
    }

    private IEnumerator FlashPartRoutine(Image imageToFlash)
    {
        imageToFlash.color = flashColor;
        float elapsed = 0f;

        while (imageToFlash.color != normalColor)
        {
            elapsed += Time.deltaTime * fadeSpeed;
            imageToFlash.color = Color.Lerp(flashColor, normalColor, elapsed);
            yield return null;
        }
    }
}