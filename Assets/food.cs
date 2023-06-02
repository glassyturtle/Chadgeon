using Unity.Netcode;
using UnityEngine;

public class food : NetworkBehaviour
{
    // Start is called before the first frame update
    [SerializeField] Sprite[] foodSprites;
    [SerializeField] SpriteRenderer sr;
    [SerializeField] GameObject particle;
    [SerializeField] CircleCollider2D area;

    private void Start()
    {
        area.enabled = true;
        sr.sprite = foodSprites[Random.Range(0, foodSprites.Length)];
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Pigeon hitPigeon = collision.GetComponent<Pigeon>();
        if (hitPigeon && hitPigeon.IsOwner)
        {
            hitPigeon.GainXPServerRpc(10);
            hitPigeon.PlayEatSound();
            hitPigeon.HealServerRpc(hitPigeon.maxHp.Value / 5);

            if (IsOwner)
            {
                SpawnEatParticleServerRpc(hitPigeon.transform.position);
                Destroy(gameObject);
            }
        }
    }
    [ServerRpc]
    private void SpawnEatParticleServerRpc(Vector3 pos)
    {
        GameObject particl = Instantiate(particle, pos, transform.rotation);
        particl.GetComponent<NetworkObject>().Spawn();
    }
}
