using System.Collections;
using System.Linq;
using Mirror;
using Mirror.Examples.MultipleMatch;
using Steamworks;
using UnityEngine;

[RequireComponent(typeof(CipherController))]
public class NetworkCipherPlayer : NetworkBehaviour
{
    private static WaitForSeconds _waitForSeconds0_5 = new WaitForSeconds(0.5f);
    #region Serialized Fields
    [Header("Local Player Prefabs")]
    [SerializeField] private GameObject playerCamPrefab;
    [SerializeField] private GameObject hudPrefab;

    [Header("Cipher Model")]
    [SerializeField] private Transform cipherModel;
    #endregion

    #region Cached Components
    [Header("Cipher Controller")]
    [SerializeField] private CipherController cipher;
    #endregion

    #region SyncVars
    [SyncVar] public string CipherID = "CipherOne";
    [SyncVar(hook = nameof(OnLoadoutChanged))] public string LoadoutJson;

    public CipherLoadout CurrentLoadout { get; private set; }
    #endregion

    #region Runtime State
    public PlayerData playerData;
    private GameObject localCamInstance;
    private GameObject localHUDInstance;

    // Cache the Steam persona name used as a key in GameManager.connectedPlayers
    private string cachedPersonaName;
    #endregion

    public override void OnStartLocalPlayer()
    {
        Debug.Log($"[NetworkCipherPlayer] Local player before base: {netIdentity.netId}");

        base.OnStartLocalPlayer();
        Debug.Log($"[NetworkCipherPlayer] Local player after base: {netIdentity.netId}");

        // Ensure we have a reference to the CipherController
        if (cipher == null)
            cipher = GetComponent<CipherController>();

        // Mark this cipher as the local player
        if (cipher != null)
            cipher.isLocalPlayer = true;

        // Activate the camera holder if present (the actual camera prefab is instantiated later)
        if (cipher != null && cipher.cameraHolder != null)
            cipher.cameraHolder.gameObject.SetActive(true);

        // Cache the Steam persona name once to avoid repeated native calls
        cachedPersonaName = SteamFriends.GetPersonaName();

        if (GameManager.Instance == null)
        {
            Debug.LogError("[NetworkCipherPlayer] GameManager.Instance is null - cannot fetch player data.");
            return;
        }

        // Try to get player data; if missing, wait for it with a coroutine
        if (!GameManager.Instance.connectedPlayers.TryGetValue(cachedPersonaName, out var data))
        {
            Debug.LogWarning($"[NetworkCipherPlayer] PlayerData not yet available for {cachedPersonaName}. Delaying HUD init...");
            StartCoroutine(WaitForPlayerData());
            return;
        }

        playerData = data;
        SetupCameraAndHUD();

        Debug.Log("[NetworkCipherPlayer] Getting LocalLoadout");
        var localLoadout = PlayerInventory.Instance.GetLoadoutFor(CipherID);
        Debug.Log($"[NetworkCipherPlayer] Local Loadout = : {localLoadout.CipherID}");
        CmdSetLoadout(JsonUtility.ToJson(localLoadout));
        Debug.Log("[NetworkCipherPlayer] Local Loadout cmd done");

        // Start coroutine to spawn the cipher model after a short delay
        StartCoroutine(WaitAndSpawnCipher());
        
        Debug.Log($"[NetworkCipherPlayer] Local player started: {netIdentity.netId}");
    }
    
    private void OnDestroy()
    {
        // Clean up instantiated local-only objects
        if (isLocalPlayer)
        {
            if (localCamInstance != null)
                Destroy(localCamInstance);
            if (localHUDInstance != null)
                Destroy(localHUDInstance);
        }
    }

    public override void OnStopLocalPlayer()
    {
        cipher.isLocalPlayer = false;
        if (cipher.cameraHolder != null)
        {
            cipher.cameraHolder.gameObject.SetActive(false);
        }
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        if (isLocalPlayer)
        {
            Debug.Log("Local player stopped.");
        }
    }

    void Update()
    {
        // Optional: only local player should process input
        if (!isLocalPlayer) return;

        // Input and per-frame logic are mostly handled inside CipherController
    }

    // Example of a command to fire a weapon (executed on the server)
    [Command]
    public void CmdFire(Vector3 origin, Vector3 direction)
    {
        // serveur peut valider puis appeler un RPC pour spawn effet/damage
        // Server can validate and then call an RPC to spawn effects / apply damage
        RpcDoFire(origin, direction);
    }

    [ClientRpc]
    void RpcDoFire(Vector3 origin, Vector3 direction)
    {
        // Play local VFX (muzzle flash, hit markers) or apply client-side logic.
        // If authoritative server handles damage, that should be done on the server already.
    }

