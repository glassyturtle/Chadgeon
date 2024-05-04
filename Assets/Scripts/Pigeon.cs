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
    protected bool inPoo = false;
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
    public bool inEvacsite = false;
    private float knockbackMod = 1;
    private float regen = 0.02f;
    private int currentPigeonAttackSprite;
    private int secondsToRespawn = 3;

    //Upgrades and abilities
    public Dictionary<Upgrades, bool> pigeonUpgrades = new();
    public bool hasAbilityM2 = false;
    public bool hasAbilityE = false;
    public bool hasAbilityQ = false;
    protected float m2AbilityCooldown = 1;
    protected float eAbilityCooldown = 1;
    protected float qAbilityCooldown = 1;
    protected bool sprintOnCooldown = false;
    protected Vector3 abilityTargetPos;
    public int chargedFeathers = 0; //Razor feathers


    //UI
    [SerializeField] protected TextMesh displayText;
    [SerializeField] protected TextMesh flockDisplayText;

    //Refrences
    [SerializeField] protected Pigeon targetPigeon;
    [SerializeField] protected Rigidbody2D body;
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
    [SerializeField] private GameObject pigeonPoo;
    [SerializeField] private AudioClip[] cruchSound;
    [SerializeField] private AudioSource audioSource;


    //Skins
    [SerializeField] private int skinBase;
    [SerializeField] private int skinBody;
    [SerializeField] private int skinHead;

    #region Structs and Enums
    public enum Upgrades
    {
        pigeonOfViolence = -1,
        pigeonOfMomentum = -2,
        pigeonOfGrowth = -3,
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
        bleed = 15,
        pigeonPoo = 16,
        psionic = 17,
        overclock = 18,
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
        public bool isAssassinating;
        public bool hasBleed;
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
            serializer.SerializeValue(ref isAssassinating);
            serializer.SerializeValue(ref hasBleed);
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

        int totalDamageTaking;


        if (atkProp.isAssassinating)
        {
            totalDamageTaking = Mathf.RoundToInt(maxHp.Value * 0.5f);
            currentHP.Value -= totalDamageTaking;
        }
        else
        {
            //Stops calculating if successufly dodged
            if (!atkProp.isEnchanted && pigeonUpgrades.ContainsKey(Upgrades.evasive) && Random.Range(0, 100) <= 25) return;

            //Gets the knockback direction of the hit
            Vector2 direction = transform.position - new Vector3(atkProp.posX, atkProp.posY);
            direction.Normalize();

            //Calculates total damage taken with modifiers
            totalDamageTaking = atkProp.damage;
            if (isSlaming) totalDamageTaking /= 2;
            if (atkProp.hasCriticalDamage && Random.Range(0, 100) <= 25) totalDamageTaking *= 2;
            if (atkProp.isAssassin && ((float)currentHP.Value / maxHp.Value) <= 0.33f) totalDamageTaking *= 2;
            if (!atkProp.isEnchanted && pigeonUpgrades.ContainsKey(Upgrades.tough)) totalDamageTaking = Mathf.RoundToInt(totalDamageTaking * 0.7f);
            if (isMaxing) totalDamageTaking = Mathf.RoundToInt(totalDamageTaking * 0.5f);
            if (atkProp.hasBleed && Random.Range(0, 100) <= 25)
            {
                StopCoroutine(StartBleed());
                StartCoroutine(StartBleed());
            }

            currentHP.Value -= totalDamageTaking;

            //Calculates Life Steal

            //Calculates Knockback

            if (atkProp.hasKnockBack) HandleKnockbackServerRpc(40 * totalDamageTaking * knockbackMod * direction);
            else HandleKnockbackServerRpc(20 * totalDamageTaking * knockbackMod * direction);
        }


        //Sound Effects and Blood
        SpawnBloodServerRpc();


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
    private void SpawnPigeonPooServerRpc(Vector2 pos)
    {
        GameObject gains = Instantiate(pigeonPoo, pos, transform.rotation);
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
    [ServerRpc]
    private void ChangePigeonSizeServerRpc(bool changeToBig)
    {
        if (changeToBig)
        {
            gameObject.transform.localScale = new Vector3(2, 2, 0);
        }
        else
        {
            gameObject.transform.localScale = new Vector3(1, 1, 0);
        }
    }


    //Public
    public void PlayEatSound()
    {
        if (!audioSource.isPlaying)
        {
            audioSource.clip = cruchSound[Random.Range(0, cruchSound.Length)];
            audioSource.Play();
        }
    }
    public void GainXP(int amnt)
    {
        if (isKnockedOut.Value) return;
        if (pigeonUpgrades.TryGetValue(Upgrades.psionic, out _)) amnt = Mathf.RoundToInt(amnt * 1.2f);
        if (isPlayer) SaveDataManager.totalPigeonXPEarned += amnt;
        xp += amnt;
        if (xp >= xpTillLevelUp)
        {
            LevelUP();
        }
    }
    public void GainXP(int amnt, bool isCone)
    {

        if (isKnockedOut.Value) return;
        if (pigeonUpgrades.TryGetValue(Upgrades.psionic, out _)) amnt = Mathf.RoundToInt(amnt * 1.2f);
        if (isCone == true && isPlayer)
        {
            GameDataHolder.conesCollected++;
            SaveDataManager.totalConesCollected++;
        }
        if (isPlayer)
        {
            SaveDataManager.totalPigeonXPEarned += amnt;

        }
        xp += amnt;

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
            case Upgrades.pigeonPoo:
                if (hasAbilityE) hasAbilitySlotUnlocked = true; break;
            case Upgrades.razorFeathers:
                if (hasAbilityM2) hasAbilitySlotUnlocked = true; break;
        }

        if (!IsOwner || pigeonUpgrades.ContainsKey(upgrade) || hasAbilitySlotUnlocked) return;
        if (upgrade != Upgrades.pigeonOfMomentum && upgrade != Upgrades.pigeonOfGrowth && upgrade != Upgrades.pigeonOfViolence) pigeonUpgrades.Add(upgrade, true);
        if (!pigeonAI) GameManager.instance.AddUpgradeToDisply((int)upgrade);
        switch (upgrade)
        {
            case Upgrades.pigeonOfGrowth:
                maxHp.Value += 30;
                currentHP.Value += 30;
                break;
            case Upgrades.pigeonOfViolence:
                damage += 12;
                break;
            case Upgrades.pigeonOfMomentum:
                speed += 200;
                break;
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
            case Upgrades.psionic:
                if (isPlayer) GameManager.instance.AddExtraUpgradePick();
                break;
            case Upgrades.hearty:
                maxHp.Value += level.Value * 2;
                currentHP.Value += level.Value * 2;
                break;
            case Upgrades.slam:
                if (isPlayer) GameManager.instance.ActivateAbility(Upgrades.slam);
                hasAbilityM2 = true; break;
            case Upgrades.hiddinTalon:
                if (isPlayer) GameManager.instance.ActivateAbility(Upgrades.hiddinTalon);
                hasAbilityQ = true; break;
            case Upgrades.mewing:
                if (isPlayer) GameManager.instance.ActivateAbility(Upgrades.mewing);
                hasAbilityQ = true; break;
            case Upgrades.wholeGains:
                if (isPlayer) GameManager.instance.ActivateAbility(Upgrades.wholeGains);
                hasAbilityE = true; break;
            case Upgrades.pigeonPoo:
                if (isPlayer) GameManager.instance.ActivateAbility(Upgrades.pigeonPoo);
                hasAbilityE = true; break;
            case Upgrades.razorFeathers:
                if (isPlayer) GameManager.instance.ActivateAbility(Upgrades.razorFeathers);
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
        if (chargedFeathers <= 0 || !pigeonUpgrades.ContainsKey(Upgrades.razorFeathers)) return;
        atkProp.flock = flock;


        if (chargedFeathers >= 3)
        {
            if (pigeonUpgrades.ContainsKey(Upgrades.overclock))
            {
                if (isPlayer) GameManager.instance.StartCooldown(Upgrades.razorFeathers, 3);
                m2AbilityCooldown = 3;
            }
            else
            {
                if (isPlayer) GameManager.instance.StartCooldown(Upgrades.razorFeathers, 4);
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
        if (eAbilityCooldown > 0 || !pigeonUpgrades.ContainsKey(Upgrades.wholeGains)) return;

        if (pigeonUpgrades.ContainsKey(Upgrades.overclock))
        {
            if (isPlayer) GameManager.instance.StartCooldown(Upgrades.wholeGains, 22);
            eAbilityCooldown = 22;
        }
        else
        {
            if (isPlayer) GameManager.instance.StartCooldown(Upgrades.wholeGains, 30);
            eAbilityCooldown = 30;
        }
        SpawnWholeGainsServerRpc(pos);
    }
    protected void SummonPigeonPoo(Vector2 pos)
    {
        if (eAbilityCooldown > 0 || !pigeonUpgrades.ContainsKey(Upgrades.pigeonPoo)) return;

        if (pigeonUpgrades.ContainsKey(Upgrades.overclock))
        {
            if (isPlayer) GameManager.instance.StartCooldown(Upgrades.pigeonPoo, 7);
            eAbilityCooldown = 7;
        }
        else
        {
            if (isPlayer) GameManager.instance.StartCooldown(Upgrades.pigeonPoo, 10);
            eAbilityCooldown = 10;
        }
        SpawnPigeonPooServerRpc(pos);
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
                if (GameDataHolder.gameMode == "Supremacy") flock = GameDataHolder.flock;
                else flock = 1;
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
        if ((pigeonAI && GameDataHolder.gameMode != "Supremacy") || isPlayer) GameManager.instance.allpigeons.Add(this);
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
        if (m2AbilityCooldown > 0 || !pigeonUpgrades.ContainsKey(Upgrades.slam)) return false;

        //Starts slam Cooldown
        if (pigeonUpgrades.ContainsKey(Upgrades.overclock))
        {
            if (isPlayer) GameManager.instance.StartCooldown(Upgrades.slam, 3);
            m2AbilityCooldown = 3;
        }
        else
        {
            if (isPlayer) GameManager.instance.StartCooldown(Upgrades.slam, 4);
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
        if (qAbilityCooldown > 0 || !pigeonUpgrades.ContainsKey(Upgrades.mewing)) return false;

        //Starts mew Cooldown
        if (pigeonUpgrades.ContainsKey(Upgrades.overclock))
        {
            if (isPlayer) GameManager.instance.StartCooldown(Upgrades.mewing, 90);
            qAbilityCooldown = 90;
        }
        else
        {
            if (isPlayer) GameManager.instance.StartCooldown(Upgrades.mewing, 120);
            qAbilityCooldown = 120;
        }

        StartCoroutine(StopMoggingTimeout());
        return true;
    }
    protected bool Assassinate()
    {
        targetPigeon = GetNearestPigeonTarget();

        if (qAbilityCooldown > 0 || targetPigeon != null || !pigeonUpgrades.ContainsKey(Upgrades.hiddinTalon)) return false;

        //Starts mew Cooldown
        if (pigeonUpgrades.ContainsKey(Upgrades.overclock))
        {
            if (isPlayer) GameManager.instance.StartCooldown(Upgrades.hiddinTalon, 67);
            qAbilityCooldown = 67;
        }
        else
        {
            if (isPlayer) GameManager.instance.StartCooldown(Upgrades.hiddinTalon, 90);
            qAbilityCooldown = 90;
        }


        isAssassinating = true;
        canSwitchAttackSprites = false;
        currentPigeonState.Value = 4;


        if (targetPigeon.transform.position.x > transform.position.x)
        {
            isPointingLeft.Value = true;
        }
        else
        {
            isPointingLeft.Value = false;
        }
        return true;
    }
    protected void StopMogging()
    {
        isMaxing = false;
        speedMod += .5f;

        ChangePigeonSizeServerRpc(false);
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

        PigeonAttack(GetBasicAttackValues(pos.x, pos.y), theAngle);


        StopCoroutine(StopSlam());
        isSlaming = false;
    }
    protected void LandAssassinate()
    {
        if (!isAssassinating) return;
        canSwitchAttackSprites = true;

        Vector2 pos = transform.position;
        pos = Vector2.MoveTowards(pos, abilityTargetPos, 0.1f);

        Vector3 targ = abilityTargetPos;
        targ.z = 0f;
        targ.x -= transform.position.x;
        targ.y -= transform.position.y;

        float angle = Mathf.Atan2(targ.y, targ.x) * Mathf.Rad2Deg;
        Quaternion theAngle = Quaternion.Euler(new Vector3(0, 0, angle));

        AttackProperties atkProp = GetBasicAttackValues(pos.x, pos.y);
        atkProp.isAssassinating = true;


        PigeonAttack(atkProp, theAngle);
        isAssassinating = false;
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
                if (isAssassinating) gameObject.layer = 10;
                else gameObject.layer = 8;

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
    protected AttackProperties GetBasicAttackValues(float x, float y)
    {
        AttackProperties atkProp = new()
        {
            pigeonID = NetworkObjectId,
            damage = damage,
            hasCriticalDamage = false,
            isEnchanted = false,
            isAssassin = false,
            hasKnockBack = false,
            isAssassinating = false,
            posX = x,
            posY = y,

        };
        if (pigeonUpgrades.TryGetValue(Upgrades.critcalDamage, out bool _)) atkProp.hasCriticalDamage = true;
        if (isMaxing) atkProp.damage *= 2;
        if (pigeonUpgrades.TryGetValue(Upgrades.brawler, out _)) atkProp.hasKnockBack = true;
        if (pigeonUpgrades.TryGetValue(Upgrades.assassin, out _)) atkProp.isAssassin = true;
        if (pigeonUpgrades.TryGetValue(Upgrades.enchanted, out _)) atkProp.isEnchanted = true;
        if (pigeonUpgrades.TryGetValue(Upgrades.bleed, out _)) atkProp.hasBleed = true;

        return atkProp;
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
    public void LevelUP()
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
            }
            else
            {
                GameManager.instance.ShowUpgrades();
            }
        }
    }
    private void AddRandomUpgrade()
    {
        Upgrades upgrade = Upgrades.pigeonOfGrowth;
        bool hasAbilitySlotUnlocked = false;

        for (int i = 0; i < 1000; i++)
        {
            upgrade = GameManager.instance.allPigeonUpgrades[Random.Range(0, GameManager.instance.allPigeonUpgrades.Count)];
            hasAbilitySlotUnlocked = false;

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
                case Upgrades.pigeonPoo:
                    if (hasAbilityE) hasAbilitySlotUnlocked = true; break;
                case Upgrades.razorFeathers:
                    if (hasAbilityM2) hasAbilitySlotUnlocked = true; break;
            }


            if (pigeonUpgrades.TryGetValue(upgrade, out bool _) ||
                (upgrade == Upgrades.hiddinTalon && !pigeonUpgrades.TryGetValue(Upgrades.assassin, out bool _)) ||
                hasAbilitySlotUnlocked)
            {
                continue;
            }
            else
            {
                break;
            }
        }

        if (pigeonUpgrades.TryGetValue(upgrade, out bool _) || hasAbilitySlotUnlocked)
        {
            AddUpgrade((Upgrades)Random.Range(-1, -3));
        }
        else
        {
            AddUpgrade(upgrade);
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
                if (chargedFeathers < 3 && pigeonUpgrades.ContainsKey(Upgrades.razorFeathers))
                {
                    //Starts slam Cooldown
                    chargedFeathers += 1;
                    if (chargedFeathers >= 3) m2AbilityCooldown = 0;
                    else
                    {
                        if (pigeonUpgrades.ContainsKey(Upgrades.overclock))
                        {
                            if (isPlayer) GameManager.instance.StartCooldown(Upgrades.razorFeathers, 3);
                            m2AbilityCooldown = 3;
                        }
                        else
                        {
                            if (isPlayer) GameManager.instance.StartCooldown(Upgrades.razorFeathers, 4);
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
        if (pigeonUpgrades.TryGetValue(Upgrades.overclock, out _)) yield return new WaitForSeconds(0.5f);
        else yield return new WaitForSeconds(1f);

        sprintOnCooldown = false;
    }
    private IEnumerator Respawn()
    {
        if (pigeonAI && pigeonAI.diesAfterDeath)
        {
            GameManager.instance.enemiesRemaining.Value--;
        }
        if (GameDataHolder.gameMode != "Supremacy" && isPlayer)
        {

            GameManager.instance.StartSpectating(secondsToRespawn);
        }


        bool suddenDeathBefore = false;
        if (GameManager.instance.isSuddenDeath.Value) suddenDeathBefore = true;
        yield return new WaitForSeconds(secondsToRespawn);

        if (pigeonAI && pigeonAI.diesAfterDeath)
        {
            Destroy(gameObject);
        }
        else if (suddenDeathBefore)
        {
            StopCoroutine(StopSlam());
            isKnockedOut.Value = true;
            if (isPlayer) GameManager.instance.StartSpectating();
            yield return null;
        }
        else
        {
            if (GameDataHolder.gameMode != "Supremacy")
            {
                secondsToRespawn += 3;
                GameManager.instance.StopSpectating();
            }
            StartFly();
        }
    }
    private IEnumerator StartBleed()
    {
        for (int i = 0; i < 5; i++)
        {
            yield return new WaitForSeconds(1);
            SpawnBloodServerRpc();
            currentHP.Value -= Mathf.RoundToInt(maxHp.Value * 0.03f);
            if (currentHP.Value <= 0 && !isKnockedOut.Value)
            {
                if (isPlayer) SaveDataManager.totalTimesKnockedOut++;
                if (GameManager.instance.isSuddenDeath.Value)
                {

                    currentHP.Value = 0;
                    isSlaming = false;
                    StopCoroutine(StopSlam());
                    isKnockedOut.Value = true;
                    if (isPlayer) GameManager.instance.StartSpectating();
                }
                else
                {
                    currentHP.Value = 0;
                    isSlaming = false;
                    StopCoroutine(StopSlam());
                    isKnockedOut.Value = true;
                    StartCoroutine(Respawn());
                }
            }
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
            else if (inPoo)
            {
                currentHP.Value -= 3;

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
        ChangePigeonSizeServerRpc(true);
        yield return new WaitForSeconds(20f);
        StopMogging();
    }
}
