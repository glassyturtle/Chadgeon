using Pathfinding;
using System;
using System.Collections;
using UnityEngine;

public class PigeonAI : Pigeon
{

    [SerializeField] protected food targetFood;
    [SerializeField] private float nextWaypointDistance = 3f;

    public bool diesAfterDeath = false;

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
    Vector3 iceCreamLocation;

    private FleePath fleePath;
    public void SetAI(int difficulty)
    {
        switch (difficulty)
        {
            case 0:
                behaviorAI = SimpBehavior;
                pigeonName = "Simp";
                break;
            case 1:
                behaviorAI = ChadBehavior;
                pigeonName = "Chad";
                break;
            case 2:
                behaviorAI = SigmaBehavior;
                pigeonName = "Sigma";
                break;
            case 3:
                behaviorAI = PVEBehavior;
                pigeonName = "Goon";
                isFleeing = false;
                if (!GameManager.instance.isSuddenDeath.Value) iceCreamLocation = FindFirstObjectByType<BuiltConeScript>().transform.position;
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
        if (GameManager.instance.currentSecond.Value == -1) return;
        FindNearbyPigeon();
        FindNearbyFood();
        SyncPigeonAttributes();
    }

    private void FixedUpdate()
    {
        if (!IsOwner || GameManager.instance.currentSecond.Value == -1) return;

        Vector2 direction;

        isPathfinding = false;

        if (isFlying)
        {
            direction = (abilityTargetPos - transform.position).normalized;
            body.AddForce(6 * speed * speedMod * Time.fixedDeltaTime * direction);
            if ((transform.position - abilityTargetPos).sqrMagnitude <= 0.05f)
            {
                StopFlying();
            }
            return;
        }

        if (isMewing) return;
        if (!isKnockedOut.Value)
        {
            if (!isSlaming && !isAssassinating)
            {
                isPathfinding = true;
                float distanceToPigeon = Mathf.Infinity;
                float distanceToFood = Mathf.Infinity;
                if (targetPigeon) distanceToPigeon = (targetPigeon.transform.position - transform.position).sqrMagnitude;
                if (targetFood) distanceToFood = (targetFood.transform.position - transform.position).sqrMagnitude;

                behaviorAI?.Invoke(distanceToPigeon, distanceToFood);
            }
            else if (isSlaming)
            {
                direction = (abilityTargetPos - transform.position).normalized;
                CheckDirection(direction);
                body.AddForce(speed * 4 * Time.deltaTime * speedMod * direction);
                if ((transform.position - abilityTargetPos).sqrMagnitude <= 2.5f)
                {
                    EndSlam();
                }
            }
            else if (isAssassinating)
            {
                direction = (targetPigeon.transform.position - transform.position).normalized;
                if (!canSwitchAttackSprites) CheckDirection(direction);
                body.AddForce(4 * speed * Time.fixedDeltaTime * speedMod * direction);
                if ((transform.position - targetPigeon.transform.position).sqrMagnitude <= 2.5f && !targetPigeon.isKnockedOut.Value)
                {
                    LandAssassinate();
                }
            }
        }

        if (isSprinting)
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
            if (isSprinting)
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

            if (distance < nextWaypointDistance && distance <= 0.15f)
            {
                currentWaypoint++;
            }
        }
        direction = (nextPoint - transform.position).normalized;
        CheckDirection(direction);
        Vector3 force;
        if (isSprinting && !inPoo)
        {
            force = speed * speedMod * Time.deltaTime * 2 * direction;
        }
        else
        {
            if (inPoo) force = speed * speedMod * Time.deltaTime * 0.2f * direction;
            else force = speed * speedMod * Time.deltaTime * direction;
        }
        body.AddForce(force);
    }


