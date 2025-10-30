using UnityEngine;
using Steamworks;
using Mirror;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Manages Steam lobby creation, joining, and player state synchronization
/// </summary>
public class LobbyController : MonoBehaviour
{
    #region Constants
    private const string READY_KEY = "ready";
    private const string STATE_KEY = "state";
    private const string HOST_KEY = "host";
    private const string GAME_VERSION_KEY = "gameVersion";
    private const string GAME_SCENE_NAME = "PrototypeArena";
    private const string READY_TRUE = "1";
    private const string READY_FALSE = "0";
    private const string LOBBY_STATE_STARTING = "starting";
    private const string LOBBY_STATE_LOBBY = "lobby";
    #endregion

    #region Steam Callbacks
    // Callbacks must be stored on instance fields to stay alive
    private Callback<LobbyCreated_t> lobbyCreatedCallback;
    private Callback<LobbyEnter_t> lobbyEnteredCallback;
    private Callback<GameLobbyJoinRequested_t> lobbyJoinRequestedCallback;
    private Callback<LobbyMatchList_t> lobbyMatchListCallback;
    private Callback<LobbyDataUpdate_t> lobbyDataUpdatedCallback;
    #endregion

    #region Private Fields
    private CSteamID currentLobbyID = CSteamID.Nil;
    private bool isHost = false;
    #endregion

    #region Public Properties
    /// <summary>
    /// Indicates if the current client is the host of the lobby
    /// </summary>
    public bool IsHost => isHost;

    /// <summary>
    /// Event triggered when successfully joining a lobby
    /// </summary>
    public delegate void OnLobbyJoinedCallback();
    public event OnLobbyJoinedCallback OnLobbyJoined;
    #endregion
    /// <summary>
    /// Maximum number of players allowed in the lobby
    /// </summary>
    [SerializeField, Range(2, 32)]
    private int maxMembers = 4;

    #region Unity Lifecycle Methods
    /// <summary>
    /// Initializes Steam callbacks during Awake
    /// </summary>
    private void Awake()
    {
        InitializeSteamCallbacks();
    }

    /// <summary>
    /// Validates Steam initialization during Start
    /// </summary>
    private void Start()
    {
        ValidateSteamInitialization();
    }
    #endregion

    #region Initialization Methods
    /// <summary>
    /// Registers all required Steam callbacks
    /// </summary>
    private void InitializeSteamCallbacks()
    {
        lobbyCreatedCallback = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        lobbyEnteredCallback = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        lobbyJoinRequestedCallback = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        lobbyMatchListCallback = Callback<LobbyMatchList_t>.Create(OnLobbyMatchList);
        lobbyDataUpdatedCallback = Callback<LobbyDataUpdate_t>.Create(OnLobbyDataUpdated);
    }

    /// <summary>
    /// Validates that Steam is properly initialized
    /// </summary>
    private void ValidateSteamInitialization()
    {
        if (!SteamManager.Initialized)
        {
            Debug.LogError("Steamworks not initialized! Make sure Steam is running and the app is properly configured.");
            return;
        }
    }
    #endregion

    #region Create / Join Lobby

