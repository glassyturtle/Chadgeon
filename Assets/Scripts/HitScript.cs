using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class HitScript : NetworkBehaviour
{
    public Pigeon attackingPigeon = null;
    public ulong indexOfDamagingPigeon;
    public Pigeon.AttackProperties attackProperties = new();

    [SerializeField] private CircleCollider2D area;

    private void Start()
    {
        area.enabled = true;
        if (!IsOwner) return;
        StartCoroutine(Damage());
    }

    IEnumerator Damage()
    {
        yield return new WaitForSeconds(0.1f);
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Pigeon hitPigeon = collision.GetComponent<Pigeon>();
        if (!hitPigeon || !hitPigeon.IsOwner || attackingPigeon == hitPigeon) return;

        hitPigeon.OnPigeonHitServerRpc(attackProperties);
    }
}
