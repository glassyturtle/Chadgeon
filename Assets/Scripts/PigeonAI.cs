using System;
using System.Collections;
using UnityEngine;

public class PigeonAI : Pigeon
{

    [SerializeField] protected Pigeon targetPigeon;
    [SerializeField] protected food targetFood;


    private float hitColldown = 0.3f;
    protected bool canHit = true;

    private Action<float, float> behaviorAI;



    public void SetAI(int difficulty)
    {
        switch (difficulty)
        {
            case 0:
                behaviorAI = SimpBehavior;
                pigeonName = "Simp";
                hitColldown = 0.3f;
                break;
            case 1:
                behaviorAI = ChadBehavior;
                pigeonName = "Chad";
                hitColldown = 0.2f;
                break;
            case 2:
                behaviorAI = SigmaBehavior;
                pigeonName = "Sigma";
                hitColldown = 0.15f;
                break;
        }
        displayText.text = pigeonName + " lvl " + level;
    }


    private void Start()
    {
        OnPigeonSpawn();
    }
    private void Update()
    {
        FindNearbyPigeon();
        FindNearbyFood();
    }

    private void FixedUpdate()
    {
        if (!isKnockedOut.Value)
        {
            if (!isSlaming.Value)
            {
                float distanceToPigeon = Mathf.Infinity;
                float distanceToFood = Mathf.Infinity;
                if (targetPigeon) distanceToPigeon = (targetPigeon.transform.position - transform.position).sqrMagnitude;
                if (targetFood) distanceToFood = (targetFood.transform.position - transform.position).sqrMagnitude;

                behaviorAI?.Invoke(distanceToPigeon, distanceToFood);
            }
            else if (isSlaming.Value)
            {
                Vector2 direction = (slamPos - transform.position).normalized;
                CheckDirection(direction);
                body.AddForce(direction * speed * 4 * Time.deltaTime);
                if ((transform.position - slamPos).sqrMagnitude <= 2.5f)
                {
                    EndSlam();
                }
            }
        }
        else if (body.velocity.magnitude < 0.1f && canDeCollide)
        {
            canDeCollide = false;
            bodyCollider.enabled = false;
        }
    }

