using System.Collections.Generic;
using kcp2k;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class CipherHUDController : MonoBehaviour
{
    [Header("Player Stats")]
    public Image playerIcon;
    public TMP_Text playerName;
    [SerializeField] private StaminaUI staminaUI;
    [SerializeField] private HealthUI healthUI;

    [Header("Team Info")]
    public Transform teamInfoGroup;
    public GameObject teammateInfoPrefab;

    [Header("Crosshair")]
    public GameObject crosshair;

    [Header("Radar")]
    public GameObject radarPlaceholder;

    [Header("Inventory")]
    public Transform inventoryGroup;
    public GameObject weaponSlotPrefab;
    public List<WeaponSlotUI> weaponSlots;
    private CipherController currentCipher;
    private NetworkCipherPlayer currentNetworkCipher;

    private void OnEnable()
    {
        MyNetworkManager.OnAllPlayersSpawned += SetupHUD;
    }

    private void OnDisable()
    {
        MyNetworkManager.OnAllPlayersSpawned -= SetupHUD;
    }

    public void SetupHUD()
    {
        if (currentCipher == null || currentNetworkCipher == null) { return; }

        staminaUI.BindPlayer(currentCipher.staminaSystem);
        healthUI.BindPlayer(currentCipher.healthSystem);
        playerName.text = currentNetworkCipher.playerData.playerName;
        var playerList = GameManager.Instance.connectedPlayers;
        foreach(var player in playerList)
        {
            if (player.Key == currentNetworkCipher.playerData.playerName) { continue; }
            GameObject teammate = Instantiate(teammateInfoPrefab, teamInfoGroup);
            TeamMateInfoUI info = teammate.GetComponent<TeamMateInfoUI>();
            info.BindPlayer(currentCipher, currentNetworkCipher);
        }
    }
    
    public void BindCipher(CipherController cipher, NetworkCipherPlayer networkCipher)
    {
        currentCipher = cipher;
        currentNetworkCipher = networkCipher;
    }
}
