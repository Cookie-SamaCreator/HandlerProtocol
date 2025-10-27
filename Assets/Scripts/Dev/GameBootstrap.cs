using UnityEngine;
using System.Collections.Generic;

public class GameBootstrap : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject cipherPrefab;
    public GameObject playerCamPrefab;
    public GameObject hudPrefab;

    [Header("Setup")]
    public Transform[] spawnPoints;

    private List<CipherController> activePlayers = new();

    void Start()
    {
        // Simule 1 joueur local (plus tard: remplacé par un spawn réseau)
        SpawnLocalPlayer();
    }

    public void SpawnLocalPlayer()
    {
        if (cipherPrefab == null || playerCamPrefab == null) return;

        Transform spawn = spawnPoints.Length > 0
            ? spawnPoints[Random.Range(0, spawnPoints.Length)]
            : null;

        GameObject playerObj = Instantiate(cipherPrefab, spawn?.position ?? Vector3.zero, Quaternion.identity);
        CipherController playerController = playerObj.GetComponent<CipherController>();
        activePlayers.Add(playerController);

        GameObject cam = Instantiate(playerCamPrefab);
        if(!cam.TryGetComponent<CinemachineSprintFX>(out var cinemachineCam))
        {
            Debug.LogError("Could not find cinemachineSprintFX");
            return;
        }
        cinemachineCam.BindCipherPlayer(playerController);

        GameObject hud = Instantiate(hudPrefab);
        if (!hud.TryGetComponent<StaminaUI>(out var staminaUI))
        {
            Debug.LogError("Could not find StaminaUI");
            return;
        }
        staminaUI.BindPlayer(playerObj.GetComponent<Stamina>());
    }
}
