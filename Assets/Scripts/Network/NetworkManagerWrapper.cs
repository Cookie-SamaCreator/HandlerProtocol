using Mirror;
using UnityEngine;

public class NetworkManagerWrapper : MonoBehaviour
{
    public static NetworkManagerWrapper Instance { get; private set; }
    private NetworkManager nm;

    private void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        Instance = this;
        nm = NetworkManager.singleton;
    }

    public void StartHost()
    {
        nm?.StartHost();
    }

    public void StartClient()
    {
        nm?.StartClient();
    }

    public void ServerChangeScene(string sceneName)
    {
        nm?.ServerChangeScene(sceneName);
    }
}
