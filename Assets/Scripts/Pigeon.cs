using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Pigeon : NetworkBehaviour
{
    public NetworkVariable<bool> isKnockedOut = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> maxHp = new(20, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> currentHP = new(20, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> level = new(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    [SerializeField] private NetworkVariable<bool> isPointingLeft = new(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    [SerializeField] private NetworkVariable<int> currentPigeonState = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);


    public bool isFlying;
    public bool isSlaming;
    public int xp;
    public int xpTillLevelUp;
    public int flock;
    public string pigeonName;
    public Dictionary<Upgrades, bool> pigeonUpgrades = new();
    public bool isSprinting = false;
    public float stamina = 5, maxStamina = 5;


    [SerializeField] protected GameManager gm;
    [SerializeField] protected TextMesh displayText;
    [SerializeField] protected TextMesh flockDisplayText;
    [SerializeField] protected Rigidbody2D body;
    [SerializeField] protected float speed;
    [SerializeField] protected int damage;


    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] hitClips;
    [SerializeField] private AudioClip[] cruchSound;
    [SerializeField] private GameObject bloodEffect;
    [SerializeField] private GameObject healthBarGameobject;
    [SerializeField] private Transform hpBar;
    [SerializeField] private SpriteRenderer sr, bodysr, headsr;
    public bool isPlayer = false;
    [SerializeField] private PigeonAI pigeonAI;
    [SerializeField] private HitScript slash;

    protected int secTillSlam = 5;
    protected bool canSlam = false;
    protected Vector3 slamPos;
    protected float speedMod = 1;
    private float knockbackMod = 1;
    protected float staminaRecoveryRate = 1;
    protected bool inBorder = true;
    protected bool sprintOnCooldown = false;
    protected float hitColldown = 0.3f;


    private float regen = 0.02f;
    protected bool canSwitchAttackSprites = true;
    private int currentPigeonAttackSprite;

    //Skins
    [SerializeField] private int skinBase;
    [SerializeField] private int skinBody;
    [SerializeField] private int skinHead;


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
    [ServerRpc]
    private void HandleKnockbackServerRpc(Vector2 force)
    {
        body.AddForce(force);
    }
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
            if (gm.isSuddenDeath.Value)
            {

                hasBeenKO = true;
                totalDamageTaking -= currentHP.Value;
                currentHP.Value = 0;
                isSlaming = false;
                StopCoroutine(StopSlam());
                isKnockedOut.Value = true;
                if (isPlayer) gm.StartSpectating();
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
        if (!IsOwner) return;
        pigeonUpgrades.Add(upgrade, true);
        if (!pigeonAI) gm.AddUpgradeToDisply((int)upgrade);
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
                if (isPlayer) gm.ShowSlamCoolDown();
                canSlam = true;
                break;
        }
    }
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
    [ServerRpc]
    private void UpdateSeverPigeonsServerRpc(GameManager.PigeonInitializeProperties pigeonData)
    {
        GameManager.instance.pigeonStartData.Add(pigeonData);
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
        gm = GameManager.instance;
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
    protected bool StartSlam(Vector3 desiredSlamPos)
    {
        if (!canSlam) return false;
        canSlam = false;
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
        slamPos = Vector2.MoveTowards(transform.position, desiredSlamPos, 5f);
        StartCoroutine(StopSlam());
        return true;
    }
    protected void EndSlam()
    {
        if (!isSlaming) return;
        canSwitchAttackSprites = true;

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
        isSlaming = false;
        if (isPlayer)
        {
            StartCoroutine(gm.StartSlamCoolDown());
        }
    }
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
    protected void SyncPigeonAttributes()
    {
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

    [ServerRpc]
    private void SpawnBloodServerRpc()
    {
        GameObject blood = Instantiate(bloodEffect, new Vector3(transform.position.x, transform.position.y, -1), transform.rotation);
        blood.GetComponent<NetworkObject>().Spawn();
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
            if (isPlayer) gm.StartSpectating();
            yield return null;
        }
        else
        {
            StartFly();
        }
    }
    private void StartFly()
    {
        if (!IsOwner) return;
        isFlying = true;

        slamPos = gm.GetSpawnPos();
        //StartCoroutine(StopFlight());
        StartCoroutine(FlyAnimation());
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
                    gm.StartSpectating();
                }
            }
            else
            {
                HealServer(regen * maxHp.Value);
            }
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
    protected IEnumerator StartSprintCooldown()
    {
        yield return new WaitForSeconds(1);
        sprintOnCooldown = false;
    }

    [ServerRpc]
    private void StopMomentumServerRpc(Vector3 pos)
    {
        body.velocity = Vector2.zero;
        transform.position = pos;
    }
    protected void StopFlying()
    {
        StopMomentumServerRpc(slamPos);
        isFlying = false;
        if (pigeonUpgrades.ContainsKey(Upgrades.slam)) canSlam = true;
        currentHP.Value = maxHp.Value;
        stamina = maxStamina;
        isKnockedOut.Value = false;

    }

}
