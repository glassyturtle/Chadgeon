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
        if (!isKnockedOut)
        {
            if (targetFood == null && targetPigeon != null || (targetPigeon != null && targetFood != null
                && (transform.position - targetPigeon.transform.position).sqrMagnitude <
                (transform.position - targetFood.transform.position).sqrMagnitude))
            {
                if (canHit && (targetPigeon.transform.position - transform.position).sqrMagnitude <= 2f)
                {
                    canHit = false;
                    StartCoroutine(RechargeHitColldown());
                    PigeonAttack(targetPigeon.transform.position);
                }
                else
                {
                    Vector2 direction = (targetPigeon.transform.position - transform.position).normalized;
                    CheckPigeonDirection(direction);
                    body.AddForce(direction * speed * Time.deltaTime);
                }
            }
            else if (targetFood != null)
            {
                Vector2 direction = (targetFood.transform.position - transform.position).normalized;
                CheckPigeonDirection(direction);
                body.AddForce(direction * speed * Time.deltaTime);
            }
        }
    }
}
