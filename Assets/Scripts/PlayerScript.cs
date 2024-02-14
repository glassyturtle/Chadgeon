using UnityEngine;

public class PlayerScript : Pigeon
{
    private void Start()
    {
        OnPigeonSpawn();
        if (!IsOwner) return;
        gm.player = this;
        gm.mainCamera.Follow = transform;
    }
    private void HandleMovement()
    {
        Vector2 inputVector = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")).normalized;
        HandleMovement(inputVector);
    }

    private void HandleMovement(Vector2 inputVector)
    {
        if (isFlying.Value)
        {
            Vector2 direction = (slamPos - transform.position).normalized;
            body.AddForce(4 * speed * Time.fixedDeltaTime * direction);
            if ((transform.position - slamPos).sqrMagnitude <= 0.1f)
            {
                StopFlying();
            }
        }
        else if (!isKnockedOut.Value && !isSlaming.Value)
        {
            //Store user input as a movement vector
            if (Input.GetKey(KeyCode.LeftShift) && (stamina >= maxStamina || isSprinting.Value == true))
            {
                if (stamina <= 0)
                {
                    isSprinting.Value = false;
                    stamina = 0;
                    return;
                }
                isSprinting.Value = true;
                body.AddForce(speed * 2 * Time.fixedDeltaTime * inputVector);
                stamina -= Time.fixedDeltaTime;
            }
            else
            {
                body.AddForce(speed * Time.fixedDeltaTime * inputVector);
                if (stamina < maxStamina)
                {
                    isSprinting.Value = false;
                    stamina += Time.fixedDeltaTime * staminaRecoveryRate * 0.5f;
                    if (stamina > maxStamina) stamina = maxStamina;
                }
            }

            if (canSwitchAttackSprites.Value) CheckDirection(inputVector);
        }
        else if (isSlaming.Value)
        {
            Vector2 direction = (slamPos - transform.position).normalized;
            if (!canSwitchAttackSprites.Value) CheckDirection(direction);
            body.AddForce(4 * speed * Time.fixedDeltaTime * direction * speedMod);
            if ((transform.position - slamPos).sqrMagnitude <= 0.1f)
            {
                EndSlam();
            }
        }
    }

    private void Update()
    {
        SyncPigeonAttributes();
        if (!IsOwner) return;
        if (!isKnockedOut.Value && !isSlaming.Value)
        {

            if (Input.GetMouseButton(0) && !isSprinting.Value && punchCooldown <= 0)
            {
                punchCooldown = 0.3f;

                Vector2 pos = transform.position;
                pos = Vector2.MoveTowards(pos, Camera.main.ScreenToWorldPoint(Input.mousePosition), 0.5f);

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
                    isEnchanted = false,
                    isAssassin = false,
                    hasKnockBack = false,
                    posX = pos.x,
                    posY = pos.y,
                };
                if (pigeonUpgrades.TryGetValue(Upgrades.critcalDamage, out bool _)) atkProp.hasCriticalDamage = true;
                if (pigeonUpgrades.TryGetValue(Upgrades.brawler, out _)) atkProp.hasKnockBack = true;
                if (pigeonUpgrades.TryGetValue(Upgrades.assassin, out _)) atkProp.isAssassin = true;
                if (pigeonUpgrades.TryGetValue(Upgrades.enchanted, out _)) atkProp.isEnchanted = true;


                PigeonAttack(atkProp, theAngle);
            }
            else if (Input.GetKeyDown(KeyCode.Space) && canSlam)
            {
                StartSlam(Camera.main.ScreenToWorldPoint(Input.mousePosition));
            }

            if (punchCooldown > 0)
            {
                punchCooldown -= Time.deltaTime;
            }
            else
            {
                punchCooldown = 0;
            }
        }
    }
    private void FixedUpdate()
    {
        if (!IsOwner) return;
        HandleMovement();
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

}
