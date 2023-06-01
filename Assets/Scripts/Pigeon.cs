using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine.UIElements;

public class Pigeon : NetworkBehaviour
{
    public Dictionary<Upgrades, int> pigeonUpgrades = new Dictionary<Upgrades, int>();
    public NetworkVariable<bool> isKnockedOut = new NetworkVariable<bool>(false);
    public int power = 1;
    public int maxHp;
    public int currentHP;
    public int xp;
    public int xpTillLevelUp;
    public int level = 1;

    [SerializeField] protected GameManager gm;
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip[] hitClips;
    [SerializeField] AudioClip[] cruchSound;

    [SerializeField] GameObject bloodEffect;
    [SerializeField] protected Rigidbody2D body;
    [SerializeField] protected CircleCollider2D bodyCollider;
    [SerializeField] protected float speed;
    [SerializeField] protected Transform hpBar;


    protected int secTillSlam = 5, secTillFly = 15;
    protected bool canSlam = false, canfly = false, isSlaming, canDeCollide = false;
    protected Vector3 slamPos;
    

    [SerializeField] PigeonAI pigeonAI;
    [SerializeField] private GameObject slash;
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] Sprite[] pigeonAttackSprites;
    [SerializeField] private Sprite defaultPigeonSprite, pigeonJumpSprite, pigeonSlamSprite;
    [SerializeField] bool isPlayer = false;

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
    public void OnPigeonHit(Pigeon AttackinPigeon)
    {
        if (AttackinPigeon == null || AttackinPigeon == this) return;

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


        int totalDamageTaking = AttackinPigeon.power;
        if (isSlaming) totalDamageTaking /= 2;
        if(AttackinPigeon.pigeonUpgrades.ContainsKey(Upgrades.critcalDamage) && UnityEngine.Random.Range(0,100) <= 10)
        {
            totalDamageTaking *= 4;
        }
        if (pigeonUpgrades.ContainsKey(Upgrades.tough))
        {
            totalDamageTaking = Mathf.RoundToInt(totalDamageTaking * 0.8f);
        }
        currentHP -= totalDamageTaking;

        if (AttackinPigeon.pigeonUpgrades.ContainsKey(Upgrades.lifeSteal))
        {
            AttackinPigeon.Heal(AttackinPigeon.power / 3);
        }

        if (AttackinPigeon.pigeonUpgrades.ContainsKey(Upgrades.knockBack))
        {
            body.AddForce(direction * totalDamageTaking * 50);

        }
        else
        {
            body.AddForce(direction * totalDamageTaking * 10);
        }

        if (pigeonAI && !isKnockedOut.Value) hpBar.localScale = new Vector3((float)currentHP / maxHp, 0.097f, 1);
        GameObject blood = Instantiate(bloodEffect, new Vector3(transform.position.x, transform.position.y, -1), transform.rotation);
        blood.GetComponent<NetworkObject>().Spawn();

        if(currentHP <= 0 && !isKnockedOut.Value)
        {
            isSlaming = false;
            StopCoroutine(StopSlam());

            AttackinPigeon.GainXP((level * 5));

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

                if (pigeonAI) hpBar.localScale = new Vector3(0, 0.097f, 1);
                isKnockedOut.Value = true;
                sr.flipY = true;
                sr.sortingOrder = -1;
                StartCoroutine(Respawn());
            }
        }
    }
    public void GainXP(int amnt)
    {
        if (isKnockedOut.Value) return;
        xp += amnt;
        if(xp >= xpTillLevelUp)
        {
            LevelUP();
        }
    }
    public void Heal(int amt)
    {
        if (isKnockedOut.Value) return;
        currentHP += amt;
        if (currentHP > maxHp) currentHP = maxHp;
        if (pigeonAI)
        {
            hpBar.localScale = new Vector3((float)currentHP / maxHp, 0.097f, 1);
        }
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
                maxHp += 50;
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
        if(IsOwner) StartCoroutine(JumpAnimation());
        StartCoroutine(Regen());
        body.freezeRotation = true;
        currentHP = maxHp;
        gm = FindObjectOfType<GameManager>();
        gm.allpigeons.Add(this);
    }

    [ServerRpc(RequireOwnership = false)]
    protected void PigeonAttackServerRpc(Vector3 position, Quaternion theAngle)
    {

        GameObject attack = Instantiate(slash, position, theAngle);
        attack.GetComponent<NetworkObject>().Spawn(true);
        attack.GetComponent<HitScript>().pigeonThatDealtDamage = this;



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

        PigeonAttackServerRpc(slamPos, theAngle);

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
        if (isKnockedOut.Value)
        {
            sr.flipY = true;
        }
        else
        {
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
        level++;
        xp -= xpTillLevelUp;
        xpTillLevelUp =Mathf.RoundToInt(xpTillLevelUp*  1.15f);
        power++;
        maxHp += 5;
        currentHP += 5;
        speed+= 20;
         
        if(pigeonAI != null)
        {
            pigeonAI.AILevelUP();
        }

        if(0 == level % 5)
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
        currentHP = maxHp;
        if (pigeonAI) hpBar.localScale = new Vector3(1, 0.097f, 1);
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
            Heal(regen);
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