    /// <summary>
    /// Creates a new Steam lobby for friends only
    /// </summary>
    public void CreateLobby()
    {
        if (!ValidateSteamConnection()) return;

        isHost = true;
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, maxMembers);
        Debug.Log("[LobbyController] Creating new lobby...");
    }

    /// <summary>
    /// Callback triggered when a lobby is successfully created
    /// </summary>
    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogError($"[LobbyController] Failed to create lobby: {callback.m_eResult}");
            isHost = false;
            return;
        }

        isHost = true;
        currentLobbyID = new CSteamID(callback.m_ulSteamIDLobby);
        Debug.Log($"[LobbyController] Lobby created successfully with ID: {currentLobbyID}");

        InitializeLobbyData();
        OnLobbyJoined?.Invoke();
    }

    /// <summary>
    /// Initializes the lobby data with default values
    /// </summary>
    private void InitializeLobbyData()
    {
        // Set lobby-wide metadata
        SteamMatchmaking.SetLobbyData(currentLobbyID, HOST_KEY, SteamUser.GetSteamID().ToString());
        SteamMatchmaking.SetLobbyData(currentLobbyID, STATE_KEY, LOBBY_STATE_LOBBY);
        SteamMatchmaking.SetLobbyData(currentLobbyID, GAME_VERSION_KEY, Application.version);

        // Initialize local player's ready state
        SteamMatchmaking.SetLobbyMemberData(currentLobbyID, READY_KEY, READY_FALSE);
    }

    /// <summary>
    /// Validates the Steam connection
    /// </summary>
    private bool ValidateSteamConnection()
    {
        if (!SteamManager.Initialized)
        {
            Debug.LogError("[LobbyController] Cannot perform lobby operations: Steam is not initialized");
            return false;
        }
        return true;
    }

    /// <summary>
    /// Joins an existing Steam lobby using its ID
    /// </summary>
    /// <param name="lobbyID">The Steam ID of the lobby to join</param>
    public void JoinLobby(CSteamID lobbyID)
    {
        if (!ValidateSteamConnection()) return;
        if (lobbyID == CSteamID.Nil)
        {
            Debug.LogError("[LobbyController] Cannot join lobby: Invalid lobby ID");
            return;
        }

        isHost = false;
        SteamMatchmaking.JoinLobby(lobbyID);
        Debug.Log($"[LobbyController] Attempting to join lobby: {lobbyID}");
    }

    /// <summary>
    /// Callback triggered when successfully joining a lobby
    /// </summary>
    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        if (callback.m_EChatRoomEnterResponse != 1)
        {
            Debug.LogError($"[LobbyController] Failed to enter lobby: Response code {callback.m_EChatRoomEnterResponse}");
            return;
        }

        currentLobbyID = new CSteamID(callback.m_ulSteamIDLobby);

        if (isHost)
        {
            Debug.Log("[LobbyController] Host entered own lobby — skipping client initialization");
            return;
        }

        Debug.Log($"[LobbyController] Successfully entered lobby: {currentLobbyID}");

        // Initialize joining player's ready state
        SteamMatchmaking.SetLobbyMemberData(currentLobbyID, READY_KEY, READY_FALSE);

        OnLobbyJoined?.Invoke();
    }

    /// <summary>
    /// Handles Steam friend invites to join a lobby
    /// </summary>
    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        Debug.Log($"[LobbyController] Received lobby join request from friend for lobby: {callback.m_steamIDLobby}");
        JoinLobby(callback.m_steamIDLobby);
    }

    /// <summary>
    /// Handles the response from a lobby list request
    /// </summary>
    private void OnLobbyMatchList(LobbyMatchList_t callback)
    {
        Debug.Log($"[LobbyController] Found {callback.m_nLobbiesMatching} matching lobbies");
        // TODO: Implement lobby list handling if needed
    }

    #endregion

    #region Ready / Start

    /// <summary>
    /// Sets the ready status for the local player
    /// </summary>
    /// <param name="ready">True if player is ready, false otherwise</param>
    public void SetReady(bool ready)
    {
        if (!ValidateLobbyState("set ready status")) return;

        string readyStatus = ready ? READY_TRUE : READY_FALSE;
        SteamMatchmaking.SetLobbyMemberData(currentLobbyID, READY_KEY, readyStatus);
        Debug.Log($"[LobbyController] Local player ready status set to: {ready}");
    }

    /// <summary>
    /// Initiates the game start sequence (host only)
    /// </summary>
    public void StartGame()
    {
        if (!ValidateHostPrivileges()) return;
        if (!ValidateLobbyState("start game")) return;
        if (!AreAllPlayersReady()) return;

        Debug.Log("[LobbyController] All players are ready. Initiating game start sequence...");

        // Update lobby state
        SteamMatchmaking.SetLobbyData(currentLobbyID, STATE_KEY, LOBBY_STATE_STARTING);

        // Initialize networking and load game scene
        StartNetworkingAndLoadScene();
    }

    /// <summary>
    /// Validates that the current player has host privileges
    /// </summary>
    private bool ValidateHostPrivileges()
    {
        if (!isHost)
        {
            Debug.LogWarning("[LobbyController] Only the host can start the game");
            return false;
        }
        return true;
    }

    /// <summary>
    /// Validates that the lobby exists and is in a valid state
    /// </summary>
    private bool ValidateLobbyState(string operation)
    {
        if (currentLobbyID == CSteamID.Nil)
        {
            Debug.LogWarning($"[LobbyController] Cannot {operation}: No active lobby");
            return false;
        }
        return true;
    }

    /// <summary>
    /// Checks if all players in the lobby are ready
    /// </summary>
    private bool AreAllPlayersReady()
    {
        int memberCount = SteamMatchmaking.GetNumLobbyMembers(currentLobbyID);

        for (int i = 0; i < memberCount; i++)
        {
            CSteamID memberId = SteamMatchmaking.GetLobbyMemberByIndex(currentLobbyID, i);
            string readyValue = SteamMatchmaking.GetLobbyMemberData(currentLobbyID, memberId, READY_KEY);

            if (readyValue != READY_TRUE)
            {
                Debug.LogWarning("[LobbyController] Cannot start: Not all players are ready");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Initializes networking and loads the game scene
    /// </summary>
    private void StartNetworkingAndLoadScene()
    {
        if (NetworkManager.singleton != null && !NetworkServer.active && !NetworkClient.active)
        {
            NetworkManager.singleton.StartHost();
            StartCoroutine(WaitAndChangeScene());
        }
        else if (NetworkManager.singleton != null && NetworkServer.active)
        {
            StartCoroutine(WaitAndChangeScene());
        }
    }

    private IEnumerator WaitAndChangeScene()
    {
        // Attends 1 frame complète pour que le client local finisse son spawn
        yield return null;
        yield return new WaitForEndOfFrame();

        NetworkManager.singleton.ServerChangeScene(GAME_SCENE_NAME);
    }

    private void OnLobbyDataUpdated(LobbyDataUpdate_t data)
    {
        // Vérifie que c’est bien le lobby actuel
        if ((CSteamID)data.m_ulSteamIDLobby != currentLobbyID)
            return;

        string state = SteamMatchmaking.GetLobbyData(currentLobbyID, "state");

        // Si l'hôte a lancé la partie
        if (state == "starting" && !isHost)
        {
            Debug.Log("Lobby state is 'starting' → launching client connection...");

            if (NetworkManager.singleton != null && !NetworkClient.active)
            {
                NetworkManager.singleton.StartClient();
            }
        }
    }


    #endregion

    private void OnDestroy()
    {
        // Unregister callbacks if needed (Steamworks.NET disposes automatically when callback field is GC'd,
        // but it's good to clear references explicitly)
        lobbyCreatedCallback = null;
        lobbyEnteredCallback = null;
        lobbyJoinRequestedCallback = null;
        lobbyMatchListCallback = null;
    }

    public void LeaveCurrentLobby()
    {
        if (currentLobbyID != CSteamID.Nil)
        {
            SteamMatchmaking.LeaveLobby(currentLobbyID);
            Debug.Log($"Left lobby {currentLobbyID}");
            currentLobbyID = CSteamID.Nil;
        }

        if (NetworkServer.active || NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopHost();
        }
    }
}