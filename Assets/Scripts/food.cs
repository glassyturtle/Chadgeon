using Unity.Netcode;
using UnityEngine;

public class food : NetworkBehaviour
{
    [SerializeField] Sprite[] foodSprites;
    [SerializeField] SpriteRenderer sr;
    [SerializeField] GameObject particle;
    [SerializeField] CircleCollider2D area;

    private void Start()
    {
        area.enabled = true;
        sr.sprite = foodSprites[Random.Range(0, foodSprites.Length)];
    }
    public void DestroySelf()
    {
        GameObject particl = Instantiate(particle, transform.position, transform.rotation);
        particl.GetComponent<NetworkObject>().Spawn();
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Pigeon hitPigeon = collision.GetComponent<Pigeon>();
        if (hitPigeon)
        {
            hitPigeon.PlayEatSound();
            if (hitPigeon.IsOwner)
            {
                Debug.Log("HitPigeon" + Time.time);
                hitPigeon.GainXP(10);
                hitPigeon.HealServer(hitPigeon.maxHp.Value / 5);
                DestroyFoodObject(this);
            }
        }
    }

    public static void DestroyFoodObject(food foodObj)
    {
        FindObjectOfType<GameManager>().DestroyFoodObject(foodObj);
    }
}
