using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Steamworks;
using System.Collections.Generic;
using Mirror;
using System.Reflection;

/// <summary>
/// Manages the lobby UI: main menu, lobby panel and player list display.
/// Grouped into regions and includes small optimizations (cached FieldInfo,
/// safer lifecycle handling) to reduce runtime overhead.
/// </summary>
public class LobbyUIManager : MonoBehaviour
{
    #region Inspector
    [Header("Main Panel")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private Button buttonHost;
    [SerializeField] private Button buttonJoin;

    [Header("Lobby Panel")]
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private Transform playersContainer;
    [SerializeField] private GameObject playerEntryPrefab;
    [SerializeField] private TMP_Text lobbyStatusText;

    [Header("Lobby Buttons")]
    [SerializeField] private Button readyButton;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button leaveButton;

    [Header("NetworkManager")]
    [SerializeField] private MyNetworkManager netManager;
    #endregion

    #region References / State
    private LobbyController lobbyController;

    // Cache the FieldInfo for the private currentLobbyID in LobbyController to avoid
    // doing repeated reflection lookups every update tick.
    private FieldInfo currentLobbyField;

    // Map Steam IDs to UI entries so we can add/remove/update efficiently.
    private Dictionary<CSteamID, PlayerEntryUI> playerEntries = new();
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        // Ensure cursor is usable in menus
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

        // Cache FieldInfo for performance (used in UpdateLobbyDisplay)
        currentLobbyField = typeof(LobbyController).GetField("currentLobbyID",
            BindingFlags.NonPublic | BindingFlags.Instance);

        // Wire up main menu buttons (check for null to avoid runtime errors)
        if (buttonHost != null) buttonHost.onClick.AddListener(OnHostClicked);
        if (buttonJoin != null) buttonJoin.onClick.AddListener(OnJoinClicked);

        // Wire up lobby buttons
        if (readyButton != null) readyButton.onClick.AddListener(OnReadyClicked);
        if (startGameButton != null) startGameButton.onClick.AddListener(OnStartGameClicked);
        if (leaveButton != null) leaveButton.onClick.AddListener(OnLeaveClicked);

        // Default to main menu
        SwapPanel(false);

        // Subscribe to LobbyController events
        lobbyController.OnLobbyJoined += OnLobbyJoined;

        // Refresh the lobby display regularly; the frequency can be tuned as needed
        InvokeRepeating(nameof(UpdateLobbyDisplay), 1f, 2f);
    }

    private void OnDestroy()
    {
        // Unsubscribe to avoid memory leaks / null reference callbacks
        if (lobbyController != null)
            lobbyController.OnLobbyJoined -= OnLobbyJoined;

        if (buttonHost != null) buttonHost.onClick.RemoveListener(OnHostClicked);
        if (buttonJoin != null) buttonJoin.onClick.RemoveListener(OnJoinClicked);
        if (readyButton != null) readyButton.onClick.RemoveListener(OnReadyClicked);
        if (startGameButton != null) startGameButton.onClick.RemoveListener(OnStartGameClicked);
        if (leaveButton != null) leaveButton.onClick.RemoveListener(OnLeaveClicked);
    }
    #endregion

    #region UI Helpers
    /// <summary>
    /// Show either the main panel or the lobby panel.
    /// </summary>
    private void SwapPanel(bool isLobby)
    {
        if (mainPanel != null) mainPanel.SetActive(!isLobby);
        if (lobbyPanel != null) lobbyPanel.SetActive(isLobby);
    }
    #endregion

    #region Button Actions
    private void OnHostClicked()
    {
        if (lobbyStatusText != null) lobbyStatusText.text = "Creating lobby...";
        if (buttonHost != null) buttonHost.interactable = false;
        if (buttonJoin != null) buttonJoin.interactable = false;
        lobbyController?.CreateLobby();
    }

    private void OnJoinClicked()
    {
        if (lobbyStatusText != null) lobbyStatusText.text = "Joining friend lobby...";
        if (buttonHost != null) buttonHost.interactable = false;
        if (buttonJoin != null) buttonJoin.interactable = false;
        // Steam overlay will trigger the relevant LobbyController callback
    }

    private void OnReadyClicked()
    {
        lobbyController?.SetReady(true);
        if (readyButton != null) readyButton.interactable = false;
        if (lobbyStatusText != null) lobbyStatusText.text = "You are ready! Waiting for others...";
    }

