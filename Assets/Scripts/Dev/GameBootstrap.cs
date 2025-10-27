using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    [Header("Player Prefab")]
    public GameObject cipherPrefab;
    public GameObject hudPrefab;

    [Header("Spawn Point")]
    public Transform spawnPoint;

    private void Start()
    {
        if (cipherPrefab == null || spawnPoint == null || hudPrefab == null)
        {
            Debug.LogError("[GameBootstrap] Missing references in GameBootstrap!");
            return;
        }

        GameObject player = Instantiate(cipherPrefab, spawnPoint.position, spawnPoint.rotation);
        player.name = "LocalPlayer_Cipher";

        if (!player.TryGetComponent<Stamina>(out var playerStamina))
        {
            Debug.LogError("[GameBootstrap] No stamina detected on Player");
        }

        GameObject playerHUD = Instantiate(hudPrefab);
        if (!playerHUD.TryGetComponent<StaminaUI>(out var staminaUI))
        {
            Debug.LogError("[GameBootstrap] No stamina detected on HUD");
        }
        staminaUI.BindPlayer(playerStamina);
    }
}
