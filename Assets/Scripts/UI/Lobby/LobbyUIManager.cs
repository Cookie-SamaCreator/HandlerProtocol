using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Steamworks;
using System.Collections.Generic;

public class LobbyUIManager : MonoBehaviour
{
    [Header("Main Panel")]
    public GameObject mainPanel;
    public Button buttonHost;
    public Button buttonJoin;

    [Header("Lobby Panel")]
    public GameObject lobbyPanel;
    public Transform playersContainer;
    public GameObject playerEntryPrefab;
    public TMP_Text lobbyStatusText;

    public Button readyButton;
    public Button startGameButton;
    public Button leaveButton;

    private LobbyController lobbyController;
    private Dictionary<CSteamID, PlayerEntryUI> playerEntries = new();

    private void Awake()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Start()
    {
        lobbyController = FindFirstObjectByType<LobbyController>();
        if (lobbyController == null)
        {
            Debug.LogError("[LobbyUIManager] No LobbyController found in scene!");
            return;
        }

        // Liaison des boutons du menu principal
        buttonHost.onClick.AddListener(OnHostClicked);
        buttonJoin.onClick.AddListener(OnJoinClicked);

        // Liaison des boutons du lobby
        readyButton.onClick.AddListener(OnReadyClicked);
        startGameButton.onClick.AddListener(OnStartGameClicked);
        leaveButton.onClick.AddListener(OnLeaveClicked);

        // Par défaut on affiche le menu principal
        SwapPanel(false);

        // Abonnement aux événements du LobbyController
        lobbyController.OnLobbyJoined += OnLobbyJoined;

        // Rafraîchissement de la liste toutes les 2 secondes
        InvokeRepeating(nameof(UpdateLobbyDisplay), 1f, 2f);
    }

    private void OnDestroy()
    {
        if (lobbyController != null)
            lobbyController.OnLobbyJoined -= OnLobbyJoined;
    }

    /// <summary>
    /// Active/désactive les panneaux
    /// </summary>
    private void SwapPanel(bool isLobby)
    {
        mainPanel.SetActive(!isLobby);
        lobbyPanel.SetActive(isLobby);
    }

    // =============================
    //       BUTTON ACTIONS
    // =============================

    private void OnHostClicked()
    {
        lobbyStatusText.text = "Creating lobby...";
        buttonHost.interactable = false;
        buttonJoin.interactable = false;
        lobbyController.CreateLobby();
    }

    private void OnJoinClicked()
    {
        lobbyStatusText.text = "Joining friend lobby...";
        buttonHost.interactable = false;
        buttonJoin.interactable = false;
        // L’invite Steam déclenchera OnGameLobbyJoinRequested dans LobbyController
    }

    private void OnReadyClicked()
    {
        lobbyController.SetReady(true);
        readyButton.interactable = false;
        lobbyStatusText.text = "You are ready! Waiting for others...";
    }

    private void OnStartGameClicked()
    {
        lobbyController.StartGame();
    }

    private void OnLeaveClicked()
    {
        lobbyController.LeaveCurrentLobby();
        ResetToMainMenu();
    }

    // =============================
    //       EVENT CALLBACKS
    // =============================

    private void OnLobbyJoined()
    {
        Debug.Log("[LobbyUIManager] Joined lobby successfully.");
        SwapPanel(true);

        leaveButton.gameObject.SetActive(true);
        readyButton.gameObject.SetActive(true);

        if (lobbyController.IsHost)
        {
            startGameButton.gameObject.SetActive(true);
            lobbyStatusText.text = "Lobby created. Waiting for players...";
        }
        else
        {
            startGameButton.gameObject.SetActive(false);
            lobbyStatusText.text = "Joined lobby. Waiting for host...";
        }

        UpdateLobbyDisplay();
    }

    // =============================
    //       DISPLAY UPDATE
    // =============================

    private void UpdateLobbyDisplay()
    {
        if (!SteamManager.Initialized) return;

        var field = typeof(LobbyController).GetField("currentLobbyID",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field == null) return;

        CSteamID lobbyID = (CSteamID)field.GetValue(lobbyController);
        if (lobbyID == CSteamID.Nil) return;

        int memberCount = SteamMatchmaking.GetNumLobbyMembers(lobbyID);
        lobbyStatusText.text = $"{memberCount} player(s) in lobby";

        // Supprime les entrées des joueurs qui ont quitté
        foreach (var id in new List<CSteamID>(playerEntries.Keys))
        {
            bool stillInLobby = false;
            for (int i = 0; i < memberCount; i++)
            {
                if (SteamMatchmaking.GetLobbyMemberByIndex(lobbyID, i) == id)
                {
                    stillInLobby = true;
                    break;
                }
            }
            if (!stillInLobby)
            {
                Destroy(playerEntries[id].gameObject);
                playerEntries.Remove(id);
            }
        }

        // Ajoute ou met à jour les joueurs
        for (int i = 0; i < memberCount; i++)
        {
            CSteamID memberId = SteamMatchmaking.GetLobbyMemberByIndex(lobbyID, i);
            string name = SteamFriends.GetFriendPersonaName(memberId);
            string readyValue = SteamMatchmaking.GetLobbyMemberData(lobbyID, memberId, "ready");
            bool isReady = readyValue == "1";

            Texture2D avatar = GetSteamAvatar(memberId);

            if (!playerEntries.ContainsKey(memberId))
            {
                GameObject entry = Instantiate(playerEntryPrefab, playersContainer);
                PlayerEntryUI ui = entry.GetComponent<PlayerEntryUI>();
                playerEntries.Add(memberId, ui);
            }

            playerEntries[memberId].SetData(name, avatar, isReady);
        }
    }

    private Texture2D GetSteamAvatar(CSteamID steamID)
    {
        int imageID = SteamFriends.GetLargeFriendAvatar(steamID);
        if (imageID == -1) return null;

        if (SteamUtils.GetImageSize(imageID, out uint width, out uint height))
        {
            byte[] image = new byte[width * height * 4];
            if (SteamUtils.GetImageRGBA(imageID, image, (int)(width * height * 4)))
            {
                Texture2D texture = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false);
                texture.LoadRawTextureData(image);
                texture.Apply();
                return texture;
            }
        }
        return null;
    }

    // =============================
    //       RESET / CLEANUP
    // =============================

    private void ResetToMainMenu()
    {
        // Supprime les entrées UI existantes
        foreach (var entry in playerEntries.Values)
            Destroy(entry.gameObject);
        playerEntries.Clear();

        // Réactive le main menu
        buttonHost.interactable = true;
        buttonJoin.interactable = true;
        readyButton.interactable = true;

        SwapPanel(false);
    }
}
