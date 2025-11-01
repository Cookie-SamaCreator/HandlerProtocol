using UnityEngine;

public class Health : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        var cipherPlayer = GetComponent<NetworkCipherPlayer>();
        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        // placeholder: disable player or respawn
        Debug.Log($"{gameObject.name} died");
        gameObject.SetActive(false);
    }
}
