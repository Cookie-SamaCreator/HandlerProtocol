using UnityEngine;

public class Health : MonoBehaviour
{
    [Header("Base Settings")]
    public float maxHealth = 100f;
    public float currentHealth;

    public delegate void OnHealthChangedDelegate(float current, float max);
    public event OnHealthChangedDelegate OnHealthChanged;
    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        currentHealth += amount;
        if (currentHealth >= maxHealth)
        {
            currentHealth = maxHealth;
        }
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Die()
    {
        // placeholder: disable player or respawn
        Debug.Log($"{gameObject.name} died");
        gameObject.SetActive(false);
    }

    public void ModifyMaxHealth(float amount)
    {
        maxHealth += amount;
        maxHealth = Mathf.Max(1f, maxHealth); // avoid 0 or negative health
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void ApplyHealthBuff(float amount, float duration)
    {
        //TODO : Implement this
    }
}
