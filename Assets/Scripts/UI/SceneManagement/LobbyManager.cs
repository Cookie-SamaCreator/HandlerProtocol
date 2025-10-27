using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance;

    private void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Placeholder API — replace with networked lobby implementation
    public void CreateLobby()
    {
        Debug.Log("Lobby created (placeholder).");
        // Example: call Steamworks/Photon API here
    }

    public void JoinLobby(string code)
    {
        Debug.Log($"Joining lobby {code} (placeholder).");
    }
}
