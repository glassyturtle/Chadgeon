using Unity.Netcode;
using UnityEngine;

public class food : NetworkBehaviour
{
    // Start is called before the first frame update
    [SerializeField] Sprite[] foodSprites;
    [SerializeField] SpriteRenderer sr;
    [SerializeField] GameObject particle;
    private void Start()
    {
        sr.sprite = foodSprites[Random.Range(0, foodSprites.Length)];
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Pigeon hitPigeon = collision.GetComponent<Pigeon>();
        if (hitPigeon)
        {
            GameObject particl =  Instantiate(particle, hitPigeon.transform.position, transform.rotation);
            particl.GetComponent<NetworkObject>().Spawn();
            hitPigeon.GainXP(10);
            hitPigeon.PlayEatSound();
            hitPigeon.Heal(hitPigeon.maxHp.Value / 5);
            Destroy(gameObject);
        }
    }
}
