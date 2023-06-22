using System.Collections;

using Unity.Netcode;
using UnityEngine;

public class HitScript : NetworkBehaviour
{
    public NetworkVariable<ulong> indexOfDamagingPigeon = new NetworkVariable<ulong>(1);
    [SerializeField] private CircleCollider2D area;

    private void Start()
    {
        area.enabled = true;
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
            if (hitPigeon.IsOwner) hitPigeon.OnPigeonHitServerRpc(indexOfDamagingPigeon.Value);
        }
    }
}
