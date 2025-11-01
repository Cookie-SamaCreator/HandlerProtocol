using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StaminaUI : MonoBehaviour
{
    [Header("References")]
    public Image staminaFill;
    private Stamina playerStamina;
    public TMP_Text staminaText;

    public void BindPlayer(Stamina pStamina)
    {
        playerStamina = pStamina;

        if (playerStamina != null)
        {
            playerStamina.OnStaminaChanged += UpdateStaminaBar;
            UpdateStaminaBar(playerStamina.currentStamina, playerStamina.maxStamina);
        }
    }

    private void OnDestroy()
    {
        if (playerStamina != null)
            playerStamina.OnStaminaChanged -= UpdateStaminaBar;
    }

    private void UpdateStaminaBar(float current, float max)
    {
        if (staminaFill != null)
        {
            staminaFill.fillAmount = current / max;
        }

        if (staminaText != null)
        {
            staminaText.text = $"{Mathf.FloorToInt(current)} / {max}";
        }

    }
}
