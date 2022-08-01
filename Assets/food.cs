using UnityEngine;

public class food : MonoBehaviour
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
            Instantiate(particle, hitPigeon.transform.position, transform.rotation);
            hitPigeon.GainXP(10);
            hitPigeon.PlayEatSound();
            hitPigeon.Heal(hitPigeon.maxHp / 5);
            Destroy(gameObject);
        }
    }
}
