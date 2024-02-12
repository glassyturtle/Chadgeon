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

                    Vector3 pos = transform.position;
                    AttackProperties atkProp = new()
                    {
                        pigeonID = NetworkObjectId,
                        damage = damage,
                        hasCriticalDamage = false,
                        hasKnockBack = false,
                        posX = pos.x,
                        posY = pos.y,
                    };
                    if (pigeonUpgrades.TryGetValue(Upgrades.critcalDamage, out bool a)) atkProp.hasCriticalDamage = true;
                    if (pigeonUpgrades.TryGetValue(Upgrades.brawler, out bool d)) atkProp.hasKnockBack = true;
                    PigeonAttack(atkProp, theAngle);
                }
                else
                {
                    Vector2 direction = (targetPigeon.transform.position - transform.position).normalized;
                    CheckDirection(direction);
                    body.AddForce(speed * Time.deltaTime * direction);
                }
            }
            else if (targetFood != null)
            {
                Vector2 direction = (targetFood.transform.position - transform.position).normalized;
                CheckDirection(direction);
                body.AddForce(speed * Time.deltaTime * direction);
            }
        }
    }
}
