using Mirror;
using UnityEngine;

[RequireComponent(typeof(CipherController))]
[RequireComponent(typeof(Stamina))]
public class NetworkCipherPlayer : NetworkBehaviour
{
    private CipherController cipher;
    void Awake()
    {
        cipher = GetComponent<CipherController>();
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        cipher.isLocalPlayer = true;
        Debug.Log("Local player started: " + netIdentity.netId);
    }

    public override void OnStopLocalPlayer()
    {
        cipher.isLocalPlayer = false;
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        if (isLocalPlayer)
        {
            Debug.Log("Local player stopped.");
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
