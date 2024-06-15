using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class featherScript : NetworkBehaviour
{

    [SerializeField] Rigidbody2D body;
    bool activated = false;
    public NetworkVariable<Pigeon.AttackProperties> attackProperties = new(new Pigeon.AttackProperties(), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public void Activate(Pigeon.AttackProperties atk, Quaternion angle)
    {
        attackProperties.Value = atk;
        transform.rotation = angle;
        activated = true;
        StartCoroutine(DestroyWait());
    }

    private void FixedUpdate()
    {
        if (!activated) return;
        body.AddForce(20 * transform.right);
    }

    IEnumerator DestroyWait()
    {
        yield return new WaitForSeconds(4);
        Destroy(gameObject);
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsOwner || !activated) return;

        Pigeon hitPigeon = collision.GetComponent<Pigeon>();

        if (!hitPigeon || attackProperties.Value.pigeonID == hitPigeon.NetworkObjectId) return;

        HitPigeonServerRPC(hitPigeon.NetworkObjectId);
    }

    [ServerRpc]
    private void HitPigeonServerRPC(ulong targetID)
    {
        try
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects[targetID])
            {
                NetworkObject ob = NetworkManager.Singleton.SpawnManager.SpawnedObjects[targetID];
                if (!ob) return;

                ob.GetComponent<Pigeon>().OnPigeonHitCLientRPC(attackProperties.Value);
                Destroy(gameObject);
            }
        }
        catch
        {
            Debug.Log("Start Error");
        }
    }
}
