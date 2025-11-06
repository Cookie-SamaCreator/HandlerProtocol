using UnityEngine;
using Mirror;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class Weapon : NetworkBehaviour
{
    [Header("Definition")]
    public WeaponDefinition definition;

    [Header("Runtime")]
    public Transform firePoint;
    public Transform modelRoot;

    private float nextFireTime = 0f;
    private bool isFiring = false;
    private bool canFire = true;

    private AudioSource audioSource;
    private float FireDelay => 60f / definition.FireRate;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }
    private void Start()
    {
        if (definition == null)
            Debug.LogWarning($"Weapon on {gameObject.name} has no WeaponDefinition!");
    }

    public void SetupFromDefinition(WeaponDefinition def)
    {
        definition = def;

        if(definition.WeaponModelPrefab != null && modelRoot != null)
        {
            foreach (Transform t in modelRoot) Destroy(t.gameObject);

            var model = Instantiate(definition.WeaponModelPrefab, modelRoot);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;

            if (firePoint == null)
            {
                var fp = model.transform.Find("FirePoint");
                if (fp != null) firePoint = fp;
            }
        }
    }

    public void TryFire(bool isHeld, bool isNewPress)
    {
        if (!canFire || definition == null || firePoint == null)
            return;

        switch (definition.FireMode)
        {
            case FireMode.Semi:
                if (isNewPress && Time.time >= nextFireTime)
                {
                    FireOnce();
                }
                break;

            case FireMode.Burst:
                if (isNewPress && !isFiring && Time.time >= nextFireTime)
                    StartCoroutine(FireBurst(definition.BurstCount));
                break;

            case FireMode.FullAuto:
                if (isHeld && Time.time >= nextFireTime)
                    FireOnce();
                break;
        }
    }

    private IEnumerator FireBurst(int count)
    {
        isFiring = true;

        for (int i = 0; i < count; i++)
        {
            FireOnce();
            yield return new WaitForSeconds(FireDelay);
        }

        isFiring = false;
    }

    private void FireOnce()
    {
        nextFireTime = Time.time + FireDelay;

        if (definition.MuzzleFlashPrefab)
        {
            var flash = Instantiate(definition.MuzzleFlashPrefab, firePoint.position, firePoint.rotation);
            Destroy(flash, 0.1f);
        }

        if (isLocalPlayer)
            CmdFireProjectile(firePoint.position, firePoint.forward);
    }

    [Command]
    private void CmdFireProjectile(Vector3 origin, Vector3 direction)
    {
        if (definition.ProjectilePrefab != null)
        {
            var projectile = Instantiate(definition.ProjectilePrefab.gameObject, origin, Quaternion.LookRotation(direction));
            projectile.GetComponent<Projectile>().Initialize(gameObject, definition.Damage);

            NetworkServer.Spawn(projectile);
        }
        else
        {
            // 🔹 Hitscan simplifié si pas de projectile
            if (Physics.Raycast(origin, direction, out RaycastHit hit, 100f))
            {
                if (hit.collider.TryGetComponent(out Health health))
                    health.TakeDamage(definition.Damage, gameObject);

                RpcSpawnHitEffect(hit.point, hit.normal);
            }
        }

        RpcPlayFireEffects();
    }

    [ClientRpc]
    private void RpcPlayFireEffects()
    {
        if (definition.FireSound)
            AudioSource.PlayClipAtPoint(definition.FireSound, firePoint.position);
    }

    [ClientRpc]
    private void RpcSpawnHitEffect(Vector3 point, Vector3 normal)
    {
        if (definition.MuzzleFlashPrefab)
        {
            var impact = Instantiate(definition.MuzzleFlashPrefab, point, Quaternion.LookRotation(normal));
            Destroy(impact, 1f);
        }
    }
}
