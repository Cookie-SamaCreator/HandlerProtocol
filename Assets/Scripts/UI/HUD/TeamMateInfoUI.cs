using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TeamMateInfoUI : MonoBehaviour
{
    [SerializeField] private Image playerIcon;
    [SerializeField] private TMP_Text playerName;
    [SerializeField] private HealthUI playerHealth;

    public void BindPlayer(CipherController cipher, NetworkCipherPlayer networkCipher)
    {
        playerName.text = networkCipher.playerData.playerName;
        playerHealth.BindPlayer(cipher.health);
        //TODO : Implement player icon   
    }
}
