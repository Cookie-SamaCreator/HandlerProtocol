using System;
using Mirror;
using Mirror.FizzySteam;
using Steamworks;
using UnityEngine;

/// <summary>
/// Custom NetworkManager for The Handler Protocol.
/// </summary>
public class MyNetworkManager : NetworkManager
{
    [Header("Debug Options")]
    [Tooltip("Enable verbose debug logging for the custom NetworkManager.")]
    public bool verboseLogging = true;

    [Header("Prefabs")]
    [Tooltip("Optional camera prefab instantiated when a cipher player joins. Can be left null.")]
    [SerializeField]
    private GameObject cipherPlayerCamPrefab;

    [Tooltip("Optional HUD prefab instantiated when a cipher player joins. Can be left null.")]
    [SerializeField]
    private GameObject cipherHUDPrefab;

    private const string LogPrefix = "[MyNetworkManager]";

    #region Logging helpers
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
            LogWarning($"Spawned player (netId: {player.GetComponent<NetworkIdentity>().netId}) has no CipherController component.");
        }

        if (!player.TryGetComponent(out Stamina playerStamina))
        {
            LogWarning($"Spawned player (netId: {player.GetComponent<NetworkIdentity>().netId}) has no Stamina component.");
        }

        // Instantiate and bind camera prefab if provided and player has a CipherController.
        if (cipherPlayerCamPrefab != null)
        {
            GameObject cam = Instantiate(cipherPlayerCamPrefab);
            if (cam.TryGetComponent<CinemachineSprintFX>(out var sprintFX))
            {
                if (playerCipherController != null)
                {
                    sprintFX.BindCipherPlayer(playerCipherController);
                }
                else
                {
                    LogWarning("CinemachineSprintFX found, but spawned player has no CipherController to bind.");
                }
            }
            else
            {
                LogError("CinemachineSprintFX not found on camera prefab!");
            }
        }
        else
        {
            Log("No cipherPlayerCamPrefab assigned; skipping camera instantiation.");
        }

        // Instantiate and bind HUD prefab if provided and player has a Stamina component.
        if (cipherHUDPrefab != null)
        {
            GameObject hud = Instantiate(cipherHUDPrefab);
            if (hud.TryGetComponent<StaminaUI>(out var staminaUI))
            {
                if (playerStamina != null)
                {
                    staminaUI.BindPlayer(playerStamina);
                }
                else
                {
                    LogWarning("StaminaUI found, but spawned player has no Stamina component to bind.");
                }
            }
            else
            {
                LogError("StaminaUI not found on HUD prefab!");
            }
        }
        else
        {
            Log("No cipherHUDPrefab assigned; skipping HUD instantiation.");
        }

        // Final informative log.
        var nid = player.GetComponent<NetworkIdentity>();
        Log($"Player spawned for connection {conn.connectionId} (netId: {nid?.netId})");
    }

    /// <summary>
    /// A client disconnected from the server. Keep the base behaviour and log the event.
    /// </summary>
    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        Log($"Connection {conn.connectionId} disconnected.");
        base.OnServerDisconnect(conn);
    }

    /// <summary>
    /// Called when the local client connects to a server.
    /// </summary>
    public override void OnClientConnect()
    {
        Log("Connected to server.");
        base.OnClientConnect();
    }

    /// <summary>
    /// Called when the local client disconnects from a server.
    /// </summary>
    public override void OnClientDisconnect()
    {
        Log("Disconnected from server.");
        base.OnClientDisconnect();
    }

    /// <summary>
    /// Server-side transport error.
    /// </summary>
    public override void OnServerError(NetworkConnectionToClient conn, TransportError error, string reason)
    {
        LogError($"Network error on connection {conn?.connectionId}: {error} ({reason})");
        base.OnServerError(conn, error, reason);
    }

    /// <summary>
    /// Client-side transport error.
    /// </summary>
    public override void OnClientError(TransportError error, string reason)
    {
        LogError($"Client network error: {error} ({reason})");
        base.OnClientError(error, reason);
    }

    /// <summary>
    /// New server connection — keep base behaviour and log the connect.
    /// </summary>
    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        Log($"New server connection {conn.connectionId}");
        base.OnServerConnect(conn);
    }
}
