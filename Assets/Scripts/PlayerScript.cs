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
    [ServerRpc]
    private void FlyToRespawnServerRpc(float speed, Vector3 respawnPoint)
    {
        if ((transform.position - respawnPoint).sqrMagnitude <= 0.02f)
        {
            transform.position = body.velocity = Vector2.zero;
            transform.position = respawnPoint;
        }
        else
        {
            Vector2 direction = (respawnPoint - transform.position).normalized;
            body.AddForce(speed * direction);
        }
    }
    private void HandleMovement(Vector2 inputVector)
    {
        if (isMewing) return;
        if (isFlying)
        {
            FlyToRespawnServerRpc(6 * speed * speedMod * Time.fixedDeltaTime, abilityTargetPos);
            if ((transform.position - abilityTargetPos).sqrMagnitude <= 0.05f)
            {
                StopFlying();
            }
            return;
        }
        else if (isAssassinating)
        {
            if (targetPigeon == null) LandAssassinate();
            Vector2 direction = (targetPigeon.transform.position - transform.position).normalized;
            if (!canSwitchAttackSprites) CheckDirection(direction);
            MoveServerRpc(4 * speed * Time.fixedDeltaTime * speedMod, direction);
            //
            if ((transform.position - targetPigeon.transform.position).sqrMagnitude <= 2.5f && !targetPigeon.isKnockedOut.Value)
            {
                LandAssassinate();
            }
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
        else if (!isKnockedOut.Value)
        {
            //Store user input as a movement vector
            if (Input.GetKey(KeyCode.LeftShift) && stamina > 0 && !inPoo)
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
                if (inPoo) MoveServerRpc((speed * Time.fixedDeltaTime * speedMod) * 0.2f, inputVector);
                else MoveServerRpc(speed * Time.fixedDeltaTime * speedMod, inputVector);

                if (stamina < maxStamina && !sprintOnCooldown)
                {
                    stamina += Time.fixedDeltaTime * staminaRecoveryRate * 0.5f;
                    if (stamina > maxStamina) stamina = maxStamina;
                }
            }

            if (canSwitchAttackSprites) CheckDirection(inputVector);
        }
    }

    private void Update()
    {
        SyncPigeonAttributes();
        if (!IsOwner || GameManager.instance.currentSecond.Value == -1) return;
        if (!isKnockedOut.Value && !isSlaming && !isAssassinating && !isMewing)
        {
            if ((Input.GetMouseButton(0) || (Input.GetMouseButton(1)) && pigeonUpgrades.ContainsKey(Upgrades.razorFeathers)) && !isSprinting && hitColldown <= 0)
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

                if (Input.GetMouseButton(1) && pigeonUpgrades.ContainsKey(Upgrades.razorFeathers) && chargedFeathers > 0) PigeonThrow(GetBasicAttackValues(pos.x, pos.y), theAngle);
                else PigeonAttack(GetBasicAttackValues(pos.x, pos.y), theAngle);

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
            else if (Input.GetKeyDown(KeyCode.Q) && pigeonUpgrades.ContainsKey(Upgrades.hiddinTalon))
            {
                Assassinate();
            }
            else if (Input.GetKeyDown(KeyCode.E))
            {
                if (pigeonUpgrades.ContainsKey(Upgrades.wholeGains))
                {
                    SummonWholeGain(Camera.main.ScreenToWorldPoint(Input.mousePosition));
                }
                else if (pigeonUpgrades.ContainsKey(Upgrades.pigeonPoo))
                {
                    SummonPigeonPoo(transform.position);
                }
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
        if (collision.CompareTag("Evac"))
        {
            inEvacsite = true;
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
        if (collision.CompareTag("Evac"))
        {
            inEvacsite = false;
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
