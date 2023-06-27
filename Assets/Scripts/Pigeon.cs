using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Pigeon : NetworkBehaviour
{
    public NetworkVariable<bool> isKnockedOut = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> maxHp = new(50, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> currentHP = new(50, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> xp = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> xpTillLevelUp = new(20, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> level = new(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public Dictionary<Upgrades, bool> pigeonUpgrades = new();

    [SerializeField] protected GameManager gm;
    [SerializeField] protected string pigeonName;
    [SerializeField] protected TextMesh displayText;
    [SerializeField] protected Rigidbody2D body;
    [SerializeField] protected CircleCollider2D bodyCollider;
    [SerializeField] protected float speed;
    [SerializeField] protected NetworkObject no;
    [SerializeField] protected int damage;


    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] hitClips;
    [SerializeField] private AudioClip[] cruchSound;
    [SerializeField] private GameObject bloodEffect;
    [SerializeField] private GameObject healthBarGameobject;
    [SerializeField] private Transform hpBar;
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Sprite defaultPigeonSprite, pigeonJumpSprite, pigeonSlamSprite;
    [SerializeField] private Sprite[] pigeonAttackSprites;
    [SerializeField] private bool isPlayer = false;
    [SerializeField] private PigeonAI pigeonAI;
    [SerializeField] private HitScript slash;

    protected int secTillSlam = 5, secTillFly = 15;
    protected bool canSlam = false, canfly = false, isSlaming, canDeCollide = false;
    protected Vector3 slamPos;

    private int regen = 3;
    private NetworkVariable<bool> isPointingLeft = new(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<bool> isSpriteNotHopping = new(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    protected NetworkVariable<bool> canSwitchAttackSprites = new(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<int> currentPigeonAttackSprite = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);


    public enum Upgrades
    {
        regen = 0,
        tough = 1,
        knockBack = 2,
        bulk = 3,
        lifeSteal = 4,
        dodge = 5,
        critcalDamage = 6,
        slam = 7,
        fly = 8,
    }
    public struct AttackProperties : INetworkSerializable
    {
        public ulong indexOfDamagingPigeon;
        public int damage;
        public bool hasCriticalDamage;
        public bool hasKnockBack;
        public bool attackingUp;
        public bool isFacingLeft;
        public float posX;
        public float posY;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref indexOfDamagingPigeon);
            serializer.SerializeValue(ref damage);
            serializer.SerializeValue(ref hasCriticalDamage);
            serializer.SerializeValue(ref hasKnockBack);
            serializer.SerializeValue(ref isFacingLeft);
            serializer.SerializeValue(ref posX);
            serializer.SerializeValue(ref posY);
            serializer.SerializeValue(ref attackingUp);
        }
    }
    public struct DealtDamageProperties : INetworkSerializable
    {
        public bool hasDied;
        public bool hasHit;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref hasDied);
            serializer.SerializeValue(ref hasHit);
        }
    }


    public void OnPigeonHit(AttackProperties atkProp)
    {
        //Stops calculating if successufly dodged
        if (pigeonUpgrades.ContainsKey(Upgrades.dodge) && Random.Range(0, 100) <= 30) return;

        //Gets the knockback direction of the hit
        Vector2 direction = transform.position - new Vector3(atkProp.posX, atkProp.posY);
        direction.Normalize();

        //Calculates total damage taken with modifiers
        int totalDamageTaking = atkProp.damage;
        if (isSlaming) totalDamageTaking /= 2;
        if (atkProp.hasCriticalDamage && Random.Range(0, 100) <= 10) totalDamageTaking *= 4;
        if (pigeonUpgrades.ContainsKey(Upgrades.tough)) totalDamageTaking = Mathf.RoundToInt(totalDamageTaking * 0.7f);
        currentHP.Value -= totalDamageTaking;

        //Calculates Life Steal

        //Calculates Knockback
        if (atkProp.hasKnockBack) body.AddForce(50 * totalDamageTaking * direction);
        else body.AddForce(10 * totalDamageTaking * direction);

        //Sound Effects and Blood
        SpawnBloodServerRpc();
        if (!audioSource.isPlaying)
        {
            audioSource.clip = hitClips[Random.Range(0, hitClips.Length)];
            audioSource.Play();
        }

        //Logic when pigeon has no health and is not already knocked out
        if (currentHP.Value > 0 || isKnockedOut.Value) return;
        isSlaming = false;
        StopCoroutine(StopSlam());

        isKnockedOut.Value = true;
        sr.sortingOrder = -1;
        StartCoroutine(Respawn());

        /*
        if (gm.isSuddenDeath)
        {
            if (this == gm.player)
            {
                gm.Lose();
                Destroy(gameObject);
            }
            else
            {
                gm.allpigeons.Remove(this);
                if (gm.allpigeons.Count == 1)
                    gm.Win();
                Destroy(gameObject);
            }
        }
        else
        {


        }
        */
    }
    public void GainXP(int amnt)
    {
        if (isKnockedOut.Value) return;
        xp.Value += amnt;
        if (xp.Value >= xpTillLevelUp.Value)
        {
            LevelUP();
        }
    }

    public void HealServer(int amt)
    {
        if (isKnockedOut.Value) return;
        currentHP.Value += amt;
        if (currentHP.Value > maxHp.Value) currentHP.Value = maxHp.Value;

    }


    public void OnDealtDamage(DealtDamageProperties ddProp)
    {

        if (pigeonUpgrades.ContainsKey(Upgrades.lifeSteal)) HealServer(damage / 3);


        if (!audioSource.isPlaying)
        {
            audioSource.clip = hitClips[Random.Range(0, hitClips.Length)];
            audioSource.Play();
        }
    }
    public void PlayEatSound()
    {
        if (!audioSource.isPlaying)
        {
            audioSource.clip = cruchSound[Random.Range(0, cruchSound.Length)];
            audioSource.Play();
        }
    }
    public void AddUpgrade(Upgrades upgrade)
    {
        pigeonUpgrades.Add(upgrade, true);

        switch (upgrade)
        {
            case Upgrades.regen:
                regen = 10;
                break;
            case Upgrades.bulk:
                maxHp.Value += 50;
                break;
            case Upgrades.fly:
                canfly = true;
                break;
            case Upgrades.slam:
                if (isPlayer) gm.ShowSlamCoolDown();
                canSlam = true;
                break;
        }
    }


    protected void PigeonAttack(AttackProperties atkProp, Quaternion theAngle)
    {
        slash.Activate(new Vector3(atkProp.posX, atkProp.posY), theAngle);

        if (currentPigeonAttackSprite.Value == 0) currentPigeonAttackSprite.Value = 1;
        else currentPigeonAttackSprite.Value = 0;

        if (currentPigeonAttackSprite.Value == 0) atkProp.attackingUp = true;

        if (canSwitchAttackSprites.Value)
        {
            canSwitchAttackSprites.Value = false;
            if (atkProp.posX > transform.position.x)
            {
                isPointingLeft.Value = true;
            }
            else
            {
                isPointingLeft.Value = false;
            }
            atkProp.isFacingLeft = isPointingLeft.Value;
            StartCoroutine(DelayBeforeSpriteChange());
        }

        slash.attackProperties.Value = atkProp;

        if (isSlaming)
        {
            slash.transform.localScale = new Vector3(6, 6, 1);
        }

    }


    protected void OnPigeonSpawn()
    {
        if (IsOwner)
        {
            StartCoroutine(JumpAnimation());
            healthBarGameobject.SetActive(false);
            displayText.gameObject.SetActive(false);
            hpBar.gameObject.SetActive(false);
            StartCoroutine(Regen());
        }

        body.freezeRotation = true;
        currentHP.Value = maxHp.Value;
        gm = FindObjectOfType<GameManager>();
        gm.allpigeons.Add(this);
    }
    protected void CheckDirection(Vector2 direction)
    {
        if (direction.x == 0) return;
        if (direction.x > 0)
        {
            isPointingLeft.Value = true;
        }
        else
        {
            isPointingLeft.Value = false;
        }
    }
    protected void StartSlam(Vector3 desiredSlamPos)
    {
        if (!canSlam) return;
        canSlam = false;
        isSlaming = true;
        slamPos = Vector2.MoveTowards(transform.position, desiredSlamPos, 5f);
        StartCoroutine(StopSlam());
        sr.sprite = pigeonSlamSprite;
    }
    protected void EndSlam()
    {
        if (!isSlaming) return;

        Vector3 targ = slamPos;
        targ.z = 0f;
        targ.x -= transform.position.x;
        targ.y -= transform.position.y;

        float angle = Mathf.Atan2(targ.y, targ.x) * Mathf.Rad2Deg;
        Quaternion theAngle = Quaternion.Euler(new Vector3(0, 0, angle));

        AttackProperties atkProp = new()
        {
            indexOfDamagingPigeon = no.NetworkObjectId,
            damage = damage,
            hasCriticalDamage = false,
            hasKnockBack = false,
            attackingUp = false,
            posX = slamPos.x,
            posY = slamPos.y,
        };
        if (pigeonUpgrades.TryGetValue(Upgrades.critcalDamage, out bool _)) atkProp.hasCriticalDamage = true;
        if (pigeonUpgrades.TryGetValue(Upgrades.knockBack, out bool _)) atkProp.hasKnockBack = true;
        PigeonAttack(atkProp, theAngle);

        StartCoroutine(SlamCoolDown());
        StopCoroutine(StopSlam());
        isSlaming = false;
        if (isPlayer)
        {
            StartCoroutine(gm.StartSlamCoolDown());
        }
    }
    protected void SyncPigeonAttributes()
    {
        sr.flipX = isPointingLeft.Value;
        if (!IsOwner)
        {
            displayText.text = pigeonName + " LVL:" + level.Value;
        }
        if (isKnockedOut.Value)
        {
            if (!IsOwner)
            {
                hpBar.gameObject.SetActive(false);
            }
            sr.flipY = true;
        }
        else
        {
            if (!IsOwner)
            {
                hpBar.gameObject.SetActive(true);
                hpBar.localScale = new Vector3((float)currentHP.Value / maxHp.Value, 0.097f, 1);
            }

            sr.flipY = false;
            if (!canSwitchAttackSprites.Value)
            {
                sr.sprite = pigeonAttackSprites[currentPigeonAttackSprite.Value];
            }
            else
            {
                if (isSpriteNotHopping.Value)
                {
                    sr.sprite = defaultPigeonSprite;
                }
                else
                {
                    sr.sprite = pigeonJumpSprite;
                }
            }
        }
    }

    [ServerRpc]
    private void SpawnBloodServerRpc()
    {
        GameObject blood = Instantiate(bloodEffect, new Vector3(transform.position.x, transform.position.y, -1), transform.rotation);
        blood.GetComponent<NetworkObject>().Spawn();
    }
    private void UpdateNotHopping(bool isHoping)
    {
        if (body.velocity != Vector2.zero && isHoping)
        {
            isSpriteNotHopping.Value = false;
        }
        else
        {
            isSpriteNotHopping.Value = true;
        }
    }
    private void LevelUP()
    {
        level.Value++;
        xp.Value -= xpTillLevelUp.Value;
        xpTillLevelUp.Value = Mathf.RoundToInt(xpTillLevelUp.Value * 1.15f);
        damage++;
        maxHp.Value += 5;
        currentHP.Value += 5;
        speed += 20;

        if (pigeonAI != null)
        {
            pigeonAI.AILevelUP();
        }

        if (0 == level.Value % 5)
        {
            if (pigeonAI)
            {
                AddRandomUpgrade();
            }
            else
            {
                gm.ShowUpgrades();
            }
        }
    }
    private void AddRandomUpgrade()
    {
        for (int i = 0; i < 1000; i++)
        {
            Upgrades upgrade = gm.allPigeonUpgrades[Random.Range(0, gm.allPigeonUpgrades.Count)];
            if (pigeonUpgrades.TryGetValue(upgrade, out bool _))
            {
                continue;
            }
            else
            {
                AddUpgrade(upgrade);
                break;
            }
        }
    }
    private IEnumerator Respawn()
    {
        StartCoroutine(CheckDecollide());
        yield return new WaitForSeconds(5);
        canDeCollide = false;
        currentHP.Value = maxHp.Value;
        if (pigeonUpgrades.ContainsKey(Upgrades.slam)) canSlam = true;
        isKnockedOut.Value = false;
        bodyCollider.enabled = true;
        sr.sortingOrder = 0;
        sr.flipY = false;
    }
    private IEnumerator JumpAnimation()
    {
        while (true)
        {
            if (!canSwitchAttackSprites.Value)
            {
                yield return new WaitForSeconds(0.15f);
                continue;
            }
            UpdateNotHopping(false);
            yield return new WaitForSeconds(0.2f);
            if (!canSwitchAttackSprites.Value)
            {
                yield return new WaitForSeconds(0.15f);
                continue;
            }
            UpdateNotHopping(true);
            yield return new WaitForSeconds(0.2f);
        }
    }
    private IEnumerator DelayBeforeSpriteChange()
    {
        yield return new WaitForSeconds(0.15f);
        canSwitchAttackSprites.Value = true;
    }
    private IEnumerator Regen()
    {
        while (true)
        {
            yield return new WaitForSeconds(1);
            HealServer(regen);
        }
    }
    private IEnumerator CheckDecollide()
    {
        yield return new WaitForSeconds(1);
        canDeCollide = true;
    }
    private IEnumerator SlamCoolDown()
    {
        yield return new WaitForSeconds(3);
        canSlam = true;
    }
    private IEnumerator StopSlam()
    {
        yield return new WaitForSeconds(0.5f);
        EndSlam();
    }

}
