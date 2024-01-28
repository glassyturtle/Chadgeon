using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class Pigeon : NetworkBehaviour
{
    public NetworkVariable<bool> isKnockedOut = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> isFlying = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> isSlaming = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> maxHp = new(50, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> currentHP = new(50, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> xp = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> xpTillLevelUp = new(20, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> level = new(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<FixedString128Bytes> pigeonName = new("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public Dictionary<Upgrades, bool> pigeonUpgrades = new();

    [SerializeField] protected GameManager gm;
    [SerializeField] protected TextMesh displayText;
    [SerializeField] protected Rigidbody2D body;
    [SerializeField] protected CircleCollider2D bodyCollider;
    [SerializeField] protected float speed;
    [SerializeField] protected int damage;


    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] hitClips;
    [SerializeField] private AudioClip[] cruchSound;
    [SerializeField] private GameObject bloodEffect;
    [SerializeField] private GameObject healthBarGameobject;
    [SerializeField] private Transform hpBar;
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Sprite defaultPigeonSprite, pigeonJumpSprite, pigeonSlamSprite, pigeonFlap1Sprite, pigeonFlap2Sprite;
    [SerializeField] private Sprite[] pigeonAttackSprites;
    [SerializeField] private bool isPlayer = false;
    [SerializeField] private PigeonAI pigeonAI;
    [SerializeField] private HitScript slash;

    protected int secTillSlam = 5;
    protected bool canSlam = false;
    protected Vector3 slamPos;

    private int regen = 3;
    private NetworkVariable<bool> isPointingLeft = new(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<bool> isSpriteNotHopping = new(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    protected NetworkVariable<bool> canSwitchAttackSprites = new(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<int> currentPigeonAttackSprite = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<int> currentFlySprite = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);


    public enum Upgrades
    {
        regen = 0,
        tough = 1,
        brawler = 2,
        hearty = 3,
        bloodLust = 4,
        evasive = 5,
        critcalDamage = 6,
        slam = 7,
        nest = 9,
        swiftness = 10,
        hiddinTalon = 11,
        peckingOrder = 12,
        bleed = 13,
        enchanted = 14,
        superFeed = 15,
        assassin = 16,
    }
    public struct AttackProperties : INetworkSerializable
    {
        public ulong pigeonID;
        public int damage;
        public bool hasCriticalDamage;
        public bool hasKnockBack;
        public bool attackingUp;
        public bool isFacingLeft;
        public float posX;
        public float posY;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref pigeonID);
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
        public int xpOnKill;
        public int damageDealt;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref hasDied);
            serializer.SerializeValue(ref xpOnKill);
            serializer.SerializeValue(ref damageDealt);
        }
    }

    public void OnPigeonHit(AttackProperties atkProp)
    {

        //Stops calculating if successufly dodged
        if (pigeonUpgrades.ContainsKey(Upgrades.evasive) && Random.Range(0, 100) <= 30) return;

        //Gets the knockback direction of the hit
        Vector2 direction = transform.position - new Vector3(atkProp.posX, atkProp.posY);
        direction.Normalize();

        //Calculates total damage taken with modifiers
        int totalDamageTaking = atkProp.damage;
        if (isSlaming.Value) totalDamageTaking /= 2;
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
        bool hasBeenKO = false;

        if (currentHP.Value <= 0 && !isKnockedOut.Value)
        {
            if (gm.isSuddenDeath.Value)
            {

                hasBeenKO = true;
                totalDamageTaking -= currentHP.Value;
                currentHP.Value = 0;
                isSlaming.Value = false;
                StopCoroutine(StopSlam());
                isKnockedOut.Value = true;
                gm.StartSpectating();
            }
            else
            {
                hasBeenKO = true;
                totalDamageTaking -= currentHP.Value;
                currentHP.Value = 0;
                isSlaming.Value = false;
                StopCoroutine(StopSlam());
                isKnockedOut.Value = true;
                StartCoroutine(Respawn());
            }
        }


        DealtDamageProperties ddProp = new DealtDamageProperties
        {
            hasDied = hasBeenKO,
            damageDealt = totalDamageTaking,
            xpOnKill = level.Value * 15,
        };

        OnDealtDamageServerRpc(ddProp, atkProp.pigeonID);

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

    [ServerRpc]
    private void OnDealtDamageServerRpc(DealtDamageProperties ddProp, ulong pigeonID)
    {
        NetworkObject ob = NetworkManager.Singleton.SpawnManager.SpawnedObjects[pigeonID];
        if (!ob) return;
        ob.GetComponent<Pigeon>().ReciveDamageClientRpc(ddProp, pigeonID);
    }

    [ClientRpc]
    public void ReciveDamageClientRpc(DealtDamageProperties ddProp, ulong pigeonID)
    {
        if (!IsOwner || pigeonID != NetworkObjectId) return;
        if (pigeonUpgrades.ContainsKey(Upgrades.bloodLust)) HealServer(ddProp.damageDealt / 3);

        GainXP(ddProp.damageDealt / 4);

        if (ddProp.hasDied)
        {
            GainXP(ddProp.xpOnKill);
        }

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
            case Upgrades.hearty:
                maxHp.Value += 50;
                currentHP.Value += 50;
                break;
            case Upgrades.slam:
                if (isPlayer) gm.ShowSlamCoolDown();
                canSlam = true;
                break;
        }
    }


    protected void PigeonAttack(AttackProperties atkProp, Quaternion theAngle)
    {

        slash.attackProperties.Value = atkProp;
        slash.Activate(new Vector3(atkProp.posX, atkProp.posY), theAngle, isSlaming.Value);

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

    }


    protected void OnPigeonSpawn()
    {
        if (IsOwner)
        {
            if (GameDataHolder.multiplayerName == "") pigeonName.Value = "Chadgeon";
            else pigeonName.Value = GameDataHolder.multiplayerName;
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
        isSlaming.Value = true;
        canSwitchAttackSprites.Value = false;
        if (desiredSlamPos.x > transform.position.x)
        {
            isPointingLeft.Value = true;
        }
        else
        {
            isPointingLeft.Value = false;
        }
        slamPos = Vector2.MoveTowards(transform.position, desiredSlamPos, 5f);
        StartCoroutine(StopSlam());
    }
    protected void EndSlam()
    {
        if (!isSlaming.Value) return;
        canSwitchAttackSprites.Value = true;

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
            attackingUp = false,
            posX = slamPos.x,
            posY = slamPos.y,
        };
        if (pigeonUpgrades.TryGetValue(Upgrades.critcalDamage, out bool _)) atkProp.hasCriticalDamage = true;
        if (pigeonUpgrades.TryGetValue(Upgrades.brawler, out bool _)) atkProp.hasKnockBack = true;
        PigeonAttack(atkProp, theAngle);

        StartCoroutine(SlamCoolDown());
        StopCoroutine(StopSlam());
        isSlaming.Value = false;
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
            displayText.text = pigeonName.Value + " LVL:" + level.Value;
        }

        if (isKnockedOut.Value)
        {
            if (isFlying.Value)
            {
                sr.sortingOrder = 1;
                if (currentFlySprite.Value == 0) sr.sprite = pigeonFlap1Sprite;
                else sr.sprite = pigeonFlap2Sprite;

                sr.flipY = false;

            }
            else
            {
                sr.sortingOrder = -1;
                sr.flipY = true;

            }
            bodyCollider.enabled = false;

            if (!IsOwner)
            {
                hpBar.gameObject.SetActive(false);
            }
        }
        else if (isSlaming.Value)
        {
            bodyCollider.enabled = false;
            sr.sprite = pigeonSlamSprite;
            sr.sortingOrder = 1;
        }
        else
        {
            bodyCollider.enabled = true;
            sr.sortingOrder = 0;

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
        xp.Value -= xpTillLevelUp.Value;
        xpTillLevelUp.Value += 5 * level.Value;
        level.Value++;
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
        yield return new WaitForSeconds(3);
        if (gm.isSuddenDeath.Value)
        {
            StopCoroutine(StopSlam());
            isKnockedOut.Value = true;
            gm.StartSpectating();
            yield return null;
        }
        else
        {
            StartFly();

        }
    }
    private void StartFly()
    {

        isFlying.Value = true;
        slamPos = Vector2.MoveTowards(transform.position, new Vector3(Random.Range(-13f, 13f), Random.Range(-11f, 19f), 0), 50f);
        StartCoroutine(StopFlight());
        StartCoroutine(FlyAnimation());
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
    private IEnumerator FlyAnimation()
    {
        while (true)
        {
            currentFlySprite.Value = 0;
            yield return new WaitForSeconds(0.3f);
            currentFlySprite.Value = 1;
            yield return new WaitForSeconds(0.3f);
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
    private IEnumerator StopFlight()
    {
        yield return new WaitForSeconds(5f);
        StopFlying();
    }
    protected void StopFlying()
    {
        isFlying.Value = false;
        if (pigeonUpgrades.ContainsKey(Upgrades.slam)) canSlam = true;
        currentHP.Value = maxHp.Value;
        isKnockedOut.Value = false;
    }

}
