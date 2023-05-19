using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;

public class Pigeon : NetworkBehaviour
{
    public Dictionary<Upgrades, int> pigeonUpgrades = new Dictionary<Upgrades, int>();
    public bool isKnockedOut = false;
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

    private bool canSwitchAttackSprites = true;
    
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

        if (pigeonAI && !isKnockedOut) hpBar.localScale = new Vector3((float)currentHP / maxHp, 0.097f, 1);
        Instantiate(bloodEffect, new Vector3(transform.position.x, transform.position.y, -1), transform.rotation);

        if(currentHP <= 0 && !isKnockedOut)
        {
            StopCoroutine(JumpAnimation());
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
                isKnockedOut = true;
                sr.flipY = true;
                sr.sortingOrder = -1;
                StartCoroutine(Respawn());
            }
        }
    }
    public void GainXP(int amnt)
    {
        if (isKnockedOut) return;
        xp += amnt;
        if(xp >= xpTillLevelUp)
        {
            LevelUP();
        }
    }
    public void Heal(int amt)
    {
        if (isKnockedOut) return;
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
        StartCoroutine(JumpAnimation());
        StartCoroutine(Regen());
        body.freezeRotation = true;
        currentHP = maxHp;
        gm = FindObjectOfType<GameManager>();
        gm.allpigeons.Add(this);
    }
    protected void PigeonAttack(Vector3 position)
    {
        GameObject attack = Instantiate(slash, position, transform.rotation);
        attack.GetComponent<HitScript>().pigeonThatDealtDamage = this;

        Vector3 targ = position;
        targ.z = 0f;

        Vector3 objectPos = transform.position;
        targ.x = targ.x - objectPos.x;
        targ.y = targ.y - objectPos.y;

        float angle = Mathf.Atan2(targ.y, targ.x) * Mathf.Rad2Deg;
        attack.transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));

        if (isSlaming)
        {
            attack.transform.localScale = new Vector3(6, 6, 1);
        }

        if (canSwitchAttackSprites)
        {
            canSwitchAttackSprites = false;
            StopCoroutine(JumpAnimation());
            sr.sprite = pigeonAttackSprites[UnityEngine.Random.Range(0, pigeonAttackSprites.Length)];
            StartCoroutine(DelayBeforeSpriteChange());
        }
    }
    protected void CheckDirection(Vector2 direction)
    {
        if (direction.x == 0) return;
        if(direction.x > 0)
        {
            sr.flipX = true;
        }
        else
        {
            sr.flipX = false;
        }
    }
    protected void StartSlam(Vector3 desiredSlamPos)
    {
        if (!canSlam) return;
        canSlam = false;
        isSlaming = true;
        slamPos = Vector2.MoveTowards(transform.position, desiredSlamPos, 5f);
        StopCoroutine(JumpAnimation());
        StartCoroutine(StopSlam());
        sr.sprite = pigeonSlamSprite;
    }
    protected void EndSlam()
    {
        if (!isSlaming) return;
        PigeonAttack(slamPos);
        StartCoroutine(SlamCoolDown());
        StopCoroutine(StopSlam());
        isSlaming = false;
        if (isPlayer)
        {
            StartCoroutine(gm.StartSlamCoolDown());
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
        isKnockedOut = false;
        bodyCollider.enabled = true;
        sr.sortingOrder = 0;
        sr.flipY = false;
        StartCoroutine(JumpAnimation());
    }
    private IEnumerator JumpAnimation()
    {
        while (true)
        {
            if (!canSwitchAttackSprites || isKnockedOut || isSlaming) yield break;
            if (body.velocity != Vector2.zero) sr.sprite = pigeonJumpSprite;
            yield return new WaitForSeconds(0.2f);
            if (!canSwitchAttackSprites || isKnockedOut || isSlaming) yield break;
            sr.sprite = defaultPigeonSprite;
            yield return new WaitForSeconds(0.2f);
        }
    }
    private IEnumerator DelayBeforeSpriteChange()
    {
        yield return new WaitForSeconds(0.15f);
        sr.sprite = defaultPigeonSprite;
        canSwitchAttackSprites = true;
        StartCoroutine(JumpAnimation());
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
