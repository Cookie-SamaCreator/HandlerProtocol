using UnityEngine;
using Mirror;

public class Projectile : NetworkBehaviour
{
    [SerializeField] private float speed = 50f;
    [SerializeField] private float lifeTime = 3f;
    [SerializeField] private float damage = 20f;

    private GameObject owner;

    public void Initialize(GameObject owner, float dmg)
    {
        this.owner = owner;
        this.damage = dmg;
        Invoke(nameof(DestroySelf), lifeTime);
    }

    private void Update()
    {
        if (isServer)
        {
            transform.position += speed * Time.deltaTime * transform.forward;
        }
    }

    [ServerCallback]
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == owner) return;

        if (other.TryGetComponent(out Health target))
            target.TakeDamage(damage, owner);

        DestroySelf();
    }

    [Server]
    private void DestroySelf()
    {
        NetworkServer.Destroy(gameObject);
    }
}
