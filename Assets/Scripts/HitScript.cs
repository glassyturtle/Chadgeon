using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class HitScript : NetworkBehaviour
{
    public NetworkVariable<Pigeon.AttackProperties> attackProperties = new(new Pigeon.AttackProperties(), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> isVisible = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    [SerializeField] private CircleCollider2D area;
    [SerializeField] private GameObject mask;
    [SerializeField] private SpriteRenderer sr;


    private void Update()
    {
        if (isVisible.Value)
        {
            sr.enabled = true;
            area.enabled = true;
        }
        else
        {
            sr.enabled = false;
            area.enabled = false;
        }
    }
    public void Activate(Vector3 pos, Quaternion angle, bool isSlaming)
    {

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
        Pigeon hitPigeon = collision.GetComponent<Pigeon>();

        if (!hitPigeon || !hitPigeon.IsOwner || attackProperties.Value.pigeonID == hitPigeon.NetworkObjectId) return;

        hitPigeon.OnPigeonHit(attackProperties.Value);
    }
}
