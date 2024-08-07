using Unity.Netcode;
using UnityEngine;

public class food : NetworkBehaviour
{
    [SerializeField] Sprite[] foodSprites;
    [SerializeField] SpriteRenderer sr;
    [SerializeField] GameObject particle;
    [SerializeField] CircleCollider2D area;
    [SerializeField] private int healModifier;
    [SerializeField] private int xpGained;
    [SerializeField] private bool isCone = true;


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
            if (hitPigeon.IsOwner)
            {
                hitPigeon.GainXP(xpGained, isCone);
                hitPigeon.stamina++;
                if (hitPigeon.stamina >= hitPigeon.maxStamina) hitPigeon.stamina = hitPigeon.maxStamina;
                hitPigeon.HealServer(hitPigeon.maxHp.Value / healModifier);
                DestroyFoodObject(this);
            }
        }
    }

    public static void DestroyFoodObject(food foodObj)
    {
        GameManager.instance.DestroyFoodObject(foodObj);
    }
}