    public void AILevelUP()
    {
        displayText.text = pigeonName + " lvl " + level;
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("banish"))
        {
            transform.position = Vector3.zero;
        }
    }

    protected IEnumerator RechargeHitColldown()
    {
        yield return new WaitForSeconds(hitColldown);
        canHit = true;
    }
    protected void FindNearbyPigeon()
    {
        Pigeon[] allPigeons = FindObjectsOfType<Pigeon>();
        Pigeon closestPigeon = null;
        float distToCloset = Mathf.Infinity;
        for (int i = 0; i < allPigeons.Length; i++)
        {
            float currentDist = Vector2.SqrMagnitude(allPigeons[i].transform.position - transform.position);
            if (allPigeons[i] != this && currentDist < distToCloset && !allPigeons[i].isKnockedOut.Value)
            {
                closestPigeon = allPigeons[i];
                distToCloset = currentDist;
            }
        }

        if (closestPigeon == null)
        {
            targetPigeon = null;
        }
        else
        {
            targetPigeon = closestPigeon;
        }
    }
    protected void FindNearbyFood()
    {
        food[] allPigeons = FindObjectsOfType<food>();
        food closestPigeon = null;
        float distToCloset = Mathf.Infinity;
        for (int i = 0; i < allPigeons.Length; i++)
        {
            float currentDist = Vector2.SqrMagnitude(allPigeons[i].transform.position - transform.position);
            if (allPigeons[i] != this && currentDist < distToCloset)
            {
                closestPigeon = allPigeons[i];
                distToCloset = currentDist;
            }
        }

        if (closestPigeon == null)
        {
            targetFood = null;
        }
        else
        {
            targetFood = closestPigeon;
        }
    }


    private void SimpBehavior(float distanceToPigeon, float distanceToFood)
    {
        if (targetPigeon && currentHP.Value < maxHp.Value / 2)
        {
            if (distanceToPigeon <= 10)
            {
                //runs away when another pigeon is near and on less than half hp
                Vector2 direction = (transform.position - targetPigeon.transform.position).normalized;
                CheckDirection(direction);
                body.AddForce(speed * Time.deltaTime * direction);
            }
            else if (targetFood)
            {
                //goes to neaby food item 
                Vector2 direction = (targetFood.transform.position - transform.position).normalized;
                CheckDirection(direction);
                body.AddForce(speed * Time.deltaTime * direction);
            }
        }
        else
        {
            if (targetPigeon && targetFood && distanceToFood < distanceToPigeon * 2)
            {
                //goes to neaby food item 
                Vector2 direction = (targetFood.transform.position - transform.position).normalized;
                CheckDirection(direction);
                body.AddForce(speed * Time.deltaTime * direction);
            }
            else if (targetPigeon)
            {
                if (canHit && distanceToPigeon <= 2f)
                {
                    canHit = false;
                    StartCoroutine(RechargeHitColldown());

                    Vector3 targ = slamPos;
                    targ.z = 0f;
                    targ.x -= transform.position.x;
                    targ.y -= transform.position.y;

                    float angle = Mathf.Atan2(targ.y, targ.x) * Mathf.Rad2Deg;
                    Quaternion theAngle = Quaternion.Euler(new Vector3(0, 0, angle));

                    AttackProperties atkProp = new()
                    {
                        pigeonID = NetworkObjectId,
                        damage = damage,
                        hasCriticalDamage = false,
                        hasKnockBack = false,
                        posX = slamPos.x,
                        posY = slamPos.y,
                    }; if (pigeonUpgrades.TryGetValue(Upgrades.critcalDamage, out bool a)) atkProp.hasCriticalDamage = true;
                    if (pigeonUpgrades.TryGetValue(Upgrades.knockBack, out bool d)) atkProp.hasKnockBack = true;
                    PigeonAttack(atkProp, theAngle);
                }
                else
                {
                    StartSlam(targetPigeon.transform.position);
                    Vector2 direction = (targetPigeon.transform.position - transform.position).normalized;
                    CheckDirection(direction);
                    body.AddForce(speed * Time.deltaTime * direction);
                }
            }
        }
    }
    private void ChadBehavior(float distanceToPigeon, float distanceToFood)
    {
        if (canHit && distanceToPigeon <= 3f)
        {
            canHit = false;
            StartCoroutine(RechargeHitColldown());

            Vector3 targ = slamPos;
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
            if (pigeonUpgrades.TryGetValue(Upgrades.critcalDamage, out bool _)) atkProp.hasCriticalDamage = true;
            if (pigeonUpgrades.TryGetValue(Upgrades.knockBack, out bool _)) atkProp.hasKnockBack = true;
            PigeonAttack(atkProp, theAngle);
        }
        if (targetPigeon && currentHP.Value < maxHp.Value / 2)
        {
            if (distanceToPigeon <= 9)
            {
                //runs away when another pigeon is near and on less than half hp
                Vector2 direction = (transform.position - targetPigeon.transform.position).normalized;
                CheckDirection(direction);
                body.AddForce(speed * Time.deltaTime * direction);
            }
            else if (targetFood)
            {
                StartSlam(targetFood.transform.position);
                //goes to neaby food item 
                Vector2 direction = (targetFood.transform.position - transform.position).normalized;
                CheckDirection(direction);
                body.AddForce(speed * Time.deltaTime * direction);
            }
        }
        else
        {
            if (targetPigeon && targetFood && distanceToFood < distanceToPigeon * 1.5f)
            {
                //goes to neaby food item 
                Vector2 direction = (targetFood.transform.position - transform.position).normalized;
                CheckDirection(direction);
                body.AddForce(speed * Time.deltaTime * direction);
            }
            else if (targetPigeon)
            {
                StartSlam(targetPigeon.transform.position);
                Vector2 direction = (targetPigeon.transform.position - transform.position).normalized;
                CheckDirection(direction);
                body.AddForce(speed * Time.deltaTime * direction);
            }
        }
    }
    private void SigmaBehavior(float distanceToPigeon, float distanceToFood)
    {
        if (targetPigeon && canHit && distanceToPigeon <= 3.7f)
        {
            canHit = false;
            StartCoroutine(RechargeHitColldown());

            Vector3 targ = slamPos;
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
            if (pigeonUpgrades.TryGetValue(Upgrades.critcalDamage, out bool _)) atkProp.hasCriticalDamage = true;
            if (pigeonUpgrades.TryGetValue(Upgrades.knockBack, out bool _)) atkProp.hasKnockBack = true;
            PigeonAttack(atkProp, theAngle);
        }
        if (targetPigeon && targetPigeon.currentHP.Value - damage * 3 <= 0 && !gm.isSuddenDeath)
        {
            StartSlam(targetPigeon.transform.position);

            Vector2 direction = (targetPigeon.transform.position - transform.position).normalized;
            CheckDirection(direction);
            body.AddForce(speed * Time.deltaTime * direction);
        }
        else
        {
            if (targetPigeon && currentHP.Value < maxHp.Value / 2)
            {
                if (distanceToPigeon <= 9)
                {
                    //runs away when another pigeon is near and on less than half hp
                    Vector2 direction = (transform.position - targetPigeon.transform.position).normalized;
                    CheckDirection(direction);
                    body.AddForce(speed * Time.deltaTime * direction);
                }
                else if (targetFood)
                {
                    //goes to neaby food item 
                    StartSlam(targetFood.transform.position);

                    Vector2 direction = (targetFood.transform.position - transform.position).normalized;
                    CheckDirection(direction);
                    body.AddForce(speed * Time.deltaTime * direction);
                }
            }
            else
            {
                if (targetPigeon && targetFood && distanceToFood < distanceToPigeon)
                {
                    //goes to neaby food item 
                    Vector2 direction = (targetFood.transform.position - transform.position).normalized;
                    CheckDirection(direction);
                    body.AddForce(speed * Time.deltaTime * direction);
                }
                else if (targetPigeon && targetPigeon.currentHP.Value - damage * 8 > 0)
                {
                    StartSlam(targetPigeon.transform.position);
                    Vector2 direction = (targetPigeon.transform.position - transform.position).normalized;
                    CheckDirection(direction);
                    body.AddForce(speed * Time.deltaTime * direction);
                }
                else if (targetFood)
                {
                    //goes to neaby food item 
                    Vector2 direction = (targetFood.transform.position - transform.position).normalized;
                    CheckDirection(direction);
                    body.AddForce(speed * Time.deltaTime * direction);
                }
                else if (targetPigeon)
                {
                    StartSlam(targetPigeon.transform.position);
                    Vector2 direction = (targetPigeon.transform.position - transform.position).normalized;
                    CheckDirection(direction);
                    body.AddForce(speed * Time.deltaTime * direction);
                }
            }
        }
    }
}
