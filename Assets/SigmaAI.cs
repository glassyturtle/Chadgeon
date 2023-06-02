using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SigmaAI : PigeonAI
{
    private void Update()
    {
        FindNearbyPigeon();
        FindNearbyFood();
    }
    private void FixedUpdate()
    {
        if (!isKnockedOut.Value)
        {
            if (targetFood == null && targetPigeon != null || (targetPigeon != null && targetFood != null
                && (transform.position - targetPigeon.transform.position).sqrMagnitude <
                (transform.position - targetFood.transform.position).sqrMagnitude))
            {
                if (canHit && (targetPigeon.transform.position - transform.position).sqrMagnitude <= 2f)
                {
                    canHit = false;
                    StartCoroutine(RechargeHitColldown());

                    Vector3 targ = targetPigeon.transform.position;
                    targ.z = 0f;
                    targ.x -= transform.position.x;
                    targ.y -= transform.position.y;

                    float angle = Mathf.Atan2(targ.y, targ.x) * Mathf.Rad2Deg;
                    Quaternion theAngle = Quaternion.Euler(new Vector3(0, 0, angle));

                    PigeonAttackServerRpc(targetPigeon.transform.position, theAngle, no.NetworkObjectId);
                }
                else
                {
                    Vector2 direction = (targetPigeon.transform.position - transform.position).normalized;
                    CheckDirection(direction);
                    body.AddForce(direction * speed * Time.deltaTime);
                }
            }
            else if (targetFood != null)
            {
                Vector2 direction = (targetFood.transform.position - transform.position).normalized;
                CheckDirection(direction);
                body.AddForce(direction * speed * Time.deltaTime);
            }
        }
    }
}
