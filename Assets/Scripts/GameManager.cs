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

    public NetworkVariable<bool> isSuddenDeath = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> currentSecound = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<float> borderSize = new NetworkVariable<float>(0.40f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public Pigeon player;
    public List<Pigeon> allpigeons = new List<Pigeon>();
    public GameObject pigeonPrefab;
    public CinemachineVirtualCamera mainCamera;


    [SerializeField] AudioSource audioSource, clickSound;
    [SerializeField] AudioClip gigaChadSong;
    [SerializeField] int secondsTillSuddenDeath;
    [SerializeField] Image icecreamBar, healthBar, xpBar, sprintBar, staminaCooldownBar;
    [SerializeField] Button endScreenMainMenuButton;
    [SerializeField] Transform borderTransform;

    [SerializeField] GameObject endScreen, playerUI, gameUI, upgradeScreen, pauseMenu, cooldownIcon, spectateScreen, leaderboard, sprintUI, churchDoor;
    [SerializeField] TMP_Text endGameDescriptionText, spectatingText, upgradeDescText, slamCoolDownText;
    [SerializeField] GameObject endingLeaderboardTextPrefab, upgradeDescUI;
    [SerializeField] RectTransform leaderBoardTransform;
    [SerializeField] private GameObject[] upgradeDisplays;
    [SerializeField] private UpgradeDescriber[] upgradeDescibers;

    [SerializeField] GameObject FoodPrefab, nestPrefab, pooPrefab, upgradeHolder;
    [SerializeField] TextMeshProUGUI hpText, timeleftText;
    [SerializeField] TextMeshProUGUI levelText;
    [SerializeField] TextMeshProUGUI chageonName;
    [SerializeField] Slider volumeSlider;
    [SerializeField] GameObject playerPrefab;
    [SerializeField] Image[] upgradeButtonImages;
    [SerializeField] TextMeshProUGUI[] upgradeButtonText;
    [SerializeField] Sprite[] upgradeButtonSprites;
    [SerializeField] string[] upgradeNames;
    [SerializeField] string[] upgradeDesc;
    bool gameover = false;
    bool canSpawnFood = true;
    bool pauseMenuOpen = false;
    private Pigeon.Upgrades[] upgradesThatCanBeSelected = new Pigeon.Upgrades[3];
    private int currentSpectate = 0;
    private bool hasOpenedChurch = false;

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
        if (gameover) return;
        int currentAlivePigeons = GetSurvivingPigeonsCount();
        if (currentAlivePigeons <= 1)
        {
            gameover = true;
            //Someone Won the Game display credits
            ShowWinScreenClientRpc();
        }
    }
    private void Awake()
    {
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
            GameObject ob = Instantiate(endingLeaderboardTextPrefab, leaderBoardTransform);
            ob.GetComponent<TMP_Text>().text = pigeon.pigeonName.Value + " - LVL:" + pigeon.level.Value.ToString();

            if (!pigeon.isKnockedOut.Value)
            {
                endGameDescriptionText.text = pigeon.pigeonName.Value + " has defeated all of his rivals and ascended to sigma pigeon status";
            }
        }

        endScreen.SetActive(true);
    }
    public void AddUpgradeToDisply(int upgrade)
    {
        upgradeHolder.SetActive(true);
        upgradeDisplays[upgrade].SetActive(true);
    }
    public void ShowUpgradeDes(int desc)
    {
        upgradeDescUI.SetActive(true);
        upgradeDescText.text = upgradeDesc[desc];
    }
    public void CloseUpgradeDes()
    {
        upgradeDescText.text = "";
        upgradeDescUI.SetActive(false);
    }
    public void StartSpectating()
    {
        for (int i = 0; i < allpigeons.Count; i++)
        {
            Pigeon pigeon = allpigeons[i];
            if (!pigeon.isKnockedOut.Value)
            {
                currentSpectate = i;
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
        upgradeScreen.SetActive(true);

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
            upgradeDescibers[i].desc = (int)upgrade;
            upgradeButtonImages[i].sprite = upgradeButtonSprites[(int)upgrade];
            upgradeButtonText[i].text = upgradeNames[(int)upgrade];
        }

    }
    public void SelectUpgrade(int selected)
    {
        clickSound.Play();
        upgradeScreen.SetActive(false);
        upgradeDescUI.SetActive(false);
        player.AddUpgrade(upgradesThatCanBeSelected[selected]);
    }
    public IEnumerator StartSlamCoolDown()
    {

        slamCoolDownText.text = 3.ToString();
        yield return new WaitForSeconds(1);
        slamCoolDownText.text = 2.ToString();
        yield return new WaitForSeconds(1);
        slamCoolDownText.text = 1.ToString();

        yield return new WaitForSeconds(1);
        slamCoolDownText.text = "";

    }
    public void ShowSlamCoolDown()
    {
        cooldownIcon.SetActive(true);
    }
    public void SpectateNext()
    {
        currentSpectate++;
        if (currentSpectate > allpigeons.Count - 1)
        {
            currentSpectate = 0;
        }
        Pigeon pigeon = allpigeons[currentSpectate];
        mainCamera.Follow = pigeon.transform;
        spectatingText.text = "Spectating " + pigeon.pigeonName.Value;
    }
    public void SpectatePreviouse()
    {
        currentSpectate--;
        if (currentSpectate < 0)
        {
            currentSpectate = allpigeons.Count - 1;
        }
        Pigeon pigeon = allpigeons[currentSpectate];
        mainCamera.Follow = pigeon.transform;
        spectatingText.text = "Spectating " + pigeon.pigeonName.Value;
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
            float spawnX = transform.position.x;
            float spawnY = transform.position.y;
            Vector3 spawnPos = GetSpawnPos();
            GameObject player = Instantiate(playerPrefab, spawnPos, transform.rotation);
            player.GetComponent<NetworkObject>().SpawnAsPlayerObject(client, true);
        }

        for (int i = 0; i < GameDataHolder.botsToSpawn; i++)
        {

            Vector3 spawnPos = GetSpawnPos();
            GameObject pigeon = Instantiate(pigeonPrefab, spawnPos, transform.rotation);
            PigeonAI ai = pigeon.GetComponent<PigeonAI>();
            ai.SetAI(GameDataHolder.botDifficulty);
            pigeon.GetComponent<NetworkObject>().Spawn();
        }

        currentSecound.Value = secondsTillSuddenDeath;
        StartCoroutine(DepreciateIceCream());
    }
    public Vector3 GetSpawnPos()
    {
        float spawnX = transform.position.x;
        float spawnY = transform.position.y;
        float minRange = 8;
        float spawnRange = 17;

        if (Random.Range(0, 100) <= 50)
        {
            if (Random.Range(0, 100) <= 50)
            {
                spawnX += Random.Range(minRange, spawnRange);
                spawnY += Random.Range(-spawnRange, spawnRange);
            }
            else
            {
                spawnX += Random.Range(-minRange, -spawnRange);
                spawnY += Random.Range(-spawnRange, spawnRange);
            }
        }
        else
        {
            if (Random.Range(0, 100) <= 50)
            {
                spawnX += Random.Range(-spawnRange, spawnRange);
                spawnY += Random.Range(minRange, spawnRange);
            }
            else
            {
                spawnX += Random.Range(-spawnRange, spawnRange);
                spawnY += Random.Range(-minRange, -spawnRange);
            }
        }
        return new Vector3(spawnX, spawnY);
    }
    private void Start()
    {
        if (GameDataHolder.multiplayerName != "") chageonName.text = GameDataHolder.multiplayerName;
        else chageonName.text = "Chadgeon ";
    }
    public void SetFullScreen()
    {
        Screen.fullScreen = !Screen.fullScreen;
    }
    private void Update()
    {
        int minutes = Mathf.RoundToInt(currentSecound.Value / 60);
        borderTransform.localScale = new Vector3(borderSize.Value, borderSize.Value, 0);


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

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (pauseMenuOpen == true)
            {
                pauseMenuOpen = false;
                pauseMenu.SetActive(false);
            }
            else
            {
                pauseMenuOpen = true;
                pauseMenu.SetActive(true);

            }
        }

        if (Input.GetKey(KeyCode.Tab))
        {
            leaderboard.SetActive(true);
        }
        else
        {
            leaderboard.SetActive(false);
        }

        if (player.isSprinting.Value == true)
        {
            sprintUI.SetActive(true);
            sprintBar.fillAmount = player.stamina / player.maxStamina;
        }
        else
        {
            staminaCooldownBar.fillAmount = player.stamina / player.maxStamina;
            sprintUI.SetActive(false);
        }

        healthBar.fillAmount = (float)player.currentHP.Value / player.maxHp.Value;
        hpText.text = (float)player.currentHP.Value + "/" + player.maxHp.Value;
        xpBar.fillAmount = (float)player.xp.Value / player.xpTillLevelUp.Value;
        levelText.text = player.level.Value.ToString();

        if (IsServer && canSpawnFood && !isSuddenDeath.Value)
        {
            SpawnFoodServerRpc();
            if (!hasOpenedChurch && currentSecound.Value < 120)
            {
                hasOpenedChurch = true;
                OpenChurchDoorClientRpc();
            }
        }
        if (IsServer && isSuddenDeath.Value)
        {
            if (borderSize.Value > 0.05f)
                borderSize.Value -= Time.deltaTime / 300;
        }

        if (IsOwnedByServer && isSuddenDeath.Value)
        {
            CheckWinGame();
        }
    }
    public void ClosePauseMenu()
    {
        pauseMenuOpen = false;
        pauseMenu.SetActive(false);
    }

    [ServerRpc]
    private void SpawnFoodServerRpc()
    {
        StartCoroutine(SpawnFoodDelay());
    }
    IEnumerator SpawnFoodDelay()
    {
        canSpawnFood = false;
        Vector3 pos = GetSpawnPos();

        GameObject food = Instantiate(FoodPrefab, pos, transform.rotation);
        food.GetComponent<NetworkObject>().Spawn();
        yield return new WaitForSeconds(0.5f);
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
        //suddenDeathText.SetActive(true);
    }

    [ClientRpc]
    private void OpenChurchDoorClientRpc()
    {
        churchDoor.SetActive(false);
    }
}
