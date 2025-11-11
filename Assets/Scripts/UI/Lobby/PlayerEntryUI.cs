using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;

public class PlayerEntryUI : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnReadyStatusChanged))]
    public bool isReady = false;

    [Header("References")]
    [SerializeField] private RawImage avatarImage;
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private Image readyIndicator;
    [SerializeField] private Button readyButton;
    [SerializeField] private TMP_Text readyButtonText;

    [Header("Ready Colors")]
    public Color readyColor = Color.green;
    public Color notReadyColor = Color.red;

    void Start()
    {
        readyButton.interactable = isLocalPlayer;
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        readyButton.interactable = true;
        isReady = false;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        LobbyUIManager.Instance.RegisterPlayer(this);
    }
    public void SetData(string playerName, Texture2D avatar, bool isReady)
    {
        playerNameText.text = playerName;

        if (avatar != null)
            avatarImage.texture = avatar;

        this.isReady = isReady;
        UpdateReadyStatus();
        var playerData = new PlayerData(playerName, avatar);
        GameManager.Instance.RegisterPlayer(playerData);
    }

    [Command]
    void CmdSetReady()
    {
        isReady = !isReady;
        OnReadyStatusChanged(!isReady, isReady);
    }

    public void OnReadyButtonClicked()
    {
        CmdSetReady();
    }

    private void UpdateReadyStatus()
    {
        Debug.Log($"UpdateReady is Called, ready = {isReady}.");
        readyIndicator.color = isReady ? readyColor : notReadyColor;
        readyButtonText.text = isReady ? "Not Ready" : "Ready";
        ColorBlock cb = readyButton.colors;
        //Inverted colors for button
        Color color = isReady ? notReadyColor : readyColor;
        cb.normalColor = color;
        cb.selectedColor = color;
        cb.disabledColor = color;
        readyButton.colors = cb;
    }

    void OnReadyStatusChanged(bool oldValue, bool newValue)
    {
        if (NetworkServer.active)
        {
            LobbyUIManager.Instance.CheckAllPlayersReady();
        }

        UpdateReadyStatus();
    }
}
