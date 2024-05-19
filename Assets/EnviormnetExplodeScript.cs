using UnityEngine;

public class EnviormnetExplodeScript : MonoBehaviour
{
    [SerializeField] GameObject particleEffect;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Instantiate(particleEffect, transform.position, transform.rotation);
        Destroy(gameObject);
    }

}
