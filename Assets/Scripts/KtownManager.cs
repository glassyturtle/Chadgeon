using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class KtownManager : NetworkBehaviour
{
    public NetworkVariable<bool> hasChosen = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public int chosenModifier = 1;
    private int minionRelicsRemaining = 6;
    public static KtownManager instance;
    [SerializeField] private Transform[] minionSpawnLocations;


    [SerializeField] private List<DirtPileScript> dirtPiles;

    [SerializeField] private GameObject digParticle;
    [SerializeField] private GameObject foodPrefab;
    [SerializeField] GameObject minionHelmUI, minionGogglesUI, lostIlluminationScript, theChosenUI, upgradeDescUI, minionArtifacts, minionPigeons;
    [SerializeField] TMP_Text theChosenText, upgradeDescText, upgradeNameText;
    [SerializeField] string[] upgradeDesc, upgradeNames;

    private void Awake()
    {
        instance = this;
    }


    public IEnumerator RespawnDigSites()
    {
        while (true)
        {
            yield return new WaitForSeconds(20);
            if (GameManager.instance.gracePeriod)
            {
                int dirt = Random.Range(0, dirtPiles.Count);
                if (!dirtPiles[dirt].isActiveAndEnabled)
                {
                    ActivateDirtClientRpc(dirt, false);
                }
            }

            yield return null;
        }
    }
    public void ActivateMinion()
    {
        minionRelicsRemaining--;
        if (minionRelicsRemaining <= 0)
        {
            GameManager.instance.player.AddUpgrade(Pigeon.Upgrades.minionScript);
        }
    }
    public void DigDirt(DirtPileScript dirt)
    {
        if (!GameManager.instance.player.pigeonUpgrades.ContainsKey(Pigeon.Upgrades.minionHelmet) && Random.Range(0, 100) <= 10)
        {
            GameManager.instance.player.AddUpgrade(Pigeon.Upgrades.minionHelmet);
        }
        DigDirtServerRpc(dirt.transform.position, dirtPiles.IndexOf(dirt));
    }
    public void AddUpgradeToDisply(Pigeon.Upgrades upgrade)
    {
        switch (upgrade)
        {
            case Pigeon.Upgrades.minionHelmet:
                minionHelmUI.SetActive(true); break;
            case Pigeon.Upgrades.minionGoggles:
                if (GameManager.instance.player.pigeonUpgrades.ContainsKey(Pigeon.Upgrades.theChosen))
                {
                    minionArtifacts.SetActive(true);
                }
                minionGogglesUI.SetActive(true); break;
            case Pigeon.Upgrades.minionScript:
                lostIlluminationScript.SetActive(true); break;
            case Pigeon.Upgrades.theChosen:
                if (GameManager.instance.player.pigeonUpgrades.ContainsKey(Pigeon.Upgrades.minionGoggles))
                {
                    minionArtifacts.SetActive(true);
                }
                theChosenUI.SetActive(true); break;
        }
    }
    public void SacrificeYourself()
    {
        if (hasChosen.Value) return;
        UpdateChosenServerRpc();
        GameManager.instance.player.AddUpgrade(Pigeon.Upgrades.theChosen);
        theChosenText.text = "x" + Mathf.RoundToInt(GameManager.instance.player.level.Value / 5).ToString();
        chosenModifier = Mathf.RoundToInt(GameManager.instance.player.level.Value / 5);
        GameManager.instance.player.level.Value = 1;
        GameManager.instance.player.xpTillLevelUp = 30;
        GameManager.instance.player.speed = 900;
        GameManager.instance.player.damage = 10;
        GameManager.instance.player.maxHp.Value = 50;
        GameManager.instance.player.currentHP.Value = 50;
        GameManager.instance.player.xp = 0;
        StartCoroutine(SpawnFoodDelay());
    }
    public void PurchaseMinionGoggles()
    {
        GameManager.instance.player.pigeonUpgrades.Remove(Pigeon.Upgrades.minionHelmet);
        GameManager.instance.player.damgeReductionEnchantableModifier += 0.1f;
        GameManager.instance.player.AddUpgrade(Pigeon.Upgrades.minionGoggles);
        minionHelmUI.SetActive(false);
    }


    public void UnleashMinions()
    {
        SteamIntegration.instance.UnlockAchivement("minions");
        foreach (Transform location in minionSpawnLocations)
        {
            GameManager.instance.SpawnMinionDuringGameplayServerRpc(location.position);
        }
        HideAllMinionPigeonsClientRpc();
    }
    [ClientRpc]
    public void HideAllMinionPigeonsClientRpc()
    {
        minionPigeons.SetActive(false);
    }
    public void ShowUpgradeDes(int desc)
    {
        upgradeDescUI.SetActive(true);
        upgradeDescText.text = upgradeDesc[desc];
        upgradeNameText.text = upgradeNames[desc];
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdateChosenServerRpc()
    {
        hasChosen.Value = true;

    }
    [ServerRpc(RequireOwnership = false)]
    private void DigDirtServerRpc(Vector3 location, int dirtPileIndex)
    {
        GameObject particl = Instantiate(digParticle, location, transform.rotation);
        Debug.Log(particl.name);
        particl.GetComponent<NetworkObject>().Spawn();
        if (Random.Range(0, 100) <= 50)
        {
            //spawns worm
            GameManager.instance.SpawnPigeonDuringGameplay(location, 0, 16, "Worm");
        }
        ActivateDirtClientRpc(dirtPileIndex, true);
    }
    IEnumerator SpawnFoodDelay()
    {
        while (true)
        {
            yield return new WaitForSeconds(10);
            SpawnFoodServerRpc();
            yield return null;
        }

    }
    [ServerRpc(RequireOwnership = false)]
    public void SpawnFoodServerRpc()
    {
        hasChosen.Value = true;
        GameObject food = Instantiate(foodPrefab, GameManager.instance.player.transform.position, transform.rotation);
        food.GetComponent<NetworkObject>().Spawn();
    }
    [ClientRpc]
    private void ActivateDirtClientRpc(int index, bool hide)
    {
        if (hide)
        {
            dirtPiles[index].gameObject.SetActive(false);
        }
        else
        {
            dirtPiles[index].gameObject.SetActive(true);
        }
    }
}
