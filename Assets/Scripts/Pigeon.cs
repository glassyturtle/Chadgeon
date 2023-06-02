using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine.UIElements;
using System.Net.NetworkInformation;

public class Pigeon : NetworkBehaviour
{
    public Dictionary<Upgrades, int> pigeonUpgrades = new();
    public NetworkVariable<bool> isKnockedOut = new NetworkVariable<bool>(false);
    public NetworkVariable<int> power = new NetworkVariable<int>(5);
    public NetworkVariable<int> maxHp = new NetworkVariable<int>(50);
    public NetworkVariable<int> currentHP = new NetworkVariable<int>(50);
    public NetworkVariable<int> xp = new NetworkVariable<int>(0);
    public NetworkVariable<int> xpTillLevelUp = new NetworkVariable<int>(20);
    public NetworkVariable<int> level = new NetworkVariable<int>(1);

    [SerializeField] protected GameManager gm;
    [SerializeField] protected string pigeonName;
    [SerializeField] protected TextMesh displayText;
    [SerializeField] protected Rigidbody2D body;
    [SerializeField] protected CircleCollider2D bodyCollider;
    [SerializeField] protected float speed;
    [SerializeField] protected NetworkObject no;

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] hitClips;
    [SerializeField] private AudioClip[] cruchSound;
    [SerializeField] private GameObject bloodEffect;
    [SerializeField] private GameObject healthBarGameobject;
    [SerializeField] private Transform hpBar;
    [SerializeField] private GameObject slash;
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Sprite defaultPigeonSprite, pigeonJumpSprite, pigeonSlamSprite;
    [SerializeField] private Sprite[] pigeonAttackSprites;
    [SerializeField] private bool isPlayer = false;
    [SerializeField] private PigeonAI pigeonAI;

    protected int secTillSlam = 5, secTillFly = 15;
    protected bool canSlam = false, canfly = false, isSlaming, canDeCollide = false;
    protected Vector3 slamPos;
    
    private int regen = 3;
    private NetworkVariable<bool> isPointingLeft = new NetworkVariable<bool>(true);
    private NetworkVariable<bool> isSpriteNotHopping = new NetworkVariable<bool>(true);
    private NetworkVariable<bool> canSwitchAttackSprites = new NetworkVariable<bool>(true);
    private NetworkVariable<int> currentPigeonAttackSprite = new NetworkVariable<int>(0);


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
    
    public void PlayEatSound()
    {
        if (!audioSource.isPlaying)
        {
            audioSource.clip = cruchSound[UnityEngine.Random.Range(0, cruchSound.Length)];
            audioSource.Play();
        }
    }
    [ServerRpc(RequireOwnership = true)]
    public void OnPigeonHitServerRpc(ulong index)
    {
        NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(index, out NetworkObject ob);
        if (!ob)  
        {
            Debug.Log("WTF:");
            return;
        }
        Pigeon AttackinPigeon = ob.GetComponent<Pigeon>();

        if (AttackinPigeon == null || index == GetComponent<NetworkObject>().NetworkObjectId) return;

        if (pigeonUpgrades.ContainsKey(Upgrades.dodge))
        {
            if (UnityEngine.Random.Range(0, 100) <= 20)
            {
                return;
            }
        }

        if (!audioSource.isPlaying)
        {
            audioSource.clip = hitClips[UnityEngine.Random.Range(0, hitClips.Length)];
            audioSource.Play();
        }

        Vector2 direction = transform.position -AttackinPigeon.transform.position ;
        direction.Normalize();


        int totalDamageTaking = AttackinPigeon.power.Value;
        if (isSlaming) totalDamageTaking /= 2;
        if(AttackinPigeon.pigeonUpgrades.ContainsKey(Upgrades.critcalDamage) && UnityEngine.Random.Range(0,100) <= 10)
        {
            totalDamageTaking *= 4;
        }
        if (pigeonUpgrades.ContainsKey(Upgrades.tough))
        {
            totalDamageTaking = Mathf.RoundToInt(totalDamageTaking * 0.8f);
        }
        currentHP.Value -= totalDamageTaking;

        if (AttackinPigeon.pigeonUpgrades.ContainsKey(Upgrades.lifeSteal))
        {
            AttackinPigeon.HealServerRpc(AttackinPigeon.power.Value / 3);
        }

        if (AttackinPigeon.pigeonUpgrades.ContainsKey(Upgrades.knockBack))
        {
            body.AddForce(direction * totalDamageTaking * 50);

        }
        else
        {
            body.AddForce(direction * totalDamageTaking * 10);
        }

        GameObject blood = Instantiate(bloodEffect, new Vector3(transform.position.x, transform.position.y, -1), transform.rotation);
        blood.GetComponent<NetworkObject>().Spawn();

        if(currentHP.Value <= 0 && !isKnockedOut.Value)
        {
            isSlaming = false;
            StopCoroutine(StopSlam());

            AttackinPigeon.GainXPServerRpc((level.Value * 5));

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
                    {
                        gm.Win();
                    }
                    Destroy(gameObject);
                }
            }
            else
            {

                isKnockedOut.Value = true;
                sr.flipY = true;
                sr.sortingOrder = -1;
                StartCoroutine(Respawn());
            }
        }
    }
    [ServerRpc]
    public void GainXPServerRpc(int amnt)
    {
        Debug.Log(IsOwner);
        if (!IsOwner) return;
        if (isKnockedOut.Value) return;
        xp.Value += amnt;
        if(xp.Value >= xpTillLevelUp.Value)
        {
            LevelUP();
        }
    }
    [ServerRpc]
    public void HealServerRpc(int amt)
    {
        if (isKnockedOut.Value) return;
        currentHP.Value += amt;
        if (currentHP.Value > maxHp.Value) currentHP.Value =maxHp.Value;

    }
    public void AddUpgrade(Upgrades upgrade)
    {
        pigeonUpgrades.Add(upgrade, 0);

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

    [ServerRpc(RequireOwnership = false)]
    protected void PigeonAttackServerRpc(Vector3 position, Quaternion theAngle, ulong id)
    {
        GameObject attack = Instantiate(slash, position, theAngle);
        attack.GetComponent<HitScript>().indexOfDamagingPigeon.Value = id;
        attack.GetComponent<NetworkObject>().Spawn(true);

        if (isSlaming)
        {
            attack.transform.localScale = new Vector3(6, 6, 1);
        }

        if (canSwitchAttackSprites.Value)
        {
            canSwitchAttackSprites.Value = false;
            currentPigeonAttackSprite.Value = UnityEngine.Random.Range(0, pigeonAttackSprites.Length);
            StartCoroutine(DelayBeforeSpriteChange());
        }
    }

    protected void CheckDirection(Vector2 direction)
    {
        if (direction.x == 0) return;
        if(direction.x > 0)
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

        PigeonAttackServerRpc(slamPos, theAngle,no.NetworkObjectId);

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
            displayText.text = pigeonName + " Lvl:" + level.Value.ToString();
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
    private void UpdateNotHoppingServerRpc(bool isHoping)
    {
        if(body.velocity != Vector2.zero && isHoping)
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
        xpTillLevelUp.Value = Mathf.RoundToInt(xpTillLevelUp.Value *  1.15f);
        power.Value++;
        maxHp.Value += 5;
        currentHP.Value += 5;
        speed+= 20;
         
        if(pigeonAI != null)
        {
            pigeonAI.AILevelUP();
        }

        if(0 == level.Value % 5)
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
            Upgrades upgrade = gm.allPigeonUpgrades[UnityEngine.Random.Range(0, gm.allPigeonUpgrades.Count)];
            if (pigeonUpgrades.TryGetValue(upgrade, out int value))
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
            UpdateNotHoppingServerRpc(false);
            yield return new WaitForSeconds(0.2f);
            if (!canSwitchAttackSprites.Value)
            {
                yield return new WaitForSeconds(0.15f);
                continue;
            }
            UpdateNotHoppingServerRpc(true);
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
            HealServerRpc(regen);
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
