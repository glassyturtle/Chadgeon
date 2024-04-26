using Unity.Netcode;
using UnityEngine;

public class PlayerScript : Pigeon
{
    [SerializeField] private GameObject playerMinimapIcon;
    private void Start()
    {
        OnPigeonSpawn();
        if (!IsOwner) return;
        GameManager.instance.player = this;
        playerMinimapIcon.SetActive(true);
        GameManager.instance.mainCamera.Follow = transform;
    }
    private void HandleMovement()
    {
        Vector2 inputVector = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")).normalized;
        HandleMovement(inputVector);
    }
    [ServerRpc]
    private void MoveServerRpc(float speed, Vector2 direction)
    {
        body.AddForce(speed * direction);
    }
    private void HandleMovement(Vector2 inputVector)
    {
        if (isMewing) return;
        if (isFlying)
        {
            Vector2 direction = (abilityTargetPos - transform.position).normalized;
            MoveServerRpc(6 * speed * speedMod * Time.fixedDeltaTime, direction);
            if ((transform.position - abilityTargetPos).sqrMagnitude <= 0.05f)
            {
                StopFlying();
            }
            return;
        }
        else if (!isKnockedOut.Value && !isSlaming)
        {
            //Store user input as a movement vector
            if (Input.GetKey(KeyCode.LeftShift) && stamina > 0)
            {
                if (stamina <= 0)
                {
                    stamina = 0;
                    return;
                }
                sprintOnCooldown = true;
                isSprinting = true;
                MoveServerRpc(speed * 2 * Time.fixedDeltaTime * speedMod, inputVector);

                stamina -= Time.fixedDeltaTime;
            }
            else
            {
                if (isSprinting == true)
                {
                    isSprinting = false;
                    StartCoroutine(StartSprintCooldown());
                }
                MoveServerRpc(speed * Time.fixedDeltaTime * speedMod, inputVector);
                if (stamina < maxStamina && !sprintOnCooldown)
                {
                    stamina += Time.fixedDeltaTime * staminaRecoveryRate * 0.5f;
                    if (stamina > maxStamina) stamina = maxStamina;
                }
            }

            if (canSwitchAttackSprites) CheckDirection(inputVector);
        }
        else if (isSlaming)
        {
            Vector2 direction = (abilityTargetPos - transform.position).normalized;
            if (!canSwitchAttackSprites) CheckDirection(direction);
            MoveServerRpc(4 * speed * Time.fixedDeltaTime * speedMod, direction);

            if ((transform.position - abilityTargetPos).sqrMagnitude <= 2.5f)
            {
                EndSlam();
            }
        }
    }

    private void Update()
    {
        SyncPigeonAttributes();
        if (!IsOwner || GameManager.instance.currentSecond.Value == -1) return;
        if (!isKnockedOut.Value && !isSlaming && !isMewing)
        {
            if ((Input.GetMouseButton(0) || (Input.GetMouseButton(1)) && pigeonUpgrades.ContainsKey(Upgrades.razorFeathers)) && !isSprinting && hitColldown <= 0 && !isSlaming)
            {
                hitColldown = 0.3f;

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
                if (isMaxing) atkProp.damage *= 2;
                if (pigeonUpgrades.TryGetValue(Upgrades.brawler, out _)) atkProp.hasKnockBack = true;
                if (pigeonUpgrades.TryGetValue(Upgrades.assassin, out _)) atkProp.isAssassin = true;
                if (pigeonUpgrades.TryGetValue(Upgrades.enchanted, out _)) atkProp.isEnchanted = true;

                if (Input.GetMouseButton(1) && pigeonUpgrades.ContainsKey(Upgrades.razorFeathers) && chargedFeathers > 0) PigeonThrow(atkProp, theAngle);
                else PigeonAttack(atkProp, theAngle);

            }
            else if (Input.GetKeyDown(KeyCode.Mouse1) && pigeonUpgrades.ContainsKey(Upgrades.slam) && m2AbilityCooldown == 0)
            {
                StartSlam(Camera.main.ScreenToWorldPoint(Input.mousePosition));
            }
            else if (Input.GetKeyDown(KeyCode.Q) && pigeonUpgrades.ContainsKey(Upgrades.mewing))
            {
                if (qAbilityCooldown == 0 && !isMaxing)
                {
                    StartMewing();

                }
                else if (isMaxing)
                {
                    StopMogging();
                }
            }
            else if (Input.GetKeyDown(KeyCode.E) && pigeonUpgrades.ContainsKey(Upgrades.wholeGains))
            {
                SummonWholeGain(Camera.main.ScreenToWorldPoint(Input.mousePosition));
            }

            if (hitColldown > 0)
            {
                hitColldown -= Time.deltaTime;
            }
            else
            {
                hitColldown = 0;
            }
        }
    }
    private void FixedUpdate()
    {
        if (!IsOwner || GameManager.instance.currentSecond.Value == -1) return;
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
