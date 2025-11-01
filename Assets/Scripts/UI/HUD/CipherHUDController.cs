using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class CipherHUDController : MonoBehaviour
{
    [Header("Player Info")]
    public Image playerIcon;
    public TMP_Text playerName;

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

    public void SetupHUD()
    {
        if (currentCipher == null || currentNetworkCipher == null) { return; }
        
        /*TODO : setup instantiating of weapon slots*/
    }
    
    public void BindCipher(CipherController cipher, NetworkCipherPlayer networkCipher)
    {
        currentCipher = cipher;
        currentNetworkCipher = networkCipher;
    }
}
