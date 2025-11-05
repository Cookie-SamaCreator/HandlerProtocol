using UnityEngine;
using Steamworks;
using Mirror;
using UnityEngine.SceneManagement;
using System.Collections;
using Mirror.FizzySteam;

/// <summary>
/// Manages Steam lobby creation, joining, and player state synchronization
/// </summary>
public class LobbyController : MonoBehaviour
{
    private static WaitForSeconds _waitForSeconds2 = new WaitForSeconds(2f);
    private static WaitForSeconds _waitForSeconds1 = new WaitForSeconds(1f);
    #region Constants
    // Lobby keys and options
    private const string READY_KEY = "ready";
    private const string STATE_KEY = "state";
    private const string HOST_KEY = "host";
    private const string GAME_VERSION_KEY = "gameVersion";

    // Scene and state values
    private const string GAME_SCENE_NAME = "PrototypeArena";
    private const string READY_TRUE = "1";
    private const string READY_FALSE = "0";
    private const string LOBBY_STATE_STARTING = "starting";
    private const string LOBBY_STATE_LOBBY = "lobby";
    #endregion

    #region Steam Callback Fields
    // Steamworks.NET callbacks must be stored on instance fields to remain valid
    private Callback<LobbyCreated_t> lobbyCreatedCallback;
    private Callback<LobbyEnter_t> lobbyEnteredCallback;
    private Callback<GameLobbyJoinRequested_t> lobbyJoinRequestedCallback;
    private Callback<LobbyMatchList_t> lobbyMatchListCallback;
    private Callback<LobbyDataUpdate_t> lobbyDataUpdatedCallback;
    #endregion

    #region Logging helpers
    private const string LogPrefix = "[LobbyController]";
    private void Log(string message) { Debug.Log($"{LogPrefix} {message}"); }
    private void LogWarning(string message) { Debug.LogWarning($"{LogPrefix} {message}"); }
    private void LogError(string message) { Debug.LogError($"{LogPrefix} {message}"); }
    #endregion

    #region Private Fields
    // Current lobby Steam ID
    private CSteamID currentLobbyID = CSteamID.Nil;

    // Whether this client created the lobby
    private bool isHost = false;
    #endregion

    #region Public Properties & Events
    /// <summary>
    /// True when this client is the lobby host
    /// </summary>
    public bool IsHost => isHost;

    /// <summary>
    /// Event fired after this client successfully joins a lobby
    /// </summary>
    public delegate void OnLobbyJoinedCallback();
    public event OnLobbyJoinedCallback OnLobbyJoined;
    #endregion

    /// <summary>
    /// Maximum number of players allowed in the lobby (editable in inspector)
    /// </summary>
    [SerializeField, Range(2, 32)]
    private int maxMembers = 4;

    #region Unity Lifecycle Methods
    /// <summary>
    /// Initializes Steam callbacks during Awake
    /// </summary>
    private void Awake()
    {
        // Register Steam callbacks early so events are handled during startup
        InitializeSteamCallbacks();
    }

    /// <summary>
    /// Validates Steam initialization during Start
    /// </summary>
    private void Start()
    {
        ValidateSteamInitialization();
    }

    private void Update()
    {
        SteamAPI.RunCallbacks();
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
            LogError("Steamworks not initialized! Make sure Steam is running and the app is properly configured.");
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
        Log("Creating new lobby...");
    }

