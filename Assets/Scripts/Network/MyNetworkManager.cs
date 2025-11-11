using System;
using System.Collections.Generic;
using Mirror;
using Mirror.FizzySteam;
using Steamworks;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Custom NetworkManager for Handler Protocol.
/// </summary>
public class MyNetworkManager : NetworkManager
{
    [Header("Player Prefabs")]
    public GameObject CipherPlayerPrefab;
    public GameObject PlayerEntryUIPrefab;

    [Header("Debug Options")]
    [Tooltip("Enable verbose debug logging for the custom NetworkManager.")]
    public bool verboseLogging = true;
    public static event Action OnAllPlayersSpawned;
    public int expectedPlayers = 0;
    private const string GAME_SCENE_NAME = "PrototypeArena";
    private const string MENU_SCENE_NAME = "MainMenu";

    #region Logging helpers
    private const string LogPrefix = "[MyNetworkManager]";
    private void Log(string message) { if (verboseLogging) Debug.Log($"{LogPrefix} {message}"); }
    private void LogWarning(string message) { if (verboseLogging) Debug.LogWarning($"{LogPrefix} {message}"); }
    private void LogError(string message) { Debug.LogError($"{LogPrefix} {message}"); }
    #endregion

    /// <summary>
    /// Called when the server starts. Keeps a small informative log entry when verboseLogging is enabled.
    /// </summary>
    public override void OnStartServer()
    {
        base.OnStartServer();
        Log("Server started successfully.");
    }

    /// <summary>
    /// Called when the server stops.
    /// </summary>
    public override void OnStopServer()
    {
        base.OnStopServer();
        Log("Server stopped.");
    }

    /// <summary>
    /// Client started locally.
    /// </summary>
    public override void OnStartClient()
    {
        base.OnStartClient();
        Log("Client started.");
    }

    /// <summary>
    /// Client stopped locally.
    /// </summary>
    public override void OnStopClient()
    {
        base.OnStopClient();
        Log("Client stopped.");
    }

    /// <summary>
    /// Server-side player creation. Prevents duplicate player creation when the connection already has an identity.
    /// Also safely instantiates optional camera and HUD prefabs and binds them to the spawned player where appropriate.
    /// </summary>
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        base.OnServerAddPlayer(conn);
        /*if (SceneManager.GetActiveScene().name == GAME_SCENE_NAME)
        {
            if (conn == null)
            {
                LogError("OnServerAddPlayer called with null connection.");
                return;
            }

            // If this connection already has an identity, we should not create another player.
            if (conn.identity != null)
            {
                LogWarning($"Skipping AddPlayer: player already exists for connection {conn.connectionId}.");
                return;
            }

            // playerPrefab is defined on NetworkManager; ensure it's set to avoid runtime errors.
            if (playerPrefab == null)
            {
                LogError("playerPrefab is not assigned on the NetworkManager. Cannot spawn player.");
                return;
            }

            Transform startPos = GetStartPosition();

            // Instantiate player at spawn point or origin.
            Vector3 spawnPos = startPos != null ? startPos.position : Vector3.zero;
            Quaternion spawnRot = startPos != null ? startPos.rotation : Quaternion.identity;

            GameObject player = Instantiate(playerPrefab, spawnPos, spawnRot);

            // Add player for connection (this registers the player with Mirror and spawns on clients)
            NetworkServer.AddPlayerForConnection(conn, player);

            // Cache commonly-used components to avoid multiple GetComponent calls and to check for missing components.

            if (!player.TryGetComponent(out CipherController playerCipherController))
            {
                // Not fatal: some players may not have this component depending on prefab layout.
                LogError($"Spawned player (netId: {player.GetComponent<NetworkIdentity>().netId}) has no CipherController component.");
            }

            if (!player.TryGetComponent(out NetworkCipherPlayer networkCipher))
            {
                LogError($"Spawned player (netId: {player.GetComponent<NetworkIdentity>().netId}) has no NetworkCipher component.");
            }

            // Final informative log.
            var nid = player.GetComponent<NetworkIdentity>();
            Log($"Player spawned for connection {conn.connectionId} (netId: {nid?.netId})");
            players ??= new List<GameObject>();

            if (players.Count == expectedPlayers)
            {
                //RpcNotifyAllPlayersReady();
                OnAllPlayersSpawned?.Invoke();
            }
        }*/
    }

    public override void ServerChangeScene(string newSceneName)
    {
        if (newSceneName == GAME_SCENE_NAME)
        {
            //if cipher ->
            playerPrefab = CipherPlayerPrefab;
            this.onlineScene = newSceneName;
        }
        else if (newSceneName == MENU_SCENE_NAME)
        {
            playerPrefab = PlayerEntryUIPrefab;
        }
        base.ServerChangeScene(newSceneName);
    }
}