    protected IEnumerator RechargeHitColldown()
    {
        yield return new WaitForSeconds(hitColldown);
        canHit = true;
    }
    protected void FindNearbyPigeon()
    {
        Pigeon[] allPigeons = FindObjectsByType<Pigeon>(FindObjectsSortMode.None);
        Pigeon closestPigeon = null;
        float distToCloset = Mathf.Infinity;
        for (int i = 0; i < allPigeons.Length; i++)
        {
            float currentDist = Vector2.SqrMagnitude(allPigeons[i].transform.position - transform.position);
            if (allPigeons[i] != this && currentDist < distToCloset && !allPigeons[i].isKnockedOut.Value && !allPigeons[i].isFlying && (allPigeons[i].flock == 0 || allPigeons[i].flock != flock))
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
        food[] allPigeons = FindObjectsByType<food>(FindObjectsSortMode.None);
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
        AIAttack(targetPigeon.transform.position);
    }
    private void AIAttack(Vector3 attackPos)
    {
        canHit = false;
        StartCoroutine(RechargeHitColldown());

        Vector2 pos = transform.position;
        pos = Vector2.MoveTowards(pos, attackPos, 0.5f);

        Vector3 targ = pos;
        targ.z = 0f;
        targ.x -= transform.position.x;
        targ.y -= transform.position.y;

        float angle = Mathf.Atan2(targ.y, targ.x) * Mathf.Rad2Deg;
        Quaternion theAngle = Quaternion.Euler(new Vector3(0, 0, angle));

        PigeonAttack(GetBasicAttackValues(pos.x, pos.y), theAngle);
    }
    private void AIThrow()
    {
        if (chargedFeathers <= 0 || !targetPigeon) return;
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

        PigeonThrow(GetBasicAttackValues(pos.x, pos.y), theAngle);
    }
    private void AIStartSprint()
    {
        if (sprintOnCooldown || stamina <= 0 || inPoo) return;
        isSprinting = true;
    }
    private void AIStopSprinting()
    {
        if (!isSprinting) return;
        isSprinting = false;
        StartCoroutine(StartSprintCooldown());
    }
    private void SimpBehavior(float distanceToPigeon, float distanceToFood)
    {
        //Flees when less than half health
        if (isFleeing)
        {
            //Starts sprinting if has a certain amount of stamina
            if (stamina == maxStamina) AIStartSprint();
            SummonPigeonPoo(transform.position);
            SummonWholeGain(transform.position);
            if (distanceToPigeon >= 25 || currentHP.Value > targetPigeon.currentHP.Value * 2)
            {
                AIStopSprinting();
                isFleeing = false;
            }
        }
        else
        {
            Assassinate();
            StartMewing();
            SummonWholeGain(transform.position);
            SummonPigeonPoo(transform.position);
            if (targetPigeon && currentHP.Value < targetPigeon.currentHP.Value * 1.2f)
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
                else
                {
                    locationToPathfindTo = GameManager.instance.transform.position;
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
                        if (canHit) AIThrow();
                        StartSlam(targetPigeon.transform.position);
                        locationToPathfindTo = targetPigeon.transform.position;
                    }
                }
                else
                {
                    locationToPathfindTo = GameManager.instance.transform.position;
                }
            }
        }


    }
    private void ChadBehavior(float distanceToPigeon, float distanceToFood)
    {
        if (isFleeing)
        {
            SummonPigeonPoo(transform.position);

            if (stamina > maxStamina / 2) AIStartSprint();

            if (distanceToPigeon >= 25 || currentHP.Value >= targetPigeon.currentHP.Value)
            {
                if (canHit) AIThrow();
                AIStopSprinting();
                isFleeing = false;
            }
            else if (canHit && distanceToPigeon <= 6f && !isSprinting)
            {
                AIAttack();
            }
        }
        else
        {
            if (canHit && distanceToPigeon <= 6f)
            {
                AIStopSprinting();
                AIAttack();
            }
            if (targetPigeon && currentHP.Value < targetPigeon.currentHP.Value)
            {
                if (distanceToPigeon <= 12)
                {
                    //runs away when another pigeon is near and on less than half hp
                    isFleeing = true;
                }
                else if (targetFood)
                {
                    //goes to neaby food item 
                    locationToPathfindTo = targetFood.transform.position;
                }
                else
                {
                    locationToPathfindTo = GameManager.instance.transform.position;
                }
            }
            else
            {
                if (targetPigeon && targetFood && distanceToFood < distanceToPigeon * 1.5f)
                {
                    //goes to neaby food item 
                    SummonWholeGain(transform.position);
                    if (stamina == maxStamina) AIStartSprint();
                    else if (stamina <= maxStamina / 2) AIStopSprinting();
                    StartSlam(targetFood.transform.position);
                    locationToPathfindTo = targetFood.transform.position;

                }
                else if (targetPigeon)
                {
                    Assassinate();
                    StartMewing();
                    if (distanceToPigeon >= 5 && stamina == maxStamina) AIStartSprint();
                    else if (stamina <= maxStamina / 2) AIStopSprinting();
                    if (canHit) AIThrow();
                    StartSlam(targetPigeon.transform.position);
                    locationToPathfindTo = targetPigeon.transform.position;
                }
                else
                {
                    locationToPathfindTo = GameManager.instance.transform.position;
                }
            }
        }

    }
    private void SigmaBehavior(float distanceToPigeon, float distanceToFood)
    {
        if (distanceToPigeon <= 20 && targetPigeon.currentHP.Value - damage <= 0)
        {
            //Focuses nearby pigeon if 1 hit
            isFleeing = false;
            if (distanceToPigeon <= 16)
            {
                if (canHit) AIThrow();
                StartSlam(targetPigeon.transform.position);
            }
            if (distanceToPigeon <= 2.5f)
            {
                AIStopSprinting();
                if (canHit) AIAttack();
            }
            else if (distanceToPigeon >= 6 && stamina == maxStamina) AIStartSprint();

            locationToPathfindTo = targetPigeon.transform.position;
        }
        else
        {
            if (isFleeing)
            {
                if (fleePath != null)
                {
                    SummonPigeonPoo(transform.position);
                    if ((transform.position - fleePath.endPoint).sqrMagnitude > 140) StartSlam(fleePath.endPoint);
                }


                if (canHit && distanceToPigeon <= 4f)
                {
                    AIStopSprinting();
                    AIAttack();
                }
                else if (distanceToPigeon >= 30 || currentHP.Value >= targetPigeon.currentHP.Value * 1.3f)
                {
                    AIStopSprinting();
                    isFleeing = false;
                }
                else if (distanceToPigeon >= 20)
                {
                    AIStopSprinting();
                }
                else if (stamina > maxStamina / 2) AIStartSprint();

            }
            else
            {
                if (targetPigeon && canHit && distanceToPigeon <= 4)
                {
                    AIStopSprinting();
                    //If runs into another pigeon will fight them
                    AIAttack();
                }


                if (targetPigeon && currentHP.Value > targetPigeon.currentHP.Value * 1.2f && targetPigeon.currentHP.Value - damage * 3 <= 0 && (distanceToPigeon <= 30 || distanceToFood > distanceToPigeon))
                {
                    //Goes to the nearby pigeon if stronger than the other pigeon
                    Assassinate();
                    if (distanceToPigeon >= 6 && stamina == maxStamina) AIStartSprint();
                    else if (stamina <= maxStamina / 2 || distanceToPigeon <= 2.5f) AIStopSprinting();
                    if (distanceToPigeon <= 16)
                    {
                        StartMewing();
                        if (canHit) AIThrow();
                        StartSlam(targetPigeon.transform.position);
                    }
                    locationToPathfindTo = targetPigeon.transform.position;
                }
                else
                {
                    if (targetPigeon && currentHP.Value < targetPigeon.currentHP.Value * 0.6f && distanceToPigeon <= 25)
                    {
                        //Flees if can die in three hits
                        isFleeing = true;
                    }
                    else
                    {
                        if (targetFood)
                        {
                            //goes to neaby food item 
                            SummonWholeGain(transform.position);
                            if (stamina == maxStamina) AIStartSprint();
                            else if (stamina <= maxStamina / 2) AIStopSprinting();
                            if (distanceToFood <= 20 && distanceToFood >= 10) StartSlam(targetFood.transform.position);
                            locationToPathfindTo = targetFood.transform.position;
                        }
                        else if (targetPigeon)
                        {
                            locationToPathfindTo = targetPigeon.transform.position;
                        }
                        else
                        {
                            locationToPathfindTo = GameManager.instance.transform.position;
                        }
                    }
                }
            }
        }



    }
    private void PVEBehavior(float distanceToPigeon, float distanceToFood)
    {
        if (GameManager.instance.gameover) return;
        float distanceToIceCream = Vector2.SqrMagnitude(transform.position - iceCreamLocation);

        Assassinate();
        StartMewing();
        SummonWholeGain(transform.position);
        SummonPigeonPoo(transform.position);

        if (GameManager.instance.isSuddenDeath.Value)
        {
            if (targetPigeon && targetFood && distanceToFood < distanceToPigeon * 2)
            {
                //goes to neaby food item 
                if (stamina == maxStamina) AIStartSprint();
                locationToPathfindTo = targetFood.transform.position;
            }
            else if (targetPigeon)
            {
                if (distanceToPigeon >= 6 && stamina == maxStamina) AIStartSprint();

                if (canHit && distanceToPigeon <= 5f)
                {
                    AIStopSprinting();
                    AIAttack();
                }
                else
                {
                    if (canHit) AIThrow();
                    StartSlam(targetPigeon.transform.position);
                    locationToPathfindTo = targetPigeon.transform.position;
                }
            }
        }
        else
        {
            if (targetPigeon && targetFood && distanceToFood < distanceToPigeon * 2 && distanceToFood < distanceToIceCream)
            {
                //goes to neaby food item 
                if (stamina == maxStamina) AIStartSprint();
                locationToPathfindTo = targetFood.transform.position;
            }
            else if (targetPigeon && distanceToPigeon < distanceToIceCream * 4)
            {
                if (distanceToPigeon >= 6 && stamina == maxStamina) AIStartSprint();

                if (canHit && distanceToPigeon <= 5f)
                {
                    AIStopSprinting();
                    AIAttack();
                }
                else
                {
                    if (canHit) AIThrow();
                    StartSlam(targetPigeon.transform.position);
                    locationToPathfindTo = targetPigeon.transform.position;
                }
            }
            else
            {
                if (canHit && distanceToIceCream <= 5f)
                {
                    AIStopSprinting();
                    AIAttack(iceCreamLocation);
                    GameManager.instance.DamageCone();
                }
                else
                {
                    if (canHit) AIThrow();
                    StartSlam(iceCreamLocation);
                    locationToPathfindTo = iceCreamLocation;
                }
            }
        }

    }

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
                fleePath = FleePath.Construct(transform.position, thePointToFleeFrom, theGScoreToStopAt);
                // This is how strongly it will try to flee, if you set it to 0 it will behave like a RandomPath
                fleePath.aimStrength = 1;
                // Determines the variation in path length that is allowed
                fleePath.spread = 3000;
                // Start the path and return the result to MyCompleteFunction (which is a function you have to define, the name can of course be changed)
                seeker.StartPath(fleePath, OnPathComplete);
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

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("banish"))
        {
            transform.position = Vector3.zero;
        }
        if (collision.CompareTag("Border"))
        {
            inBorder = true;
        }

    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Border"))
        {
            inBorder = false;
        }
    }
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Poo") && !pigeonUpgrades.ContainsKey(Upgrades.pigeonPoo))
        {
            inPoo = true;
        }
        else
        {
            inPoo = false;
        }
    }
}