    /// <summary>
    /// Callback triggered when a lobby is successfully created
    /// </summary>
    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK)
        {
            LogError($"Failed to create lobby: {callback.m_eResult}");
            isHost = false;
            return;
        }

        isHost = true;
        currentLobbyID = new CSteamID(callback.m_ulSteamIDLobby);
        Log($"Lobby created successfully with ID: {currentLobbyID}");

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
            LogError("Cannot perform lobby operations: Steam is not initialized");
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
            LogError("Cannot join lobby: Invalid lobby ID");
            return;
        }

        isHost = false;
        SteamMatchmaking.JoinLobby(lobbyID);
        Log($"Attempting to join lobby: {lobbyID}");
    }

    /// <summary>
    /// Callback triggered when successfully joining a lobby
    /// </summary>
    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        // Use Steam's chat-room enter response enum for clarity
        if (callback.m_EChatRoomEnterResponse != (uint)EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
        {
            LogError($"Failed to enter lobby: Response code {callback.m_EChatRoomEnterResponse}");
            return;
        }

        currentLobbyID = new CSteamID(callback.m_ulSteamIDLobby);

        // If we created the lobby we already did host initialization
        if (isHost)
        {
            Log("Host entered own lobby — skipping client initialization");
            return;
        }

        Log($"Successfully entered lobby: {currentLobbyID}");

        // Ensure the joining player's ready state is initialized
        SteamMatchmaking.SetLobbyMemberData(currentLobbyID, READY_KEY, READY_FALSE);

        OnLobbyJoined?.Invoke();
    }

    /// <summary>
    /// Handles Steam friend invites to join a lobby
    /// </summary>
    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        Log($"Received lobby join request from friend for lobby: {callback.m_steamIDLobby}");
        JoinLobby(callback.m_steamIDLobby);
    }

    /// <summary>
    /// Handles the response from a lobby list request
    /// </summary>
    private void OnLobbyMatchList(LobbyMatchList_t callback)
    {
        Log($"Found {callback.m_nLobbiesMatching} matching lobbies");
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
        Log($"Local player ready status set to: {ready}");
    }

    /// <summary>
    /// Initiates the game start sequence (host only)
    /// </summary>
    public void StartGame()
    {
        if (!ValidateHostPrivileges()) return;
        if (!ValidateLobbyState("start game")) return;
        if (!AreAllPlayersReady()) return;

        Log("All players are ready. Initiating game start sequence...");

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
            LogWarning("Only the host can start the game");
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
            LogWarning($"Cannot {operation}: No active lobby");
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
                LogWarning("Cannot start: Not all players are ready");
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
        // Wait one frame and an end of frame to allow local spawning to complete
        yield return null;
        yield return new WaitForEndOfFrame();

        // Use Mirror to change the server scene (host) which will notify clients
        if (NetworkManager.singleton != null && NetworkServer.active)
        {
            NetworkManager.singleton.ServerChangeScene(GAME_SCENE_NAME);
        }
    }
    private void OnLobbyDataUpdated(LobbyDataUpdate_t data)
    {
        // Ignore updates for other lobbies
        if ((CSteamID)data.m_ulSteamIDLobby != currentLobbyID)
            return;

        // Read lobby state and react accordingly
        string state = SteamMatchmaking.GetLobbyData(currentLobbyID, STATE_KEY);

        // If the host started the game, non-hosts should connect as clients
        if (state == LOBBY_STATE_STARTING && !isHost)
        {
            Log("Lobby state is 'starting' — launching client connection...");

            if (NetworkManager.singleton == null)
            {
                LogError("No NetworkManager singleton found");
                return;
            }

            if (NetworkClient.active)
            {
                Log("Client already active - skipping");
                return;
            }

            string hostIdStr = SteamMatchmaking.GetLobbyData(currentLobbyID, HOST_KEY);

            if (string.IsNullOrEmpty(hostIdStr))
            {
                LogError("Host SteamID missing from lobby data!");
                return;
            }

            if (!ulong.TryParse(hostIdStr, out ulong hostIdULong))
            {
                LogError($"Invalid Host SteamID : {hostIdStr}");
                return;
            }

            var fizzy = Transport.active as FizzySteamworks;

            if (fizzy == null)
            {
                LogError("Active transport is not FizzySteamworks.");
                return;
            }

            Log($"Attempting to connect to host SteamID : {hostIdStr}");

            StartCoroutine(WaitThenConnect(hostIdStr));
        }
    }

    private IEnumerator WaitThenConnect(string hostIdStr)
    {
        var fizzy = Transport.active as FizzySteamworks;
        if (fizzy == null)
        {
            LogError("Transport not FizzySteamworks!");
            yield break;
        }

        yield return _waitForSeconds1;
        Log("Trying first connection attempt...");
        fizzy.ClientConnect(hostIdStr);

        // attendre 2 secondes pour voir si la connexion s’établit
        yield return _waitForSeconds2;

        if (!NetworkClient.active)
        {
            LogWarning("Connection not established yet — retrying once.");
            fizzy.ClientConnect(hostIdStr);
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
        lobbyDataUpdatedCallback = null;
    }

    public void LeaveCurrentLobby()
    {
        if (currentLobbyID != CSteamID.Nil)
        {
            SteamMatchmaking.LeaveLobby(currentLobbyID);
            Log($"Left lobby {currentLobbyID}");
            currentLobbyID = CSteamID.Nil;
        }

        if (NetworkServer.active || NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopHost();
        }
    }
}