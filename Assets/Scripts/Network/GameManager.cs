using UnityEngine;
using System.Collections.Generic;
using Mirror;
using Steamworks;

public class GameManager : MonoBehaviour
{
    [SerializeField] private LobbyUIManager lobbyUIManager;
    public static GameManager Instance;
    public Dictionary<string, PlayerData> connectedPlayers = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if(!lobbyUIManager.gameObject.activeInHierarchy)
        {
            lobbyUIManager.gameObject.SetActive(true);
        }
    }

    public void RegisterPlayer(PlayerData data)
    {
        if (!connectedPlayers.ContainsKey(data.playerName))
        {
            connectedPlayers.Add(data.playerName, data);
        }
        else
        {
            connectedPlayers[data.playerName] = data;
        }
    }

    public void ClearPlayers() => connectedPlayers.Clear();
}

[System.Serializable]
public class PlayerData
{
    public string playerName;
    public Texture2D avatar;
    public string selectedCharacterId; // ex: "Cipher01", "Rogue02", etc.
    public string[] selectedWeapons;   // pour le loadout

    public PlayerData(string name, Texture2D avatar)
    {
        this.playerName = name;
        this.avatar = avatar;
    }
}
