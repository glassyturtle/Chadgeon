using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Pigeon : NetworkBehaviour
{

    //Network variables
    public NetworkVariable<bool> isKnockedOut = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> maxHp = new(20, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> currentHP = new(20, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> level = new(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<bool> isPointingLeft = new(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<int> currentPigeonState = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    //pigeon states
    public bool isFlying;
    public bool isSprinting;
    protected bool isSlaming;
    protected bool isMewing;
    protected bool isMaxing;
    protected bool isThrowing;
    protected bool isAssassinating;
    protected bool isBleeding;
    protected bool isSlowed;
    protected bool isTurtle;

    //Pigeon Properties
    public int xp;
    public int xpTillLevelUp;
    public int flock;
    public string pigeonName;
    public float stamina;
    public float maxStamina;
    public bool isPlayer = false;
    [SerializeField] protected float speed;
    [SerializeField] protected int damage;
    protected float speedMod = 1;
    protected float staminaRecoveryRate = 1;
    protected float hitColldown = 0.3f;
    protected bool canSwitchAttackSprites = true;
    protected bool inBorder = true;
    private float knockbackMod = 1;
    private float regen = 0.02f;
    private int currentPigeonAttackSprite;

    //Upgrades and abilities
    public Dictionary<Upgrades, bool> pigeonUpgrades = new();
    public bool hasAbilityM2 = false;
    public bool hasAbilityE = false;
    public bool hasAbilityQ = false;
    protected float m2AbilityCooldown = 0;
    protected float eAbilityCooldown = 0;
    protected float qAbilityCooldown = 0;
    protected bool sprintOnCooldown = false;
    protected Vector3 abilityTargetPos;
    public int chargedFeathers = 3; //Razor feathers


    //UI
    [SerializeField] protected TextMesh displayText;
    [SerializeField] protected TextMesh flockDisplayText;

    //Refrences
    [SerializeField] protected Rigidbody2D body;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] hitClips;
    [SerializeField] private AudioClip[] cruchSound;
    [SerializeField] private GameObject bloodEffect;
    [SerializeField] private GameObject feather;
    [SerializeField] private GameObject healthBarGameobject;
    [SerializeField] private Transform hpBar;
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private SpriteRenderer bodysr;
    [SerializeField] private SpriteRenderer headsr;
    [SerializeField] private PigeonAI pigeonAI;
    [SerializeField] private HitScript slash;
    [SerializeField] private GameObject wholeGains;

    //Skins
    [SerializeField] private int skinBase;
    [SerializeField] private int skinBody;
    [SerializeField] private int skinHead;

    #region Structs and Enums
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
        swiftness = 8,
        enchanted = 9,
        assassin = 10,
        mewing = 11,
        wholeGains = 12,
        razorFeathers = 13,
        hiddinTalon = 14,
        turtle = 15,
        bleed = 16,
        pigeonPoo = 17,
        inspire = 18,
        psionic = 19,
        bandOfBrothers = 20,
        peckingOrder = 21,
        overclock = 22,
    }
    public struct AttackProperties : INetworkSerializable
    {
        public ulong pigeonID;
        public int damage;
        public bool hasCriticalDamage;
        public bool isAssassin;
        public bool hasKnockBack;
        public bool attackingUp;
        public bool isFacingLeft;
        public bool isEnchanted;
        public float posX;
        public float posY;
        public int flock;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref pigeonID);
            serializer.SerializeValue(ref damage);
            serializer.SerializeValue(ref hasCriticalDamage);
            serializer.SerializeValue(ref hasKnockBack);
            serializer.SerializeValue(ref isFacingLeft);
            serializer.SerializeValue(ref isEnchanted);
            serializer.SerializeValue(ref posX);
            serializer.SerializeValue(ref posY);
            serializer.SerializeValue(ref flock);
            serializer.SerializeValue(ref attackingUp);
            serializer.SerializeValue(ref isAssassin);
        }
    }
    public struct DealtDamageProperties : INetworkSerializable
    {
        public bool hasDied;
        public int xpOnKill;
        public int damageDealt;
        public bool wasPlayer;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref hasDied);
            serializer.SerializeValue(ref xpOnKill);
            serializer.SerializeValue(ref damageDealt);
            serializer.SerializeValue(ref wasPlayer);
        }
    }
    #endregion

    //Client RPCs
    [ClientRpc]
    public void OnPigeonHitCLientRPC(AttackProperties atkProp)
    {
        //Does not do anything if on same team or if they are not the owner
        if (!IsOwner || (atkProp.flock == flock && flock != 0)) return;

        //Stops calculating if successufly dodged
        if (!atkProp.isEnchanted && pigeonUpgrades.ContainsKey(Upgrades.evasive) && Random.Range(0, 100) <= 25) return;

        //Gets the knockback direction of the hit
        Vector2 direction = transform.position - new Vector3(atkProp.posX, atkProp.posY);
        direction.Normalize();

        //Calculates total damage taken with modifiers
        int totalDamageTaking = atkProp.damage;
        if (isSlaming) totalDamageTaking /= 2;
        if (atkProp.hasCriticalDamage && Random.Range(0, 100) <= 25) totalDamageTaking *= 2;
        if (atkProp.isAssassin && ((float)currentHP.Value / maxHp.Value) <= 0.33f) totalDamageTaking *= 2;
        if (!atkProp.isEnchanted && pigeonUpgrades.ContainsKey(Upgrades.tough)) totalDamageTaking = Mathf.RoundToInt(totalDamageTaking * 0.7f);
        if (isMaxing) totalDamageTaking = Mathf.RoundToInt(totalDamageTaking * 0.5f);

        currentHP.Value -= totalDamageTaking;

        //Calculates Life Steal

        //Calculates Knockback

        if (atkProp.hasKnockBack) HandleKnockbackServerRpc(40 * totalDamageTaking * knockbackMod * direction);
        else HandleKnockbackServerRpc(20 * totalDamageTaking * knockbackMod * direction);

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
            if (isPlayer) SaveDataManager.totalTimesKnockedOut++;
            if (GameManager.instance.isSuddenDeath.Value)
            {

                hasBeenKO = true;
                totalDamageTaking -= currentHP.Value;
                currentHP.Value = 0;
                isSlaming = false;
                StopCoroutine(StopSlam());
                isKnockedOut.Value = true;
                if (isPlayer) GameManager.instance.StartSpectating();
            }
            else
            {
                hasBeenKO = true;
                totalDamageTaking -= currentHP.Value;
                currentHP.Value = 0;
                isSlaming = false;
                StopCoroutine(StopSlam());
                isKnockedOut.Value = true;
                StartCoroutine(Respawn());
            }
        }


        DealtDamageProperties ddProp = new DealtDamageProperties
        {
            hasDied = hasBeenKO,
            damageDealt = totalDamageTaking,
            xpOnKill = 10 + (level.Value * 5),
            wasPlayer = isPlayer,
        };

        OnDealtDamageServerRpc(ddProp, atkProp.pigeonID);

    }
    [ClientRpc]
    public void ReciveDamageClientRpc(DealtDamageProperties ddProp, ulong pigeonID)


    {
        if (!IsOwner || pigeonID != NetworkObjectId) return;
        if (pigeonUpgrades.ContainsKey(Upgrades.bloodLust)) HealServer(ddProp.damageDealt / 3);

        GainXP(ddProp.damageDealt / 5);

        if (ddProp.hasDied)
        {
            if (isPlayer)
            {
                GameDataHolder.kills++;
                if (ddProp.wasPlayer) SaveDataManager.playerPigeonsKo++;
            }

            GainXP(ddProp.xpOnKill);
        }

        if (!audioSource.isPlaying)
        {
            audioSource.clip = hitClips[Random.Range(0, hitClips.Length)];
            audioSource.Play();
        }
    }


    //Server Rpcs
    [ServerRpc]
    private void OnDealtDamageServerRpc(DealtDamageProperties ddProp, ulong pigeonID)
    {
        try
        {

            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects[pigeonID])
            {
                NetworkObject ob = NetworkManager.Singleton.SpawnManager.SpawnedObjects[pigeonID];
                if (!ob) return;
                ob.GetComponent<Pigeon>().ReciveDamageClientRpc(ddProp, pigeonID);
            }
        }
        catch
        {
            Debug.Log("Start Error");
        }
    }
    [ServerRpc]
    private void UpdateSeverPigeonsServerRpc(GameManager.PigeonInitializeProperties pigeonData)
    {
        GameManager.instance.pigeonStartData.Add(pigeonData);
    }
    [ServerRpc]
    private void HandleKnockbackServerRpc(Vector2 force)
    {
        body.AddForce(force);
    }
    [ServerRpc]
    private void SpawnBloodServerRpc()
    {
        GameObject blood = Instantiate(bloodEffect, new Vector3(transform.position.x, transform.position.y, -1), transform.rotation);
        blood.GetComponent<NetworkObject>().Spawn();
    }
    [ServerRpc]
    private void SpawnWholeGainsServerRpc(Vector2 pos)
    {
        GameObject gains = Instantiate(wholeGains, pos, transform.rotation);
        gains.GetComponent<NetworkObject>().Spawn();
    }
    [ServerRpc]
    private void StopMomentumServerRpc(Vector3 pos)
    {
        body.velocity = Vector2.zero;
        transform.position = pos;
    }
    [ServerRpc]
    private void SpawnFeatherServerRpc(AttackProperties atk, Quaternion angle)
    {
        GameObject featherOb = Instantiate(feather, new Vector3(transform.position.x, transform.position.y, -1), transform.rotation);
        featherOb.GetComponent<NetworkObject>().Spawn();
        featherOb.GetComponent<featherScript>().Activate(atk, angle);
    }


    //Public
    public void GainXP(int amnt)
    {
        if (isKnockedOut.Value) return;
        if (isPlayer) SaveDataManager.totalPigeonXPEarned += amnt;
        xp += amnt;
        if (xp >= xpTillLevelUp)
        {
            LevelUP();
        }
    }
    public void GainXP(int amnt, bool isCone)
    {
        if (isCone == true && isPlayer)
        {
            GameDataHolder.conesCollected++;
            SaveDataManager.totalConesCollected++;
        }
        if (isKnockedOut.Value) return;
        if (isPlayer) SaveDataManager.totalPigeonXPEarned += amnt;
        xp += amnt * 100;
        if (xp >= xpTillLevelUp)
        {
            LevelUP();
        }
    }
    public void HealServer(float amt)
    {
        if (isKnockedOut.Value) return;
        currentHP.Value += Mathf.RoundToInt(amt);
        if (currentHP.Value > maxHp.Value) currentHP.Value = maxHp.Value;

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
        bool hasAbilitySlotUnlocked = false;
        switch (upgrade)
        {
            case Upgrades.slam:
                if (hasAbilityM2) hasAbilitySlotUnlocked = true; break;
            case Upgrades.hiddinTalon:
                if (hasAbilityQ) hasAbilitySlotUnlocked = true; break;
            case Upgrades.mewing:
                if (hasAbilityQ) hasAbilitySlotUnlocked = true; break;
            case Upgrades.wholeGains:
                if (hasAbilityE) hasAbilitySlotUnlocked = true; break;
            case Upgrades.turtle:
                if (hasAbilityE) hasAbilitySlotUnlocked = true; break;
            case Upgrades.pigeonPoo:
                if (hasAbilityE) hasAbilitySlotUnlocked = true; break;
            case Upgrades.peckingOrder:
                if (hasAbilityE) hasAbilitySlotUnlocked = true; break;
            case Upgrades.razorFeathers:
                if (hasAbilityM2) hasAbilitySlotUnlocked = true; break;
        }

        if (!IsOwner || pigeonUpgrades.ContainsKey(upgrade) || hasAbilitySlotUnlocked) return;
        pigeonUpgrades.Add(upgrade, true);
        if (!pigeonAI) GameManager.instance.AddUpgradeToDisply((int)upgrade);
        switch (upgrade)
        {
            case Upgrades.regen:
                regen = 0.06f;
                break;
            case Upgrades.tough:
                speedMod -= .1f;
                knockbackMod -= .4f;
                break;
            case Upgrades.brawler:
                knockbackMod -= .4f;
                break;
            case Upgrades.swiftness:
                speedMod += .1f;
                staminaRecoveryRate *= 1.5f;
                break;
            case Upgrades.evasive:
                speedMod += .1f;
                break;
            case Upgrades.hearty:
                maxHp.Value += level.Value * 2;
                currentHP.Value += level.Value * 2;
                break;
            case Upgrades.slam:
                if (isPlayer) GameManager.instance.ActivateAbility(Upgrades.slam);
                hasAbilityM2 = true; break;
            case Upgrades.hiddinTalon:
                GameManager.instance.ActivateAbility(Upgrades.hiddinTalon);
                hasAbilityQ = true; break;
            case Upgrades.mewing:
                GameManager.instance.ActivateAbility(Upgrades.mewing);
                hasAbilityQ = true; break;
            case Upgrades.wholeGains:
                GameManager.instance.ActivateAbility(Upgrades.wholeGains);
                hasAbilityE = true; break;
            case Upgrades.peckingOrder:
                GameManager.instance.ActivateAbility(Upgrades.peckingOrder);
                hasAbilityE = true; break;
            case Upgrades.turtle:
                GameManager.instance.ActivateAbility(Upgrades.turtle);
                hasAbilityE = true; break;
            case Upgrades.pigeonPoo:
                GameManager.instance.ActivateAbility(Upgrades.pigeonPoo);
                hasAbilityE = true; break;
            case Upgrades.razorFeathers:
                GameManager.instance.ActivateAbility(Upgrades.razorFeathers);
                hasAbilityM2 = true; break;
        }
    }
    public void UpatePigeonInitialValues(GameManager.PigeonInitializeProperties data)
    {
        flock = data.flock;
        skinBase = data.skinBase;
        skinHead = data.skinHead;
        skinBody = data.skinBody;
        pigeonName = data.pigeonName;

        if (flock != 0)
        {
            flockDisplayText.gameObject.SetActive(true);
            switch (flock)
            {
                case 1:
                    flockDisplayText.text = "Enjoyers";
                    flockDisplayText.color = Color.cyan;
                    break;
                case 2:
                    flockDisplayText.text = "Psychos";
                    flockDisplayText.color = Color.red;
                    break;
                case 3:
                    flockDisplayText.text = "Minions";
                    flockDisplayText.color = Color.yellow;
                    break;
                case 4:
                    flockDisplayText.text = "Looksmaxers";
                    flockDisplayText.color = Color.green;
                    break;
            }
        }
        else
        {
            flockDisplayText.gameObject.SetActive(false);
        }
    }


    //Protected
    protected void PigeonAttack(AttackProperties atkProp, Quaternion theAngle)
    {
        atkProp.flock = flock;
        slash.attackProperties.Value = atkProp;

        slash.Activate(new Vector3(atkProp.posX, atkProp.posY), theAngle, isSlaming);

        if (canSwitchAttackSprites)
        {
            if (currentPigeonAttackSprite == 0)
            {
                currentPigeonState.Value = 3;
                currentPigeonAttackSprite = 1;
            }
            else
            {
                currentPigeonState.Value = 2;
                currentPigeonAttackSprite = 0;

            }

            if (currentPigeonAttackSprite == 0) atkProp.attackingUp = true;

            canSwitchAttackSprites = false;
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
    protected void PigeonThrow(AttackProperties atkProp, Quaternion theAngle)
    {
        if (chargedFeathers <= 0) return;
        atkProp.flock = flock;


        if (chargedFeathers >= 3)
        {
            if (pigeonUpgrades.ContainsKey(Upgrades.overclock))
            {
                GameManager.instance.StartCooldown(Upgrades.razorFeathers, 3);
                m2AbilityCooldown = 3;
            }
            else
            {
                GameManager.instance.StartCooldown(Upgrades.razorFeathers, 4);
                m2AbilityCooldown = 4;
            }
        }
        chargedFeathers--;



        SpawnFeatherServerRpc(atkProp, theAngle);

        if (canSwitchAttackSprites)
        {
            if (currentPigeonAttackSprite == 0)
            {
                currentPigeonState.Value = 3;
                currentPigeonAttackSprite = 1;
            }
            else
            {
                currentPigeonState.Value = 2;
                currentPigeonAttackSprite = 0;

            }

            if (currentPigeonAttackSprite == 0) atkProp.attackingUp = true;

            canSwitchAttackSprites = false;
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
    protected void SummonWholeGain(Vector2 pos)
    {
        if (eAbilityCooldown > 0) return;

        if (pigeonUpgrades.ContainsKey(Upgrades.overclock))
        {
            GameManager.instance.StartCooldown(Upgrades.wholeGains, 22);
            eAbilityCooldown = 3;
        }
        else
        {
            GameManager.instance.StartCooldown(Upgrades.wholeGains, 30);
            eAbilityCooldown = 4;
        }
        SpawnWholeGainsServerRpc(pos);
    }
    protected void OnPigeonSpawn()
    {
        stamina = 3;
        maxStamina = 3;
        if (IsOwner)
        {
            if (!pigeonAI)
            {
                if (GameDataHolder.multiplayerName == "") pigeonName = "Chadgeon";
                else pigeonName = GameDataHolder.multiplayerName;

                flock = GameDataHolder.flock;
                skinBase = SaveDataManager.selectedSkinBase;
                skinBody = SaveDataManager.selectedSkinBody;
                skinHead = SaveDataManager.selectedSkinHead;
                healthBarGameobject.SetActive(false);
                displayText.gameObject.SetActive(false);
                hpBar.gameObject.SetActive(false);
            }
            currentHP.Value = maxHp.Value;

            if (flock != 0)
            {
                flockDisplayText.gameObject.SetActive(true);
                switch (flock)
                {
                    case 1:
                        flockDisplayText.text = "Enjoyers";
                        flockDisplayText.color = Color.cyan;
                        break;
                    case 2:
                        flockDisplayText.text = "Psychos";
                        flockDisplayText.color = Color.red;
                        break;
                    case 3:
                        flockDisplayText.text = "Minions";
                        flockDisplayText.color = Color.yellow;
                        break;
                    case 4:
                        flockDisplayText.text = "Looksmaxers";
                        flockDisplayText.color = Color.green;
                        break;
                }
            }
            else
            {
                flockDisplayText.gameObject.SetActive(false);
            }

            UpdateSeverPigeonsServerRpc(new GameManager.PigeonInitializeProperties
            {
                flock = flock,
                skinBase = skinBase,
                skinHead = skinHead,
                skinBody = skinBody,
                pigeonID = NetworkObjectId,
                pigeonName = pigeonName
            });

            StartCoroutine(JumpAnimation());
            StartCoroutine(Regen());
        }

        body.freezeRotation = true;
        GameManager.instance.allpigeons.Add(this);
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
    protected bool StartSlam(Vector3 desiredSlamPos)
    {
        if (m2AbilityCooldown > 0) return false;

        //Starts slam Cooldown
        if (pigeonUpgrades.ContainsKey(Upgrades.overclock))
        {
            GameManager.instance.StartCooldown(Upgrades.slam, 3);
            m2AbilityCooldown = 3;
        }
        else
        {
            GameManager.instance.StartCooldown(Upgrades.slam, 4);
            m2AbilityCooldown = 4;
        }

        isSlaming = true;
        canSwitchAttackSprites = false;
        currentPigeonState.Value = 4;

        if (desiredSlamPos.x > transform.position.x)
        {
            isPointingLeft.Value = true;
        }
        else
        {
            isPointingLeft.Value = false;
        }
        abilityTargetPos = Vector2.MoveTowards(transform.position, desiredSlamPos, 6f);
        StartCoroutine(StopSlam());
        return true;
    }
    protected bool StartMewing()
    {
        if (qAbilityCooldown > 0) return false;

        //Starts mew Cooldown
        if (pigeonUpgrades.ContainsKey(Upgrades.overclock))
        {
            GameManager.instance.StartCooldown(Upgrades.mewing, 90);
            qAbilityCooldown = 90;
        }
        else
        {
            GameManager.instance.StartCooldown(Upgrades.mewing, 120);
            qAbilityCooldown = 120;
        }

        StartCoroutine(StopMoggingTimeout());
        return true;
    }
    protected bool Assassinate()
    {
        if (qAbilityCooldown > 0) return false;

        //Starts mew Cooldown
        if (pigeonUpgrades.ContainsKey(Upgrades.overclock))
        {
            GameManager.instance.StartCooldown(Upgrades.mewing, 67);
            qAbilityCooldown = 90;
        }
        else
        {
            GameManager.instance.StartCooldown(Upgrades.mewing, 90);
            qAbilityCooldown = 120;
        }


        isSlaming = true;
        canSwitchAttackSprites = false;
        currentPigeonState.Value = 4;


        Vector3 desiredSlamPos = GetNearestPigeonTarget().transform.position;
        if (desiredSlamPos.x > transform.position.x)
        {
            isPointingLeft.Value = true;
        }
        else
        {
            isPointingLeft.Value = false;
        }

        abilityTargetPos = desiredSlamPos;


        return true;
    }
    protected void StopMogging()
    {
        isMaxing = false;
        speedMod += .5f;

        gameObject.transform.localScale = new Vector3(1, 1, 0);
    }
    protected void EndSlam()
    {
        if (!isSlaming) return;
        canSwitchAttackSprites = true;

        Vector2 pos = transform.position;
        pos = Vector2.MoveTowards(pos, abilityTargetPos, 0.1f);

        Vector3 targ = abilityTargetPos;
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
            posX = pos.x,
            posY = pos.y,
        };
        if (pigeonUpgrades.TryGetValue(Upgrades.critcalDamage, out bool _)) atkProp.hasCriticalDamage = true;
        if (pigeonUpgrades.TryGetValue(Upgrades.brawler, out bool _)) atkProp.hasKnockBack = true;
        PigeonAttack(atkProp, theAngle);

        StopCoroutine(StopSlam());
        isSlaming = false;
        if (isPlayer)
        {
            GameManager.instance.StartCooldown(Upgrades.slam, 3);
        }
    }
    protected void SyncPigeonAttributes()
    {
        //this is called on update for every pigeon

        //Reduces cooldowns if the pigeon is the owner
        CoolDownAbilities();

        sr.flipX = isPointingLeft.Value;
        bodysr.flipX = isPointingLeft.Value;
        headsr.flipX = isPointingLeft.Value;
        sr.sprite = CustomizationManager.Instance.GetSprite(CustomizationManager.SpriteType.baseSkin, skinBase, currentPigeonState.Value);
        bodysr.sprite = CustomizationManager.Instance.GetSprite(CustomizationManager.SpriteType.body, skinBody, currentPigeonState.Value);
        headsr.sprite = CustomizationManager.Instance.GetSprite(CustomizationManager.SpriteType.head, skinHead, currentPigeonState.Value);

        //Adjust sprite depending on what state they are in

        switch (currentPigeonState.Value)
        {
            case 4:
                gameObject.layer = 8;
                sr.sortingOrder = 4;
                bodysr.sortingOrder = 5;
                headsr.sortingOrder = 5;
                break;
            case 5:
                IsFlyingSpriteAdjustments();
                break;
            case 6:
                IsFlyingSpriteAdjustments();
                break;
            default:
                if (isKnockedOut.Value)
                {
                    sr.sortingOrder = -2;
                    bodysr.sortingOrder = -1;
                    headsr.sortingOrder = -1;
                    gameObject.layer = 8;
                    sr.flipY = true;
                    bodysr.flipY = true;
                    headsr.flipY = true;

                    if (!IsOwner || (IsOwner && pigeonAI))
                    {
                        hpBar.gameObject.SetActive(false);
                    }
                }
                else
                {
                    sr.sortingOrder = 1;
                    bodysr.sortingOrder = 2;
                    headsr.sortingOrder = 2;
                    gameObject.layer = 7;
                    sr.flipY = false;
                    bodysr.flipY = false;
                    headsr.flipY = false;

                    if (!IsOwner || (IsOwner && pigeonAI))
                    {
                        hpBar.gameObject.SetActive(true);
                        hpBar.localScale = new Vector3((float)currentHP.Value / maxHp.Value, 0.097f, 1);
                    }
                }
                break;
        }


        //Updates name and level label
        if (!IsOwner || (IsOwner && pigeonAI))
        {
            displayText.text = pigeonName + " LVL:" + level.Value;
        }
    }
    protected void StopFlying()
    {
        StopMomentumServerRpc(abilityTargetPos);
        isFlying = false;
        currentHP.Value = maxHp.Value;
        stamina = maxStamina;
        isKnockedOut.Value = false;

    }


    //Private
    private void IsFlyingSpriteAdjustments()
    {
        gameObject.layer = 10;
        sr.sortingOrder = 100;
        bodysr.sortingOrder = 101;
        headsr.sortingOrder = 101;
        sr.flipY = false;
        bodysr.flipY = false;
        headsr.flipY = false;
    }
    private void UpdateNotHopping(bool isHoping)
    {
        if (isPlayer)
        {
            if ((Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0) && isHoping)
            {
                currentPigeonState.Value = 1;
            }
            else
            {
                currentPigeonState.Value = 0;
            }
        }
        else
        {
            if ((Mathf.Abs(body.velocity.x) > 0.1f || Mathf.Abs(body.velocity.y) > 0.1f) && isHoping)
            {
                currentPigeonState.Value = 1;
            }
            else
            {
                currentPigeonState.Value = 0;
            }
        }
    }
    private void LevelUP()
    {
        xp -= xpTillLevelUp;
        xpTillLevelUp += 10;
        level.Value++;
        damage += 3;
        if (pigeonUpgrades.TryGetValue(Upgrades.hearty, out bool _))
        {
            maxHp.Value += 7;
            currentHP.Value += 7;
        }
        else
        {
            maxHp.Value += 5;
            currentHP.Value += 5;
        }

        speed += 25;

        if (0 == level.Value % 5)
        {
            if (isPlayer) SaveDataManager.upgradesAquired++;
            if (pigeonAI && IsHost && IsOwner)
            {
                AddRandomUpgrade();
                pigeonAI.AILevelUP();
            }
            else
            {
                GameManager.instance.ShowUpgrades();
            }
        }
    }
    private void AddRandomUpgrade()
    {
        for (int i = 0; i < 1000; i++)
        {
            Upgrades upgrade = GameManager.instance.allPigeonUpgrades[Random.Range(0, GameManager.instance.allPigeonUpgrades.Count)];

            bool hasAbilitySlotUnlocked = false;
            switch (upgrade)
            {
                case Upgrades.slam:
                    if (hasAbilityM2) hasAbilitySlotUnlocked = true; break;
                case Upgrades.hiddinTalon:
                    if (hasAbilityQ) hasAbilitySlotUnlocked = true; break;
                case Upgrades.mewing:
                    if (hasAbilityQ) hasAbilitySlotUnlocked = true; break;
                case Upgrades.wholeGains:
                    if (hasAbilityE) hasAbilitySlotUnlocked = true; break;
                case Upgrades.turtle:
                    if (hasAbilityE) hasAbilitySlotUnlocked = true; break;
                case Upgrades.pigeonPoo:
                    if (hasAbilityE) hasAbilitySlotUnlocked = true; break;
                case Upgrades.peckingOrder:
                    if (hasAbilityE) hasAbilitySlotUnlocked = true; break;
                case Upgrades.razorFeathers:
                    if (hasAbilityM2) hasAbilitySlotUnlocked = true; break;
            }


            if (pigeonUpgrades.TryGetValue(upgrade, out bool _) ||
                (flock == 0 && (upgrade == Upgrades.bandOfBrothers || upgrade == Upgrades.inspire)) ||
                (upgrade == Upgrades.hiddinTalon && !pigeonUpgrades.TryGetValue(Upgrades.assassin, out bool _)) ||
                hasAbilitySlotUnlocked)
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
    private void StartFly()
    {
        if (!IsOwner) return;
        isFlying = true;

        abilityTargetPos = GameManager.instance.GetSpawnPos();
        //StartCoroutine(StopFlight());
        StartCoroutine(FlyAnimation());
    }
    private void CoolDownAbilities()
    {
        //Called on update to reduce the cooldown of the 3 unlocked abilities
        if (!IsOwner) return;

        //reduces cooldown for each ability type. 0 means its ready to be used again
        if (hasAbilityM2)
        {
            if (m2AbilityCooldown > 0) m2AbilityCooldown -= Time.deltaTime;
            else
            {
                if (chargedFeathers < 3)
                {
                    //Starts slam Cooldown
                    chargedFeathers += 1;
                    if (chargedFeathers >= 3) m2AbilityCooldown = 0;
                    else
                    {
                        if (pigeonUpgrades.ContainsKey(Upgrades.overclock))
                        {
                            GameManager.instance.StartCooldown(Upgrades.razorFeathers, 3);
                            m2AbilityCooldown = 3;
                        }
                        else
                        {
                            GameManager.instance.StartCooldown(Upgrades.razorFeathers, 4);
                            m2AbilityCooldown = 4;
                        }
                    }

                }
                else m2AbilityCooldown = 0;

            }
        }

        if (hasAbilityE)
        {
            if (eAbilityCooldown > 0) eAbilityCooldown -= Time.deltaTime;
            else eAbilityCooldown = 0;
        }

        if (hasAbilityQ)
        {
            if (qAbilityCooldown > 0) qAbilityCooldown -= Time.deltaTime;
            else qAbilityCooldown = 0;
        }
    }
    private Pigeon GetNearestPigeonTarget()
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
            return null;
        }
        else
        {
            return closestPigeon;
        }
    }


    //Enumerators
    protected IEnumerator StartSprintCooldown()
    {
        yield return new WaitForSeconds(1);
        sprintOnCooldown = false;
    }
    private IEnumerator Respawn()
    {

        yield return new WaitForSeconds(3);

        if (GameManager.instance.isSuddenDeath.Value)
        {
            StopCoroutine(StopSlam());
            isKnockedOut.Value = true;
            if (isPlayer) GameManager.instance.StartSpectating();
            yield return null;
        }
        else
        {
            StartFly();
        }
    }
    private IEnumerator JumpAnimation()
    {
        while (true)
        {
            if (!canSwitchAttackSprites || isFlying || isKnockedOut.Value)
            {
                yield return new WaitForSeconds(0.15f);
                continue;
            }
            UpdateNotHopping(false);
            yield return new WaitForSeconds(0.2f);
            if (!canSwitchAttackSprites || isFlying || isKnockedOut.Value)
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
            if (!isFlying) break;
            currentPigeonState.Value = 5;
            yield return new WaitForSeconds(0.3f);
            if (!isFlying) break;
            currentPigeonState.Value = 6;
            yield return new WaitForSeconds(0.3f);
        }
    }
    private IEnumerator DelayBeforeSpriteChange()
    {
        yield return new WaitForSeconds(0.15f);
        canSwitchAttackSprites = true;
    }
    private IEnumerator Regen()
    {
        while (true)
        {
            yield return new WaitForSeconds(1);
            if (!inBorder && !isKnockedOut.Value)
            {
                currentHP.Value -= 5;

                if (currentHP.Value <= 0)
                {
                    currentHP.Value = 0;
                    isSlaming = false;
                    StopCoroutine(StopSlam());
                    isKnockedOut.Value = true;
                    GameManager.instance.StartSpectating();
                }
            }
            else
            {
                HealServer(regen * maxHp.Value);
            }
        }
    }
    private IEnumerator StopSlam()
    {
        yield return new WaitForSeconds(0.5f);
        EndSlam();
    }
    private IEnumerator StopMoggingTimeout()
    {
        isMewing = true;
        yield return new WaitForSeconds(3);
        isMewing = false;
        isMaxing = true;
        speedMod -= .5f;
        knockbackMod += 1f;
        gameObject.transform.localScale = new Vector3(2, 2, 0);
        yield return new WaitForSeconds(20f);
        StopMogging();
    }
}
