using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class HitScript : NetworkBehaviour
{
    public Pigeon pigeonThatDealtDamage;

    private void Start()
    {
        if (!IsOwner) return;
        StartCoroutine(damage());
    }

    IEnumerator damage()
    {
        yield return new WaitForSeconds(0.1f);
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Pigeon hitPigeon = collision.GetComponent<Pigeon>();
        if (hitPigeon)
        {
            hitPigeon.OnPigeonHit(pigeonThatDealtDamage);
        }
    }
} 
