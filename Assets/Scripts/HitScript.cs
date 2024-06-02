using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class HitScript : NetworkBehaviour
{
    public NetworkVariable<Pigeon.AttackProperties> attackProperties = new(new Pigeon.AttackProperties(), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> isVisible = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    [SerializeField] private Collider2D area;
    [SerializeField] private GameObject mask;
    [SerializeField] private SpriteRenderer sr;

    [SerializeField] AudioSource audioSorce;
    [SerializeField] AudioClip[] swooshSounds;


    private void Update()
    {
        if (isVisible.Value)
        {
            area.enabled = true;
            sr.enabled = true;
        }
        else
        {
            sr.enabled = false;
            area.enabled = false;

        }
    }
    public void Activate(Vector3 pos, Quaternion angle, bool isSlaming)
    {
        PlaySwooshServerRpc();
        if (isSlaming)
        {
            transform.localScale = new Vector3(6, 6, 1);
            mask.transform.localScale = new Vector3(20, 20, 1);

        }
        else
        {
            transform.localScale = new Vector3(2, 2, 1);
            mask.transform.localScale = new Vector3(2, 2, 1);
        }

        StopAllCoroutines();
        LeanTween.cancel(gameObject);
        mask.transform.localScale = new Vector3(2, 2, 1);
        isVisible.Value = true;

        if (attackProperties.Value.isFacingLeft == true)
        {
            if (attackProperties.Value.attackingUp == false) mask.transform.localPosition = new Vector3(0, 0.26f);
            else mask.transform.localPosition = new Vector3(0, -0.26f);
        }
        else
        {
            if (attackProperties.Value.attackingUp == true) mask.transform.localPosition = new Vector3(0, 0.26f);
            else mask.transform.localPosition = new Vector3(0, -0.26f);
        }

        LeanTween.scaleY(mask, 0, 0.1f);
        StartCoroutine(Damage());

        if (!IsOwner) return;
        transform.SetPositionAndRotation(pos, angle);
    }


    IEnumerator Damage()
    {
        yield return new WaitForSeconds(0.1f);
        isVisible.Value = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {

        if (!IsOwner) return;

        Pigeon hitPigeon = collision.GetComponent<Pigeon>();

        if (!hitPigeon || attackProperties.Value.pigeonID == hitPigeon.NetworkObjectId) return;

        HitPigeonServerRPC(hitPigeon.NetworkObjectId, attackProperties.Value);
    }

    [ServerRpc]
    private void HitPigeonServerRPC(ulong targetID, Pigeon.AttackProperties atkProp)
    {
        try
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects[targetID])
            {
                NetworkObject ob = NetworkManager.Singleton.SpawnManager.SpawnedObjects[targetID];
                if (!ob) return;
                ob.GetComponent<Pigeon>().OnPigeonHitCLientRPC(atkProp);
            }
        }
        catch
        {
            Debug.Log("Start Error");
        }
    }

    [ServerRpc]
    private void PlaySwooshServerRpc()
    {
        PlaySwooshClientRpc();

    }
    [ClientRpc]
    private void PlaySwooshClientRpc()
    {
        audioSorce.clip = swooshSounds[Random.Range(0, swooshSounds.Length)];
        audioSorce.Play();
    }

}
