using UnityEngine;
using UnityEngine.InputSystem;

public enum FireMode { SemiAuto, FullAuto }

public class Weapon : MonoBehaviour
{
    [Header("Weapon Settings")]
    public FireMode fireMode = FireMode.SemiAuto;
    public float range = 100f;
    public float damage = 25f;
    public float fireRate = 0.2f;
    public Transform firePoint;
    public LayerMask hitMask;

    [Header("FX")]
    public GameObject muzzleFlashPrefab;
    public GameObject hitEffectPrefab;

    private float nextFireTime = 0f;
    private bool canFire = true;

    public void TryFire()
    {
        if (fireMode == FireMode.SemiAuto)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame && Time.time >= nextFireTime)
            {
                Fire();
            }
        }
        else if (fireMode == FireMode.FullAuto)
        {
            if (Time.time >= nextFireTime)
            {
                Fire();
            }
        }
    }

    private void Fire()
    {
        nextFireTime = Time.time + fireRate;

        if (firePoint == null)
        {
            Debug.LogWarning("No fire point assigned!");
            return;
        }

        // FX - muzzle flash
        if (muzzleFlashPrefab)
        {
            GameObject flash = Instantiate(muzzleFlashPrefab, firePoint.position, firePoint.rotation);
            Destroy(flash, 0.1f);
        }

        // Raycast
        Vector3 origin = firePoint.position;
        Vector3 direction = firePoint.forward;

        bool hitSomething = Physics.Raycast(origin, direction, out RaycastHit hit, range, hitMask);
        Color rayColor = hitSomething ? Color.red : Color.magenta;
        // Always draw a debug ray
        if (hitSomething)
        {
            Debug.DrawLine(origin, hit.point, rayColor, 0.25f);
        }
        else
        {
            Debug.DrawLine(origin, origin + direction * range, rayColor, 0.25f);
        }

        // Apply damage only if we hit something with Health
        if (hitSomething)
        {
            var health = hit.collider.GetComponentInParent<Health>();
            if (health != null)
                health.TakeDamage(damage);

            if (hitEffectPrefab)
            {
                GameObject impact = Instantiate(hitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(impact, 1f);
            }
        }
    }
}
