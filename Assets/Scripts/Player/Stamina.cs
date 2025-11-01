using UnityEngine;
using System.Collections;
using TMPro;

public class Stamina : MonoBehaviour
{
    [Header("Base Settings")]
    public float maxStamina = 100f;
    public float currentStamina;

    [Header("Consumption & Regen")]
    public float sprintDrainRate = 15f;   // points per second while sprinting
    public float regenRate = 10f;         // points per second
    public float regenDelay = 1.5f;       // delay before regen starts after sprint

    private bool isDraining = false;
    private Coroutine regenCoroutine;

    public delegate void OnStaminaChangedDelegate(float current, float max);
    public event OnStaminaChangedDelegate OnStaminaChanged;

    private void Awake()
    {
        currentStamina = maxStamina;
    }

    private void Start()
    {
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }

    private void Update()
    {
        // Optional debug
        Debug.DrawRay(transform.position + Vector3.up * 2, Vector3.forward * (currentStamina / maxStamina), Color.green);
    }

    public bool HasStamina(float amount = 1f)
    {
        return currentStamina >= amount;
    }

    public void StartDraining()
    {
        if (!isDraining)
        {
            isDraining = true;
            if (regenCoroutine != null) StopCoroutine(regenCoroutine);
        }
    }

    public void StopDraining()
    {
        if (isDraining)
        {
            isDraining = false;
            regenCoroutine = StartCoroutine(RegenAfterDelay());
        }
    }

    private void FixedUpdate()
    {
        if (isDraining)
        {
            currentStamina -= sprintDrainRate * Time.fixedDeltaTime;
            currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        }
    }

    private IEnumerator RegenAfterDelay()
    {
        yield return new WaitForSeconds(regenDelay);

        while (!isDraining && currentStamina < maxStamina)
        {
            currentStamina += regenRate * Time.deltaTime;
            currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
            yield return null;
        }

        regenCoroutine = null;
    }

    public void ModifyMaxStamina(float amount)
    {
        maxStamina += amount;
        maxStamina = Mathf.Max(1f, maxStamina); // avoid 0 or negative
        currentStamina = Mathf.Min(currentStamina, maxStamina);
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }

    public void ApplyStaminaBuff(float amount, float duration)
    {
        //TODO : Implement this
    }

    public void ModifyRegenRate(float multiplier)
    {
        regenRate *= multiplier;
    }

    public void ModifyDrainRate(float multiplier)
    {
        sprintDrainRate *= multiplier;
    }
}
