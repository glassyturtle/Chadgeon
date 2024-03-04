using Pathfinding;
using System;
using System.Collections;
using UnityEngine;

public class PigeonAI : Pigeon
{

    [SerializeField] protected Pigeon targetPigeon;
    [SerializeField] protected food targetFood;
    [SerializeField] private float nextWaypointDistance = 3f;

    // IAstarAI ai;
    //AI configurations
    public Path path;
    private Seeker seeker;
    private int currentWaypoint = 0;

    protected bool canHit = true;

    private Action<float, float> behaviorAI;
    private bool isFleeing = false;
    bool isPathfinding = false;
    Vector2 locationToPathfindTo;


    public void SetAI(int difficulty)
    {
        switch (difficulty)
        {
            case 0:
                behaviorAI = SimpBehavior;
                pigeonName.Value = "Simp";
                break;
            case 1:
                behaviorAI = ChadBehavior;
                pigeonName.Value = "Chad";
                break;
            case 2:
                behaviorAI = SigmaBehavior;
                pigeonName.Value = "Sigma";
                break;
        }
    }


    private void Start()
    {
        OnPigeonSpawn();
        seeker = GetComponent<Seeker>();

        InvokeRepeating("UpdatePath", 0f, .2f);
    }
    private void Update()
    {
        FindNearbyPigeon();
        FindNearbyFood();
        SyncPigeonAttributes();
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;

        Vector2 direction;

        isPathfinding = false;

        if (isFlying.Value)
        {
            direction = (slamPos - transform.position).normalized;
            body.AddForce(4 * speed * Time.fixedDeltaTime * direction);
            if ((transform.position - slamPos).sqrMagnitude <= 0.1f)
            {
                StopFlying();
            }
        }


        if (!isKnockedOut.Value)
        {
            if (!isSlaming.Value)
            {
                isPathfinding = true;
                float distanceToPigeon = Mathf.Infinity;
                float distanceToFood = Mathf.Infinity;
                if (targetPigeon) distanceToPigeon = (targetPigeon.transform.position - transform.position).sqrMagnitude;
                if (targetFood) distanceToFood = (targetFood.transform.position - transform.position).sqrMagnitude;

                behaviorAI?.Invoke(distanceToPigeon, distanceToFood);
            }
            else if (isSlaming.Value)
            {
                direction = (slamPos - transform.position).normalized;
                CheckDirection(direction);
                body.AddForce(speed * 4 * Time.deltaTime * speedMod * direction);
                if ((transform.position - slamPos).sqrMagnitude <= 2.5f)
                {
                    EndSlam();
                }
            }
        }

        if (isSprinting.Value)
        {
            stamina -= Time.fixedDeltaTime;
            if (stamina <= 0)
            {
                stamina = 0;
                AIStopSprinting();
            }
        }
        else
        {
            stamina += Time.fixedDeltaTime * staminaRecoveryRate * 0.5f;
            if (stamina > maxStamina) stamina = maxStamina;
        }

        //Pathfinding
        if (!isPathfinding || path == null)
        {
            path = null;
            return;
        }

        Vector3 nextPoint;
        if (currentWaypoint >= path.vectorPath.Count)
        {
            if (isSprinting.Value)
            {
                nextPoint = Vector3.MoveTowards(body.position, locationToPathfindTo, speed * speedMod * 2 * Time.deltaTime);
            }
            else
            {
                nextPoint = Vector3.MoveTowards(body.position, locationToPathfindTo, speed * speedMod * Time.deltaTime);
            }
        }
        else
        {
            nextPoint = Vector3.MoveTowards(body.position, path.vectorPath[currentWaypoint], speed * speedMod * Time.deltaTime);
            float distance = Vector2.Distance(body.position, path.vectorPath[currentWaypoint]);

            if (distance < nextWaypointDistance)
            {
                currentWaypoint++;
            }
        }
        direction = (nextPoint - transform.position).normalized;
        CheckDirection(direction);
        Vector3 force;
        if (isSprinting.Value)
        {
            force = speed * speedMod * Time.deltaTime * 2 * direction;

        }
        else
        {
            force = speed * speedMod * Time.deltaTime * direction;
        }
        body.AddForce(force);
    }

