using Mirror;
using UnityEngine;

[RequireComponent(typeof(HandlerController))]
[RequireComponent(typeof(Health))]
public class NetworkHandlerPlayer : NetworkBehaviour
{
    private HandlerController handler;
    public Health health;
    public string playerName;
    void Awake()
    {
        handler = GetComponent<HandlerController>();
        health = GetComponent<Health>();
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        handler.isLocalPlayer = true;

        if (handler.cameraHolder != null)
        {
            handler.cameraHolder.gameObject.SetActive(true);
        }
        Debug.Log("Local handler player started: " + netIdentity.netId);
    }

    public override void OnStopLocalPlayer()
    {
        handler.isLocalPlayer = false;
        if (handler.cameraHolder != null)
        {
            handler.cameraHolder.gameObject.SetActive(false);
        }
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        if (isLocalPlayer)
        {
            Debug.Log("Local handler player stopped.");
        }
    }

    void Update()
    {
        // optionnel : uniquement le joueur local prend les inputs
        if (!isLocalPlayer) return;
        // rest of update handled in CipherController
    }

    // Exemple de commande pour tirer (exécutée sur le serveur)
    [Command]
    public void CmdFire(Vector3 origin, Vector3 direction)
    {
        // serveur peut valider puis appeler un RPC pour spawn effet/damage
        RpcDoFire(origin, direction);
    }

    [ClientRpc]
    void RpcDoFire(Vector3 origin, Vector3 direction)
    {
        // play fx localement (muzzle, hit markers, etc)
        // ou appliquer logique de dégâts si server authoritative
    }
}
