using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour
{
    [Header("References")]
    public Image healthFill;
    private Health playerHealth;
    public TMP_Text healthText;

    public void BindPlayer(Health pHealth)
    {
        playerHealth = pHealth;

        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += UpdateHealthBar;
            UpdateHealthBar(playerHealth.currentHealth, playerHealth.maxHealth);
        }
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged -= UpdateHealthBar;
    }

    private void UpdateHealthBar(float current, float max)
    {
        if (healthFill != null)
        {
            healthFill.fillAmount = current / max;
        }

        if (healthFill != null)
        {
            healthText.text = $"{Mathf.FloorToInt(current)} / {max}";
        }

    }
}
