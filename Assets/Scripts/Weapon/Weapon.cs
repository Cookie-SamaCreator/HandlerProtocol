using UnityEngine;
using Mirror;
using System.Collections;

/// <summary>
/// Network-enabled weapon system that handles firing, ammo, and effects.
/// Supports Semi, Burst, and FullAuto fire modes.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class Weapon : NetworkBehaviour
{
    #region Inspector Fields
    [Header("Weapon Definition")]
    [SerializeField, Tooltip("Weapon configuration asset")]
    private WeaponDefinition definition;

    [Header("Transform References")]
    [SerializeField, Tooltip("Where bullets spawn from")]
    private Transform firePoint;
    [SerializeField, Tooltip("Root transform for weapon model")]
    private Transform modelRoot;
    #endregion

    #region Private State
    private AudioSource audioSource;
    private float nextFireTime = 0f;
    private bool isFiring = false;
    private bool canFire = true;
    #endregion

    #region Properties
    /// <summary>
    /// Delay between shots in seconds (calculated from FireRate in definition)
    /// </summary>
    private float FireDelay
    {
        get
        {
            if (definition == null)
            {
                Debug.LogWarning("[Weapon] FireDelay requested but definition is null!");
                return 0.1f; // Safe fallback
            }
            return 60f / definition.FireRate;
        }
    }

    /// <summary>
    /// Check if this weapon's owning player is local
    /// </summary>
    private bool IsLocalPlayerWeapon => isLocalPlayer;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        // Cache audio source for fire effects
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogError("[Weapon] AudioSource component required but not found!");
        }
    }

    private void Start()
    {
        // Validate that weapon has been configured
        if (definition == null)
        {
            Debug.LogWarning($"[Weapon] {gameObject.name} has no WeaponDefinition assigned!");
        }
    }
    #endregion

    #region Setup
    /// <summary>
    /// Initialize the weapon with a definition and instantiate its model
    /// </summary>
    public void SetupFromDefinition(WeaponDefinition def)
    {
        if (def == null)
        {
            Debug.LogError("[Weapon] Cannot setup weapon with null definition!");
            return;
        }

        definition = def;
        Debug.Log($"[Weapon] Setting up {def.name} on {gameObject.name}");

        // Instantiate the weapon model if we have all required references
        if (definition.WeaponModelPrefab != null && modelRoot != null)
        {
            // Clean up any existing model children
            foreach (Transform t in modelRoot)
                Destroy(t.gameObject);

            // Instantiate new model
            var model = Instantiate(definition.WeaponModelPrefab, modelRoot);
            model.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            // Try to auto-find the fire point if not set
            if (firePoint == null)
            {
                var fp = model.transform.Find("FirePoint");
                if (fp != null)
                {
                    firePoint = fp;
                    Debug.Log("[Weapon] Auto-found FirePoint in model");
                }
                else
                {
                    Debug.LogWarning("[Weapon] FirePoint not found in model - auto-detection failed");
                }
            }

            Debug.Log($"[Weapon] Model instantiated for {definition.name}");
        }
        else
        {
            if (definition.WeaponModelPrefab == null)
                Debug.LogWarning("[Weapon] WeaponDefinition has no model prefab");
            if (modelRoot == null)
                Debug.LogWarning("[Weapon] No modelRoot transform assigned");
        }
    }
    #endregion

    #region Firing System
    /// <summary>
    /// Main firing interface - call this from input handlers
    /// </summary>
    public void TryFire(bool isHeld, bool isNewPress)
    {
        Debug.Log("TryFire");
        // Early exit if conditions aren't met
        if (!CanFire())
            return;

        // Fire based on current fire mode
        switch (definition.FireMode)
        {
            case FireMode.Semi:
                HandleSemiAuto(isNewPress);
                break;

            case FireMode.Burst:
                HandleBurst(isNewPress);
                break;

            case FireMode.FullAuto:
                HandleFullAuto(isHeld);
                break;

            default:
                Debug.LogWarning($"[Weapon] Unknown fire mode: {definition.FireMode}");
                break;
        }
    }

    /// <summary>
    /// Check if weapon is in a valid state to fire
    /// </summary>
    private bool CanFire()
    {
        if (!canFire)
        {
            Debug.LogWarning("[Weapon] Weapon is disabled from firing");
            return false;
        }

        if (definition == null)
        {
            Debug.LogError("[Weapon] Cannot fire: no definition assigned");
            return false;
        }

        if (firePoint == null)
        {
            Debug.LogError("[Weapon] Cannot fire: no fire point configured");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Handle semi-automatic fire (one shot per trigger press)
    /// </summary>
    private void HandleSemiAuto(bool isNewPress)
    {
        if (isNewPress && Time.time >= nextFireTime)
        {
            FireOnce();
        }
    }

    /// <summary>
    /// Handle burst fire (fixed number of shots per trigger press)
    /// </summary>
    private void HandleBurst(bool isNewPress)
    {
        if (isNewPress && !isFiring && Time.time >= nextFireTime)
        {
            StartCoroutine(FireBurst(definition.BurstCount));
        }
    }

    /// <summary>
    /// Handle full automatic fire (continuous while held)
    /// </summary>
    private void HandleFullAuto(bool isHeld)
    {
        if (isHeld && Time.time >= nextFireTime)
        {
            FireOnce();
        }
    }

    /// <summary>
    /// Coroutine for burst firing - fires multiple shots with delay between
    /// </summary>
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

    /// <summary>
    /// Execute a single shot - handle local effects and network firing
    /// </summary>
    private void FireOnce()
    {
        // Update fire rate limiter
        nextFireTime = Time.time + FireDelay;

        // Spawn muzzle flash locally (immediate visual feedback)
        if (definition.MuzzleFlashPrefab != null)
        {
            var flash = Instantiate(definition.MuzzleFlashPrefab, firePoint.position, firePoint.rotation);
            Destroy(flash, 0.1f);
        }

        // Only fire projectiles if this is the local player's weapon
        if (IsLocalPlayerWeapon)
        {
            CmdFireProjectile(firePoint.position, firePoint.forward);
        }
    }
    #endregion

    #region Network Commands & RPCs
    /// <summary>
    /// Server-side command to spawn projectile/hitscan damage
    /// </summary>
    [Command]
    private void CmdFireProjectile(Vector3 origin, Vector3 direction)
    {
        if (definition.ProjectilePrefab != null)
        {
            // Projectile-based firing
            SpawnProjectile(origin, direction);
        }
        else
        {
            // Hitscan-based firing (raycast)
            HandleHitscan(origin, direction);
        }

        // Notify all clients to play fire effects
        RpcPlayFireEffects();
    }

    /// <summary>
    /// Spawn a projectile on the server and network it
    /// </summary>
    private void SpawnProjectile(Vector3 origin, Vector3 direction)
    {
        var projectile = Instantiate(
            definition.ProjectilePrefab.gameObject,
            origin,
            Quaternion.LookRotation(direction)
        );

        if (projectile.TryGetComponent<Projectile>(out var projectileComponent))
        {
            projectileComponent.Initialize(gameObject, definition.Damage);
            NetworkServer.Spawn(projectile);
        }
        else
        {
            Debug.LogError("[Weapon] Projectile prefab missing Projectile component!");
            Destroy(projectile);
        }
    }

    /// <summary>
    /// Handle hitscan (raycast) damage
    /// </summary>
    private void HandleHitscan(Vector3 origin, Vector3 direction)
    {
        if (Physics.Raycast(origin, direction, out RaycastHit hit, 100f))
        {
            // Apply damage if target has Health component
            if (hit.collider.TryGetComponent(out Health health))
            {
                health.TakeDamage(definition.Damage, gameObject);
            }

            // Spawn impact effect on all clients
            RpcSpawnHitEffect(hit.point, hit.normal);
        }
    }

    /// <summary>
    /// Play fire sound and effects on all clients
    /// </summary>
    [ClientRpc]
    private void RpcPlayFireEffects()
    {
        if (definition.FireSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(definition.FireSound);
        }
    }

    /// <summary>
    /// Spawn impact effect at hit location on all clients
    /// </summary>
    [ClientRpc]
    private void RpcSpawnHitEffect(Vector3 point, Vector3 normal)
    {
        if (definition.MuzzleFlashPrefab != null)
        {
            var impact = Instantiate(
                definition.MuzzleFlashPrefab,
                point,
                Quaternion.LookRotation(normal)
            );
            Destroy(impact, 1f);
        }
    }
    #endregion

    #region Public Interface
    /// <summary>
    /// Enable or disable firing for this weapon
    /// </summary>
    public void SetCanFire(bool can)
    {
        canFire = can;
    }

    /// <summary>
    /// Reset fire cooldown immediately
    /// </summary>
    public void ResetFireTimer()
    {
        nextFireTime = 0f;
    }
    #endregion
}
