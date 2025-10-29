using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerEntryUI : MonoBehaviour
{
    [Header("References")]
    public RawImage avatarImage;
    public TMP_Text playerNameText;
    public Image readyIndicator;

    [Header("Ready Colors")]
    public Color readyColor = Color.green;
    public Color notReadyColor = Color.red;

    public void SetData(string playerName, Texture2D avatar, bool isReady)
    {
        playerNameText.text = playerName;

        if (avatar != null)
            avatarImage.texture = avatar;

        readyIndicator.color = isReady ? readyColor : notReadyColor;
    }
}