    private void OnStartGameClicked()
    {
        lobbyController?.StartGame();
    }

    private void OnLeaveClicked()
    {
        lobbyController?.LeaveCurrentLobby();
        ResetToMainMenu();
    }
    #endregion

    #region Event Callbacks
    private void OnLobbyJoined()
    {
        Debug.Log("[LobbyUIManager] Joined lobby successfully.");
        SwapPanel(true);

        if (leaveButton != null) leaveButton.gameObject.SetActive(true);
        if (readyButton != null) readyButton.gameObject.SetActive(true);

        if (lobbyController != null && lobbyController.IsHost)
        {
            if (startGameButton != null) startGameButton.gameObject.SetActive(true);
            if (lobbyStatusText != null) lobbyStatusText.text = "Lobby created. Waiting for players...";
        }
        else
        {
            if (startGameButton != null) startGameButton.gameObject.SetActive(false);
            if (lobbyStatusText != null) lobbyStatusText.text = "Joined lobby. Waiting for host...";
        }

        UpdateLobbyDisplay();
    }
    #endregion

    #region Display Update
    /// <summary>
    /// Updates the player list UI from Steam matchmaking. Uses the cached FieldInfo to
    /// read the current lobby ID from LobbyController without repeated reflection lookups.
    /// </summary>
    private void UpdateLobbyDisplay()
    {
        if (!SteamManager.Initialized) return;
        if (lobbyController == null) return;

        if (currentLobbyField == null) return; // can't read lobby id

        CSteamID lobbyID = (CSteamID)currentLobbyField.GetValue(lobbyController);
        if (lobbyID == CSteamID.Nil) return;

        int memberCount = SteamMatchmaking.GetNumLobbyMembers(lobbyID);
        if (lobbyStatusText != null) lobbyStatusText.text = $"{memberCount} player(s) in lobby";

        // Remove entries for players who left
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
                if (playerEntries.TryGetValue(id, out var removedEntry))
                {
                    Destroy(removedEntry.gameObject);
                    playerEntries.Remove(id);
                }
            }
        }

        // Add or update players
        for (int i = 0; i < memberCount; i++)
        {
            CSteamID memberId = SteamMatchmaking.GetLobbyMemberByIndex(lobbyID, i);
            string name = SteamFriends.GetFriendPersonaName(memberId);
            string readyValue = SteamMatchmaking.GetLobbyMemberData(lobbyID, memberId, "ready");
            bool isReady = readyValue == "1";

            Texture2D avatar = GetSteamAvatar(memberId);

            if (!playerEntries.ContainsKey(memberId))
            {
                var entryObj = Instantiate(playerEntryPrefab, playersContainer);
                var ui = entryObj.GetComponent<PlayerEntryUI>();
                playerEntries.Add(memberId, ui);
            }

            playerEntries[memberId].SetData(name, avatar, isReady);

            // Register player data with game manager if available
            if (GameManager.Instance != null)
                GameManager.Instance.RegisterPlayer(new PlayerData(name, avatar));
        }

        if (netManager != null)
            netManager.expectedPlayers = memberCount;
    }

    /// <summary>
    /// Retrieve a Steam avatar as a Unity Texture2D. Returns null if not available.
    /// </summary>
    private Texture2D GetSteamAvatar(CSteamID steamID)
    {
        int imageID = SteamFriends.GetLargeFriendAvatar(steamID);
        if (imageID == -1) return null;

        if (SteamUtils.GetImageSize(imageID, out uint width, out uint height))
        {
            byte[] image = new byte[width * height * 4];
            if (SteamUtils.GetImageRGBA(imageID, image, (int)(width * height * 4)))
            {
                Texture2D texture = new((int)width, (int)height, TextureFormat.RGBA32, false);
                texture.LoadRawTextureData(image);
                texture.Apply();
                return texture;
            }
        }
        return null;
    }
    #endregion

    #region Reset / Cleanup
    private void ResetToMainMenu()
    {
        // Destroy existing UI entries and clear dictionary
        foreach (var entry in playerEntries.Values)
            Destroy(entry.gameObject);
        playerEntries.Clear();

        // Re-enable main menu buttons safely
        if (buttonHost != null) buttonHost.interactable = true;
        if (buttonJoin != null) buttonJoin.interactable = true;
        if (readyButton != null) readyButton.interactable = true;

        SwapPanel(false);
    }
    #endregion
}
