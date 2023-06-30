using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : NetworkBehaviour
{

    public List<Pigeon.Upgrades> allPigeonUpgrades;

    public NetworkVariable<bool> isSuddenDeath = new NetworkVariable<bool>(false);
    public NetworkVariable<int> currentSecound = new NetworkVariable<int>(0);
    public Pigeon player;
    public List<Pigeon> allpigeons = new List<Pigeon>();
    public GameObject pigeonPrefab;
    public CinemachineVirtualCamera mainCamera;


    [SerializeField] AudioSource audioSource, clickSound;
    [SerializeField] AudioClip gigaChadSong;
    [SerializeField] int secondsTillSuddenDeath;
    [SerializeField] Image icecreamBar;
    [SerializeField] Button mainMenuButton, endScreenMainMenuButton;

    [SerializeField] GameObject suddenDeathText, endScreen, playerUI, gameUI, upgradeScreen, pauseMenu, cooldownIcon, spectateScreen;
    [SerializeField] TMP_Text endGameDescriptionText, spectatingText;

    [SerializeField] RectTransform hpBar;
    [SerializeField] RectTransform xpBar;
    [SerializeField] GameObject FoodPrefab;
    [SerializeField] TextMeshProUGUI hpText, timeleftText, slamCoolDownText;
    [SerializeField] TextMeshProUGUI xpText;
    [SerializeField] Sprite defaultChad, hurtChad, criticalChad, blinkChad;
    [SerializeField] Image chadgeonPic;
    [SerializeField] Image chadgeonDetialPic;
    [SerializeField] TextMeshProUGUI chageonName;
    [SerializeField] Slider volumeSlider;
    [SerializeField] GameObject playerPrefab;
    [SerializeField] Image[] upgradeButtonImages;
    [SerializeField] TextMeshProUGUI[] upgradeButtonText;
    [SerializeField] Sprite[] upgradeButtonSprites;
    [SerializeField] int[] spriteLocationsForEachUpgrade;
    [SerializeField] string[] upgradeNames;

    bool canSpawnFood = true;
    private Pigeon.Upgrades[] upgradesThatCanBeSelected = new Pigeon.Upgrades[3];

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += SceneManager_OnLoadEventCompleted;
    }
    public void ChangeVolume()
    {
        AudioListener.volume = volumeSlider.value;
    }
    public int GetSurvivingPigeonsCount()
    {
        int survivors = 0;
        foreach (Pigeon pigeon in allpigeons)
        {
            if (!pigeon.isKnockedOut.Value) survivors++;
        }
        return survivors;
    }
    public void CheckWinGame()
    {
        int currentAlivePigeons = GetSurvivingPigeonsCount();
        Debug.Log(currentAlivePigeons);
        if (currentAlivePigeons <= 1)
        {
            //Someone Won the Game display credits
            ShowWinScreenClientRpc();
        }
    }
    private void Awake()
    {
        mainMenuButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene("MainMenu");
        });
        endScreenMainMenuButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene("MainMenu");
        });
    }

    [ClientRpc]
    public void ShowWinScreenClientRpc()
    {
        gameUI.SetActive(false);

        foreach (Pigeon pigeon in allpigeons)
        {
            if (!pigeon.isKnockedOut.Value)
            {
                endGameDescriptionText.text = pigeon.pigeonName.Value + " has defeated all of his rivals and ascended to sigma pigeon status";

                break;
            }
        }

        endScreen.SetActive(true);
    }

    public void StartSpectating()
    {
        foreach (Pigeon pigeon in allpigeons)
        {
            if (!pigeon.isKnockedOut.Value)
            {
                playerUI.SetActive(false);
                spectateScreen.SetActive(true);
                spectatingText.text = "Spectating " + pigeon.pigeonName.Value;
                mainCamera.Follow = pigeon.transform;
                break;
            }
        }

    }
    public void ReturnToMainMenu()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene("MainMenu");
    }
    public void ShowUpgrades()
    {
        //when the player achives lvl 5 
        Dictionary<Pigeon.Upgrades, int> upgradesUsed = new Dictionary<Pigeon.Upgrades, int>();

        for (int i = 0; i < upgradesThatCanBeSelected.Length; i++)
        {
            Pigeon.Upgrades upgrade = Pigeon.Upgrades.regen;

            for (int x = 0; x < 1000; x++)
            {
                upgrade = allPigeonUpgrades[Random.Range(0, allPigeonUpgrades.Count)];
                if (!upgradesUsed.ContainsKey(upgrade) && !player.pigeonUpgrades.ContainsKey(upgrade)) break;
            }
            upgradesUsed.Add(upgrade, 1);
            upgradesThatCanBeSelected[i] = upgrade;
            upgradeButtonImages[i].sprite = upgradeButtonSprites[spriteLocationsForEachUpgrade[(int)upgrade]];
            upgradeButtonText[i].text = upgradeNames[(int)upgrade];
        }

        upgradeScreen.SetActive(true);
    }
    public void SelectUpgrade(int selected)
    {
        clickSound.Play();
        upgradeScreen.SetActive(false);
        player.AddUpgrade(upgradesThatCanBeSelected[selected]);
    }
    public void OpenPauseMenu(bool isOpen)
    {
        clickSound.Play();
        if (isOpen)
        {
            Time.timeScale = 1;
            pauseMenu.SetActive(false);
            gameUI.SetActive(true);
        }
        else
        {
            Time.timeScale = 0;
            pauseMenu.SetActive(true);
            gameUI.SetActive(false);
        }
    }
    public IEnumerator StartSlamCoolDown()
    {
        slamCoolDownText.text = 3.ToString();
        yield return new WaitForSeconds(1);
        slamCoolDownText.text = 2.ToString();
        yield return new WaitForSeconds(1);
        slamCoolDownText.text = 1.ToString();
        yield return new WaitForSeconds(1);
        slamCoolDownText.text = 0.ToString();
    }
    public void ShowSlamCoolDown()
    {
        cooldownIcon.SetActive(true);
    }
    public void SpectateNext()
    {

    }

    public void DestroyFoodObject(food foodie)
    {
        DestroyFoodObjectServerRpc(foodie.NetworkObject);
    }
    [ServerRpc(RequireOwnership = false)]
    public void DestroyFoodObjectServerRpc(NetworkObjectReference foodie)
    {
        foodie.TryGet(out NetworkObject foodieNetObj);
        if (foodieNetObj)
        {
            food foodieObj = foodieNetObj.GetComponent<food>();
            foodieObj.DestroySelf();
        }
    }


    private void SceneManager_OnLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        foreach (ulong client in NetworkManager.Singleton.ConnectedClientsIds)
        {
            GameObject player = Instantiate(playerPrefab, transform.position, transform.rotation);
            player.GetComponent<NetworkObject>().SpawnAsPlayerObject(client, true);
        }
    }

    private void Start()
    {
        if (IsOwnedByServer)
        {
            currentSecound.Value = secondsTillSuddenDeath;

            for (int i = 0; i < 0; i++)
            {
                GameObject pigeon = Instantiate(pigeonPrefab, new Vector3(Random.Range(-13f, 13f), Random.Range(-11f, 19f), 0), transform.rotation);
                PigeonAI ai = pigeon.GetComponent<PigeonAI>();
                ai.SetAI(SuperGM.difficulty);
            }
            StartCoroutine(DepreciateIceCream());
        }
    }
    public void SetFullScreen()
    {
        Screen.fullScreen = !Screen.fullScreen;
    }
    private void Update()
    {
        int minutes = Mathf.RoundToInt(currentSecound.Value / 60);
        int seconds = currentSecound.Value % 60;
        if (seconds < 10)
        {
            timeleftText.text = minutes + ":0" + seconds;
        }
        else
        {
            timeleftText.text = minutes + ":" + seconds;
        }
        icecreamBar.fillAmount = (float)currentSecound.Value / secondsTillSuddenDeath;


        if (!audioSource.isPlaying)
        {
            audioSource.clip = gigaChadSong;
            audioSource.loop = true;
            audioSource.Play();
        }
        if (!player) return;
        chageonName.text = GameDataHolder.multiplayerName + " " + " lvl: " + player.level.Value;

        if (player.currentHP.Value <= 0)
        {
            chadgeonDetialPic.sprite = criticalChad;
            hpBar.localScale = new Vector3(0, 1, 1);
            hpText.text = 0 + "/" + player.maxHp.Value;
            chadgeonDetialPic.color = Color.white;

        }
        else
        {
            hpBar.localScale = new Vector3((float)player.currentHP.Value / player.maxHp.Value, 1, 1);
            hpText.text = player.currentHP.Value + "/" + player.maxHp.Value;

            if (player.currentHP.Value >= player.maxHp.Value / 2)
            {
                chadgeonDetialPic.sprite = null;
                chadgeonDetialPic.color = new Color(0, 0, 0, 0);
            }
            else
            {
                chadgeonDetialPic.sprite = hurtChad;
                chadgeonDetialPic.color = Color.white;
            }
        }
        xpBar.localScale = new Vector3((float)player.xp.Value / player.xpTillLevelUp.Value, 1, 1);
        xpText.text = player.xp.Value + "/" + player.xpTillLevelUp.Value;

        if (IsServer && canSpawnFood && !isSuddenDeath.Value)
        {
            SpawnFoodServerRpc();
        }

    }

    [ServerRpc]
    private void SpawnFoodServerRpc()
    {
        StartCoroutine(SpawnFoodDelay());
    }
    IEnumerator SpawnFoodDelay()
    {
        canSpawnFood = false;
        GameObject food = Instantiate(FoodPrefab, new Vector3(Random.Range(-13f, 13f), Random.Range(-11f, 19f), 0), transform.rotation);
        food.GetComponent<NetworkObject>().Spawn();
        yield return new WaitForSeconds(0.35f);
        canSpawnFood = true;
    }
    IEnumerator DepreciateIceCream()
    {
        while (true)
        {
            yield return new WaitForSeconds(1);
            currentSecound.Value--;
            if (currentSecound.Value <= 0)
            {
                ActivateSuddenDeathUIClientRpc();
                isSuddenDeath.Value = true;
                yield break;

            }
            yield return null;
        }
    }

    [ClientRpc]
    private void ActivateSuddenDeathUIClientRpc()
    {
        suddenDeathText.SetActive(true);
    }
}
