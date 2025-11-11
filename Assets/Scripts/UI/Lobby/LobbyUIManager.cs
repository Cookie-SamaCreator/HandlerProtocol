using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Steamworks;
using System.Collections.Generic;
using Mirror;
using System.Collections;

public class LobbyUIManager : NetworkBehaviour
{
    private static readonly WaitForSeconds _waitForSeconds1 = new(1f);
    public static LobbyUIManager Instance;
    private const string GAME_SCENE_NAME = "PrototypeArena";

    #region Inspector
    [Header("Main Panel")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private Button buttonHost;
    [SerializeField] private Button buttonJoin;

    [Header("Lobby Panel")]
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private Transform playerListParent;
    [SerializeField] private TMP_Text lobbyStatusText;

    [Header("Lobby Buttons")]
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button leaveButton;

    [Header("NetworkManager")]
    [SerializeField] private MyNetworkManager netManager;
    #endregion

    #region References / State
    [SerializeField] private SteamLobbyController lobbyController;
    #endregion

    #region Public fields
    public List<PlayerEntryUI> playerEntries = new();
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        Debug.Log("[LobbyUIManager] LobbyUI is awake");
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Ensure cursor is usable in menus
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Start()
    {
        Debug.Log("[LobbyUIManager] LobbyUI started");
        if (lobbyController == null)
        {
            Debug.LogError("[LobbyUIManager] No LobbyController found in scene!");
            return;
        }

        // Wire up main menu buttons (check for null to avoid runtime errors)
        if (buttonHost != null) buttonHost.onClick.AddListener(OnHostClicked);
        if (buttonJoin != null) buttonJoin.onClick.AddListener(OnJoinClicked);

        // Wire up lobby buttons
        if (startGameButton != null) startGameButton.onClick.AddListener(OnStartGameClicked);
        if (leaveButton != null) leaveButton.onClick.AddListener(OnLeaveClicked);

        // Default to main menu
        SwapPanel(false);

        // Subscribe to LobbyController events
        lobbyController.OnLobbyJoined += OnLobbyJoined;

        // Refresh the lobby display regularly; the frequency can be tuned as needed
        //InvokeRepeating(nameof(UpdateLobbyDisplay), 1f, 2f);
    }

    private void OnDestroy()
    {
        // Unsubscribe to avoid memory leaks / null reference callbacks
        if (lobbyController != null)
            lobbyController.OnLobbyJoined -= OnLobbyJoined;

        if (buttonHost != null) buttonHost.onClick.RemoveListener(OnHostClicked);
        if (buttonJoin != null) buttonJoin.onClick.RemoveListener(OnJoinClicked);
        if (startGameButton != null) startGameButton.onClick.RemoveListener(OnStartGameClicked);
        if (leaveButton != null) leaveButton.onClick.RemoveListener(OnLeaveClicked);
    }
    #endregion

    public void RegisterPlayer(PlayerEntryUI player)
    {
        player.transform.SetParent(playerListParent, false);
        UpdateLobbyDisplay();
    }
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
        SwapPanel(true);
    }

    private void OnJoinClicked()
    {
        if (lobbyStatusText != null) lobbyStatusText.text = "Joining friend lobby...";
        if (buttonHost != null) buttonHost.interactable = false;
        if (buttonJoin != null) buttonJoin.interactable = false;
        // Steam overlay will trigger the relevant LobbyController callback
    }

    public void OnReadyClicked(bool readyState)
    {
        if (lobbyStatusText != null)
        {
            lobbyStatusText.text = readyState ? "You are ready! Waiting for others..." : "You are not ready.";
        }
    }

    private void OnStartGameClicked()
    {
        lobbyController.StartGame();
        if(NetworkServer.active)
        {
            NetworkManager.singleton.ServerChangeScene(GAME_SCENE_NAME);
        }
    }

    private void OnLeaveClicked()
    {
        lobbyController?.LeaveLobby();
        ResetToMainMenu();
    }
    #endregion

    #region Event Callbacks
    private void OnLobbyJoined()
    {
        Debug.Log("[LobbyUIManager] Joined lobby successfully.");
        SwapPanel(true);

        if (leaveButton != null) leaveButton.gameObject.SetActive(true);

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
    public void UpdateLobbyDisplay()
    {
        if(!lobbyPanel.activeInHierarchy){ return; }
        playerEntries.Clear();

        var lobby = SteamLobbyController.Instance.currentLobbyID;
        int memberCount = SteamMatchmaking.GetNumLobbyMembers(lobby);
        if (lobbyStatusText != null) lobbyStatusText.text = $"{memberCount} player(s) in lobby";

        CSteamID hostID = new(ulong.Parse(SteamMatchmaking.GetLobbyData(lobby, "host")));
        List<CSteamID> orderedMembers = new();

        if (memberCount == 0)
        {
            Debug.LogWarning("[LobbyUIManager] Lobby has no members.. retrying...");
            StartCoroutine(RetryUpdate());
            return;
        }

        orderedMembers.Add(hostID);

        for (int i = 0; i < memberCount; i++)
        {
            CSteamID memberID = SteamMatchmaking.GetLobbyMemberByIndex(lobby, i);
            if (memberID != hostID)
            {
                orderedMembers.Add(memberID);
            }
        }

        int j = 0;
        foreach(var member in orderedMembers)
        {
            if (!playerListParent.GetChild(j).TryGetComponent<PlayerEntryUI>(out var entry))
            {
                Debug.LogError($"Member {j} has no player entry");
            }

            string playerName = SteamFriends.GetFriendPersonaName(member);
            Texture2D avatar = GetSteamAvatar(member);
            string readyValue = SteamMatchmaking.GetLobbyMemberData(lobby, member, "ready");
            bool isReady = readyValue == "1";
            entry.SetData(playerName, avatar, isReady);
            playerEntries.Add(entry);
            j++;

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
    public void ResetToMainMenu()
    {
        // Safely destroy all child GameObjects under playerListParent.
        if (playerListParent != null)
        {
            // Iterate backwards to avoid issues when removing children
            for (int i = playerListParent.childCount - 1; i >= 0; i--)
            {
                var child = playerListParent.GetChild(i);
                if (child == null) continue;

                // Destroy differently depending on edit/play mode
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }

        playerEntries.Clear();

        // Re-enable main menu buttons safely
        if (buttonHost != null) buttonHost.interactable = true;
        if (buttonJoin != null) buttonJoin.interactable = true;

        SwapPanel(false);
    }
    #endregion

    [Server]
    public void CheckAllPlayersReady()
    {
        foreach (var player in playerEntries)
        {
            if (!player.isReady)
            {
                RpcSetPlayButtonInteractable(false);
                return;
            }
        }
        RpcSetPlayButtonInteractable(true);
    }

    [ClientRpc]
    void RpcSetPlayButtonInteractable(bool status)
    {
        startGameButton.interactable = status;
    }
    
    private IEnumerator RetryUpdate()
    {
        yield return _waitForSeconds1;
        UpdateLobbyDisplay();
    }

}
