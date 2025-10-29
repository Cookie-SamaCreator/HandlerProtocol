using UnityEngine;
using Steamworks;
using UnityEngine.SceneManagement;

public class LobbyController : MonoBehaviour
{
    // Callbacks must be stored on instance fields to stay alive
    private Callback<LobbyCreated_t> lobbyCreatedCallback;
    private Callback<LobbyEnter_t> lobbyEnteredCallback;
    private Callback<GameLobbyJoinRequested_t> lobbyJoinRequestedCallback;
    private Callback<LobbyMatchList_t> lobbyMatchListCallback;

    private CSteamID currentLobbyID = CSteamID.Nil;
    private bool isHost = false;

    public bool IsHost => isHost;

    public delegate void OnLobbyJoinedCallback();
    public event OnLobbyJoinedCallback OnLobbyJoined;

    public int maxMembers = 4;

    private void Awake()
    {
        // register callbacks
        lobbyCreatedCallback = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        lobbyEnteredCallback = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        lobbyJoinRequestedCallback = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        lobbyMatchListCallback = Callback<LobbyMatchList_t>.Create(OnLobbyMatchList);
    }

    private void Start()
    {
        if (!SteamManager.Initialized)
        {
            Debug.LogError("Steamworks not initialized!");
            return;
        }
    }

    #region Create / Join Lobby

    // Héberger
    public void CreateLobby()
    {
        if (!SteamManager.Initialized) return;

        isHost = true;
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, maxMembers);
        Debug.Log("CreateLobby called...");
        // OnLobbyCreated sera appelé via callback
    }

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogError($"Failed to create lobby: {callback.m_eResult}");
            return;
        }

        currentLobbyID = new CSteamID(callback.m_ulSteamIDLobby);
        Debug.Log("Lobby created with ID: " + currentLobbyID);

        // Set lobby-wide metadata (host SteamID, gameVersion, etc.)
        SteamMatchmaking.SetLobbyData(currentLobbyID, "host", SteamUser.GetSteamID().ToString());
        SteamMatchmaking.SetLobbyData(currentLobbyID, "state", "lobby");
        SteamMatchmaking.SetLobbyData(currentLobbyID, "gameVersion", Application.version);

        // Initialize this player's ready state via SetLobbyMemberData (for the calling user)
        SteamMatchmaking.SetLobbyMemberData(currentLobbyID, "ready", "0");

        OnLobbyJoined?.Invoke();
    }

    // Rejoindre via CSteamID
    public void JoinLobby(CSteamID lobbyID)
    {
        if (!SteamManager.Initialized) return;

        isHost = false;
        SteamMatchmaking.JoinLobby(lobbyID);
        Debug.Log("JoinLobby called for: " + lobbyID);
        // OnLobbyEntered sera appelé via callback
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        // m_EChatRoomEnterResponse indicates how it went; 1 = success usually, but check result code
        // We'll accept any non-zero lobby id entry here and then set the current lobby
        currentLobbyID = new CSteamID(callback.m_ulSteamIDLobby);
        Debug.Log("Entered lobby: " + currentLobbyID);

        // Ensure that the joining player's member data has a default ready value
        SteamMatchmaking.SetLobbyMemberData(currentLobbyID, "ready", "0");

        OnLobbyJoined?.Invoke();
    }

    // Optional: handle invites (when a friend invites you)
    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        Debug.Log("GameLobbyJoinRequested: " + callback.m_steamIDLobby);
        JoinLobby(callback.m_steamIDLobby);
    }

    // Optional: match list callback (if you use SteamMatchmaking.RequestLobbyList)
    private void OnLobbyMatchList(LobbyMatchList_t callback)
    {
        Debug.Log($"LobbyMatchList returned {callback.m_nLobbiesMatching}");
    }

    #endregion

    #region Ready / Start

    // Appelé par UI pour définir l'état "ready" du membre local
    public void SetReady(bool ready)
    {
        if (currentLobbyID == CSteamID.Nil) return;
        // SetLobbyMemberData sets data for the local user in the given lobby
        SteamMatchmaking.SetLobbyMemberData(currentLobbyID, "ready", ready ? "1" : "0");
        Debug.Log($"Set ready = {ready} for local user in lobby {currentLobbyID}");
    }

    // Lancer la partie : l'hôte vérifie que tous les membres ont ready == "1"
    public void StartGame()
    {
        if (!isHost)
        {
            Debug.LogWarning("Only host can start the game");
            return;
        }

        if (currentLobbyID == CSteamID.Nil)
        {
            Debug.LogWarning("No lobby available to start");
            return;
        }

        int memberCount = SteamMatchmaking.GetNumLobbyMembers(currentLobbyID);
        for (int i = 0; i < memberCount; i++)
        {
            CSteamID memberId = SteamMatchmaking.GetLobbyMemberByIndex(currentLobbyID, i);

            // CORRECTION: GetLobbyMemberData requires (lobby, steamIDUser, key)
            string readyValue = SteamMatchmaking.GetLobbyMemberData(currentLobbyID, memberId, "ready");
            Debug.Log($"Member {memberId} ready = {readyValue}");

            if (readyValue != "1")
            {
                Debug.LogWarning("Not all players are ready. Cannot start.");
                return;
            }
        }

        Debug.Log("All players are ready. Starting game...");

        // Optional: set lobby state to starting
        SteamMatchmaking.SetLobbyData(currentLobbyID, "state", "starting");

        // Ici on charge la scène de jeu. Plus tard on déclenchera le spawn réseau + transport.
        SceneManager.LoadScene("PrototypeArena");
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
    }

}