    private void SetupCameraAndHUD()
    {
        Debug.Log("[NetworkCipherPlayer] Instantiating local camera and HUD.");

        // Instantiate and bind camera prefab
        if (playerCamPrefab != null)
        {
            localCamInstance = Instantiate(playerCamPrefab);
            if (localCamInstance.TryGetComponent<CinemachineSprintFX>(out var sprintFX))
            {
                sprintFX.BindCipherPlayer(cipher);
                Debug.Log("[NetworkCipherPlayer] Instantiated local camera.");
            }
            else
            {
                Debug.LogWarning("[NetworkCipherPlayer] Camera prefab missing CinemachineSprintFX component.");
            }
        }

        // Instantiate and bind HUD prefab
        if (hudPrefab != null)
        {
            localHUDInstance = Instantiate(hudPrefab);
            if (localHUDInstance.TryGetComponent<CipherHUDController>(out var cipherHUD))
            {
                cipherHUD.BindCipher(cipher, this);
                Debug.Log("[NetworkCipherPlayer] Instantiated local HUD.");
            }
            else
            {
                Debug.LogWarning("[NetworkCipherPlayer] HUD prefab missing CipherHUDController component.");
            }
        }
    }

    private IEnumerator WaitForPlayerData()
    {
        // Wait until GameManager has registered the player data for this persona
        // Use a small delay per loop iteration to avoid a tight spin loop
        while (GameManager.Instance != null && !GameManager.Instance.connectedPlayers.ContainsKey(cachedPersonaName))
        {
            // If this object was destroyed or no longer local, stop waiting
            if (!this || !isActiveAndEnabled || !isLocalPlayer)
                yield break;

            yield return new WaitForSeconds(0.1f);
        }

        if (GameManager.Instance != null && GameManager.Instance.connectedPlayers.TryGetValue(cachedPersonaName, out var data))
        {
            playerData = data;
            SetupCameraAndHUD();
        }
    }

    [Command]
    private void CmdSetLoadout(string json)
    {
        LoadoutJson = json;
        Debug.Log($"[NetworkCipherPlayer] cmd loadout");
    }

    private void OnLoadoutChanged(string _, string newJson)
    {
        Debug.Log($"[NetworkCipherPlayer] onLoadout changed");
        CurrentLoadout = JsonUtility.FromJson<CipherLoadout>(newJson);
        ApplyLoadout();
    }

    private void ApplyLoadout()
    {
        // instancier armes, compétences, etc. dans CipherController
        Debug.Log("[NetworkCipherPlayer] ApplyLoadout");
        GetComponent<CipherController>().SetupLoadout(CurrentLoadout);
    }

    #region Unity Lifecycle Helpers
    private void Awake()
    {
        // Ensure cipher is cached early to allow other components to bind in Awake/OnEnable
        if (cipher == null)
            cipher = GetComponent<CipherController>();
    }
    #endregion

    /// <summary>
    /// Waits a short delay to ensure loadout is applied, then finds the CipherDefinition
    /// and instantiates its prefab under the provided `cipherModel` parent.
    /// This runs on the client and creates a local visual representation only.
    /// </summary>
    private IEnumerator WaitAndSpawnCipher()
    {
        // Small delay to allow server/client sync of SyncVars and loadout application
        yield return _waitForSeconds0_5;

        // Prefer the server-synced CurrentLoadout if available
        CipherLoadout loadoutToUse = CurrentLoadout;

        // If CurrentLoadout hasn't arrived yet, try to fetch from local inventory
        if (loadoutToUse == null && PlayerInventory.Instance != null)
        {
            loadoutToUse = PlayerInventory.Instance.GetLoadoutFor(CipherID);
            if (loadoutToUse == null)
            {
                // Try to generate or get a loadout from the definition as a fallback
                var def = PlayerInventory.Instance.GetCipherDefinition(CipherID);
                if (def != null)
                    loadoutToUse = PlayerInventory.Instance.GetOrGenerateLoadout(def);
            }
        }

        if (loadoutToUse == null)
        {
            Debug.LogWarning("[NetworkCipherPlayer] No loadout available to spawn cipher model.");
            yield break;
        }

        var cipherDef = PlayerInventory.Instance.GetCipherDefinition(loadoutToUse.CipherID);
        if (cipherDef == null)
        {
            Debug.LogWarning($"[NetworkCipherPlayer] CipherDefinition not found for id '{loadoutToUse.CipherID}'");
            yield break;
        }

        if (cipherDef.CipherPrefab != null && cipherModel != null)
        {
            Instantiate(cipherDef.CipherPrefab, cipherModel);
            Debug.Log($"[NetworkCipherPlayer] Spawned cipher prefab for {cipherDef.CipherID}");
        }
        else
        {
            Debug.LogWarning("[NetworkCipherPlayer] Cipher prefab or model parent is null");
        }
    }
}