    public void AILevelUP()
    {
        //ai.maxSpeed = speed * speedMod;
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
            if (allPigeons[i] != this && currentDist < distToCloset && !allPigeons[i].isKnockedOut.Value && !allPigeons[i].isFlying.Value)
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


    private void AIAttack()
    {
        canHit = false;
        StartCoroutine(RechargeHitColldown());

        Vector2 pos = transform.position;
        pos = Vector2.MoveTowards(pos, targetPigeon.transform.position, 0.5f);

        Vector3 targ = pos;
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
            posX = pos.x,
            posY = pos.y,
        }; if (pigeonUpgrades.TryGetValue(Upgrades.critcalDamage, out bool a)) atkProp.hasCriticalDamage = true;
        if (pigeonUpgrades.TryGetValue(Upgrades.brawler, out bool d)) atkProp.hasKnockBack = true;
        PigeonAttack(atkProp, theAngle);
    }
    private void AIStartSprint()
    {
        if (sprintOnCooldown || stamina <= 0) return;
        isSprinting.Value = true;
    }
    private void AIStopSprinting()
    {
        if (!isSprinting.Value) return;
        isSprinting.Value = false;
        StartCoroutine(StartSprintCooldown());
    }
    private void SimpBehavior(float distanceToPigeon, float distanceToFood)
    {
        if (isFleeing)
        {
            AIStartSprint();
            if (distanceToPigeon >= 25 || currentHP.Value >= maxHp.Value / 2)
            {
                AIStopSprinting();
                isFleeing = false;
            }
        }
        else
        {
            if (targetPigeon && currentHP.Value < maxHp.Value / 2)
            {

                if (distanceToPigeon <= 15)
                {
                    //runs away when another pigeon is near and on less than half hp
                    isFleeing = true;
                }
                else if (targetFood)
                {
                    //goes to neaby food item 
                    locationToPathfindTo = targetFood.transform.position;
                }
            }
            else
            {
                isFleeing = false;
                if (targetPigeon && targetFood && distanceToFood < distanceToPigeon * 2)
                {
                    //goes to neaby food item 
                    locationToPathfindTo = targetFood.transform.position;
                }
                else if (targetPigeon)
                {
                    if (canHit && distanceToPigeon <= 2f)
                    {
                        AIAttack();
                    }
                    else
                    {
                        StartSlam(targetPigeon.transform.position);
                        locationToPathfindTo = targetPigeon.transform.position;
                    }
                }
            }
        }


    }
    private void ChadBehavior(float distanceToPigeon, float distanceToFood)
    {

        if (canHit && distanceToPigeon <= 3f && !isSprinting.Value)
        {
            AIAttack();
        }
        if (targetPigeon && currentHP.Value < maxHp.Value / 2)
        {
            if (distanceToPigeon <= 12)
            {
                //runs away when another pigeon is near and on less than half hp
                isFleeing = true;
            }
            else if (targetFood)
            {
                StartSlam(targetFood.transform.position);
                //goes to neaby food item 
                locationToPathfindTo = targetFood.transform.position;
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

            canHit = false;
            StartCoroutine(RechargeHitColldown());
            Vector2 pos = transform.position;
            pos = Vector2.MoveTowards(pos, targetPigeon.transform.position, 0.5f);

            Vector3 targ = pos;
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
                posX = pos.x,
                posY = pos.y,
            };
            if (pigeonUpgrades.TryGetValue(Upgrades.critcalDamage, out bool _)) atkProp.hasCriticalDamage = true;
            if (pigeonUpgrades.TryGetValue(Upgrades.brawler, out bool _)) atkProp.hasKnockBack = true;
            PigeonAttack(atkProp, theAngle);
        }
        if (targetPigeon && targetPigeon.currentHP.Value - damage * 3 <= 0 && !gm.isSuddenDeath.Value)
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

    public float test;
    private void UpdatePath()
    {

        if (seeker.IsDone() && isPathfinding)
        {
            if (isFleeing)
            {
                // Call a FleePath call like this, assumes that a Seeker is attached to the GameObject
                Vector3 thePointToFleeFrom = targetPigeon.transform.position;

                // The path will be returned when the path is over a specified length (or more accurately when the traversal cost is greater than a specified value).
                // A score of 1000 is approximately equal to the cost of moving one world unit.
                int theGScoreToStopAt = 10000;

                // Create a path object
                FleePath path = FleePath.Construct(transform.position, thePointToFleeFrom, theGScoreToStopAt);
                // This is how strongly it will try to flee, if you set it to 0 it will behave like a RandomPath
                path.aimStrength = 1;
                // Determines the variation in path length that is allowed
                path.spread = 3000;

                // Start the path and return the result to MyCompleteFunction (which is a function you have to define, the name can of course be changed)

                test = Time.time;
                seeker.StartPath(path, OnPathComplete);
            }
            else
            {
                seeker.StartPath(body.position, locationToPathfindTo, OnPathComplete);
            }
        }






    }

    private void OnPathComplete(Path p)
    {
        if (!p.error)
        {
            path = p;
            currentWaypoint = 0;
        }
    }
}
