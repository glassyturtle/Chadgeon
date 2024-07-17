using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Pigeon;

public class GameManager : NetworkBehaviour
{
    public static GameManager instance;
    public List<Pigeon.Upgrades> allPigeonUpgrades;

    public NetworkVariable<bool> isSuddenDeath = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> currentSecond = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> enemiesRemaining = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> waveNumber = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<float> borderSize = new NetworkVariable<float>(0.40f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public Pigeon player;
    public List<Pigeon> allpigeons = new List<Pigeon>();
    public GameObject pigeonPrefab;
    public CinemachineCamera mainCamera;
    public bool isSpectating = false;
    public bool gracePeriod = false;
    private bool lookingForConeToBuild = true;
    [SerializeField] AudioSource audioSource, mewingSorce;
    [SerializeField] AudioClip[] musicTracks;
    private int currentTrack = 0;
    [SerializeField] int secondsTillSuddenDeath;
    [SerializeField] Image icecreamBar, healthBar, xpBar, sprintBar, staminaCooldownBar, dialougeImage;
    [SerializeField] Button endScreenMainMenuButton;
    [SerializeField] Transform borderTransform;

    [SerializeField] GameObject endScreen, playerUI, gameUI, pauseMenu, spectateScreen, upgradeScreen, minimapUI, sprintUI, churchDoor, goonPrefab, coneToDefend, upgradeUi, dialougeUI;
    [SerializeField] TMP_Text endGameDescriptionText, spectatingText, upgradeDescText, upgradeNameText, featherChargeCounter, gameObjectiveText, respawnTimer, pauseMenuText, dialougeText, dialougeNameText;
    [SerializeField] GameObject upgradeDescUI;
    [SerializeField] private GameObject[] upgradeDisplays;
    [SerializeField] private UpgradeDescriber[] upgradeDescibers;
    [SerializeField] private List<Transform> spawnLocations;
    [SerializeField] AbilityCooldownIconScript[] cooldownIcons;
    [SerializeField] GameObject FoodPrefab, upgradeHolder, loadingPigeonsText, suddenDeathText, iceCreamUI, hostDCUI, unbuiltConePrefab, builtConePrefab;
    [SerializeField] TextMeshProUGUI hpText, timeleftText;
    [SerializeField] TextMeshProUGUI levelText;
    [SerializeField] TextMeshProUGUI chageonName;
    [SerializeField] GameObject playerPrefab, evacZone;
    [SerializeField] GameObject extraUpgradeSelectionGameobject;
    [SerializeField] Image[] upgradeButtonImages;
    [SerializeField] Sprite[] upgradeButtonSprites;
    [SerializeField] string[] upgradeNames;
    [SerializeField] string[] upgradeDesc;
    [SerializeField] Sprite[] repeatableUpgradeButtonSprites;
    [SerializeField] string[] repeatableUpgradeNames;
    [SerializeField] string[] repeatableUpgradeDesc;
    [SerializeField] GameObject[] specialDialougeObjects;
    public bool gameover = false;
    public bool canSpawnFood = true;
    bool pauseMenuOpen = false;
    private Pigeon.Upgrades[] upgradesThatCanBeSelected = new Pigeon.Upgrades[4];
    private int currentSpectate = 0;
    bool uiOpen = true;
    private bool hasOpenedChurch = false;
    private bool selectingUpgrades = false;
    private List<GameObject> unbuiltConeGameobjects = new();
    private bool isSwapingTracks = false;
    [SerializeField] LocalizeStringEvent upgradeDesLocalization;

    public List<PigeonInitializeProperties> pigeonStartData = new();
    public struct PigeonInitializeProperties : INetworkSerializable
    {
        public ulong pigeonID;
        public int flock;
        public int skinHead;
        public int skinBody;
        public int skinBase;
        public string pigeonName;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref pigeonID);
            serializer.SerializeValue(ref flock);
            serializer.SerializeValue(ref skinHead);
            serializer.SerializeValue(ref skinBody);
            serializer.SerializeValue(ref skinBase);
            serializer.SerializeValue(ref pigeonName);
        }
    }

    private void Awake()
    {
        if (GameDataHolder.gameMode == 2)
        {
            GameDataHolder.botsToSpawn = 0;
            GameDataHolder.gameMode = 0;
        }
        instance = this;
        endScreenMainMenuButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene("MainMenu");
        });

        if (GameDataHolder.multiplayerName != "") chageonName.text = GameDataHolder.multiplayerName;
        else chageonName.text = "Chadgeon ";

        if (GameDataHolder.gameMode == 0)
        {
            gameObjectiveText.gameObject.SetActive(false);
            gracePeriod = true;

        }
        else
        {
            iceCreamUI.SetActive(false);
            gracePeriod = false;
            gameObjectiveText.gameObject.SetActive(true);
        }
    }
    private void Start()
    {

        NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_OnClientDisconnectCallback;
    }
    public override void OnNetworkSpawn()
    {

        if (IsServer)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += SceneManager_OnLoadEventCompleted;
        }
    }
    public void DamageCone()
    {
        currentSecond.Value--;
        if (IsServer && currentSecond.Value <= 0)
        {
            currentSecond.Value = -1;
            string victoryText = "The Goons have eaten the the last of your Cone!";
            ShowWinScreenClientRpc(victoryText, -1, 1);
        }

    }
    private void Update()
    {

        if (currentSecond.Value == -1) return;
        int minutes = Mathf.RoundToInt(currentSecond.Value / 60);
        borderTransform.localScale = new Vector3(borderSize.Value, borderSize.Value, 0);


        if (GameDataHolder.gameMode == 0)
        {
            int seconds = currentSecond.Value % 60;
            if (seconds < 10)
            {
                timeleftText.text = minutes + ":0" + seconds;
            }
            else
            {
                timeleftText.text = minutes + ":" + seconds;
            }
            icecreamBar.fillAmount = (float)currentSecond.Value / secondsTillSuddenDeath;

            if (IsServer && isSuddenDeath.Value)
            {
                if (borderSize.Value > 0.01f)
                    borderSize.Value -= Time.deltaTime / 400;
            }
            if (IsOwnedByServer && isSuddenDeath.Value)
            {
                CheckWinGame();
            }
        }
        else
        {

            if (isSuddenDeath.Value == true)
            {
                gameObjectiveText.text = "Get to the flight zone! " + currentSecond.Value + " seconds left";
            }
            else
            {
                timeleftText.text = Mathf.RoundToInt((currentSecond.Value / 1000f) * 100) + "%";
                icecreamBar.fillAmount = (float)currentSecond.Value / 1000;
                if (!gracePeriod && !lookingForConeToBuild)
                {
                    gameObjectiveText.text = "Wave " + waveNumber.Value + "/10 Enemies Remaining: " + enemiesRemaining.Value;
                    if (IsServer && enemiesRemaining.Value <= 0)
                    {
                        LevelUpPigeonAlliesPVE();
                        if (waveNumber.Value + 1 >= 11)
                        {
                            ActivateSuddenDeathUIClientRpc();
                            isSuddenDeath.Value = true;
                        }
                        else
                        {
                            StartGracePeriodClientRpc(20);

                        }
                    }
                }

            }

        }




        if (!isSwapingTracks && !audioSource.isPlaying)
        {
            audioSource.Stop();
            audioSource.clip = musicTracks[currentTrack];
            if (currentTrack >= musicTracks.Length - 1) currentTrack = 0;
            else
            {
                currentTrack++;

            }
            audioSource.Play();
        }
        if (!player) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (pauseMenuOpen == true)
            {
                pauseMenuOpen = false;
                pauseMenu.SetActive(false);
                if (GameDataHolder.isSinglePlayer) Time.timeScale = 1;
            }
            else
            {
                pauseMenuOpen = true;
                pauseMenu.SetActive(true);
                if (GameDataHolder.isSinglePlayer)
                {
                    pauseMenuText.text = "Chadgeon realizes he is in a singleplayer game and cannot move";
                    Time.timeScale = 0;
                }
            }
        }
        if (Input.GetKeyDown(KeyCode.U))
        {
            if (uiOpen == true)
            {
                uiOpen = false;
                gameUI.SetActive(false);
            }
            else
            {
                uiOpen = true;
                gameUI.SetActive(true);

            }
        }

        if (Input.GetKey(KeyCode.Space))
        {
            minimapUI.SetActive(true);
        }
        else
        {
            minimapUI.SetActive(false);
        }

        if (selectingUpgrades)
        {
            if (Input.GetKey(KeyCode.Alpha1) || Input.GetKey(KeyCode.Keypad1))
            {
                SelectUpgrade(0);
            }
            if (Input.GetKey(KeyCode.Alpha2) || Input.GetKey(KeyCode.Keypad2))
            {
                SelectUpgrade(1);
            }
            if (Input.GetKey(KeyCode.Alpha3) || Input.GetKey(KeyCode.Keypad3))
            {
                SelectUpgrade(2);
            }
            if (player.pigeonUpgrades.ContainsKey(Upgrades.psionic) && (Input.GetKey(KeyCode.Alpha4) || Input.GetKey(KeyCode.Keypad4)))
            {
                SelectUpgrade(3);
            }
        }

        if (player.isSprinting == true)
        {
            sprintUI.SetActive(true);
            sprintBar.fillAmount = player.stamina / player.maxStamina;
        }
        else
        {
            staminaCooldownBar.fillAmount = player.stamina / player.maxStamina;
            sprintUI.SetActive(false);
        }

        featherChargeCounter.text = player.chargedFeathers.ToString();

        healthBar.fillAmount = player.currentHP.Value / (float)player.maxHp.Value;
        hpText.text = (float)player.currentHP.Value + "/" + player.maxHp.Value;
        xpBar.fillAmount = player.xp / (float)player.xpTillLevelUp;
        levelText.text = player.level.Value.ToString();

        if (IsServer && canSpawnFood && !isSuddenDeath.Value && gracePeriod)
        {
            SpawnFood();
            if (!hasOpenedChurch && currentSecond.Value < 120)
            {
                hasOpenedChurch = true;
                OpenChurchDoorClientRpc();
            }
        }
    }


    private void SceneManager_OnLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (GameDataHolder.map == 0)
        {
            StartCoroutine(KtownManager.instance.RespawnDigSites());
        }
        StartCoroutine(WaitForClients());
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
    [ServerRpc(RequireOwnership = false)]
    public void ConstructIceCreamConeServerRpc(Vector3 pos)
    {
        GameObject cone = Instantiate(builtConePrefab, pos, transform.rotation);
        cone.GetComponent<NetworkObject>().Spawn();
        coneToDefend = cone;
        foreach (GameObject unbuiltCones in unbuiltConeGameobjects)
        {
            Destroy(unbuiltCones);
        }
        ShowIceCreamUIClientRpc();
        StartGracePeriodClientRpc(40);
    }
    [ClientRpc]
    private void ShowIceCreamUIClientRpc()
    {
        iceCreamUI.SetActive(true);
        gracePeriod = true;
        lookingForConeToBuild = false;
    }
    [ClientRpc]
    private void StartGracePeriodClientRpc(int seconds)
    {
        StartCoroutine(StartGracePeriod(seconds));
    }
    [ClientRpc]
    private void UpdatePigeonsForClientsClientRpc(PigeonInitializeProperties[] data, int botDiff, int map, int neutralBots, int t1Bots, int t2Bots, int t3Bots, int t4Bots)
    {
        GameDataHolder.map = map;
        GameDataHolder.botsToSpawn = neutralBots;
        GameDataHolder.botsFlock1 = t1Bots;
        GameDataHolder.botsFlock2 = t2Bots;
        GameDataHolder.botsFlock3 = t3Bots;
        GameDataHolder.botsFlock4 = t4Bots;
        GameDataHolder.botDifficulty = botDiff;
        foreach (PigeonInitializeProperties p in data)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects[p.pigeonID])
            {
                NetworkObject ob = NetworkManager.Singleton.SpawnManager.SpawnedObjects[p.pigeonID];
                if (!ob) return;
                ob.GetComponent<Pigeon>().UpatePigeonInitialValues(p);
            }
        }
    }
    [ClientRpc]
    public void UpdatePigeonClientRpc(PigeonInitializeProperties data)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects[data.pigeonID])
        {
            NetworkObject ob = NetworkManager.Singleton.SpawnManager.SpawnedObjects[data.pigeonID];
            if (!ob) return;
            ob.GetComponent<Pigeon>().UpatePigeonInitialValues(data);
        }
    }
    [ClientRpc]
    public void ActiveateIcecreamUIClientRpc()
    {
        iceCreamUI.SetActive(true);

    }
    [ClientRpc]
    private void HideLoadingPigeonsTextClientRpc()
    {
        loadingPigeonsText.SetActive(false);
        playerUI.SetActive(true);
    }
    [ClientRpc]
    private void ActivateSuddenDeathUIClientRpc()
    {
        suddenDeathText.SetActive(true);
        iceCreamUI.SetActive(false);
        if (GameDataHolder.gameMode == 1)
        {
            evacZone.SetActive(true);
            if (IsServer)
            {
                StartCoroutine(StartEvacCountDown(40));
                for (int i = 0; i < 5; i++)
                {
                    Vector3 pos = new Vector2(Random.Range(-2f, 2f), Random.Range(-2f, 2f));
                    pos += coneToDefend.transform.position;
                    GameObject food = Instantiate(FoodPrefab, pos, transform.rotation);
                    food.GetComponent<NetworkObject>().Spawn();
                }

                Destroy(coneToDefend);
                waveNumber.Value = 6;
                StartCoroutine(SpawnWave());
            }
        }

    }
    [ClientRpc]
    private void OpenChurchDoorClientRpc()
    {
        if (churchDoor) churchDoor.SetActive(false);
    }
    [ClientRpc]
    public void ShowWinScreenClientRpc(string victorytext, int teamThatWon, ulong pigeonThatWon)
    {
        playerUI.SetActive(false);
        endGameDescriptionText.text = victorytext;
        endScreen.SetActive(true);



        if (teamThatWon == player.flock || pigeonThatWon == player.NetworkObjectId)
        {
            endScreen.GetComponent<EndScreenScript>().UpdateChadCoinWinStuff(true);
        }
        else
        {
            endScreen.GetComponent<EndScreenScript>().UpdateChadCoinWinStuff(false);
        }
    }


    public void CheckWinGame()
    {
        if (gameover) return;

        int survivors = 0;
        List<int> survivingFactions = new();
        Pigeon survivingPigeon = null;
        foreach (Pigeon pigeon in allpigeons)
        {
            if (pigeon == null) continue;
            if (!pigeon.isKnockedOut.Value)
            {
                survivors++;
                survivingPigeon = pigeon;
                if (!survivingFactions.Contains(pigeon.flock))
                {
                    survivingFactions.Add(pigeon.flock);
                }

            }
        }

        string victoryText = "";
        if (survivingFactions.Count == 1 && survivingFactions[0] != 0)
        {
            switch (survivingPigeon.flock)
            {
                case 1:
                    victoryText = "The Enjoyers have defeated all of thier rivals and have won the game";
                    break;
                case 2:
                    victoryText = "The Psychos have defeated all of thier rivals and have won the game";
                    break;
                case 3:
                    victoryText = "The Minions have defeated all of thier rivals and have won the game";
                    break;
                case 4:
                    victoryText = "The Looksmaxers have defeated all of thier rivals and have won the game";
                    break;
            }
            gameover = true;
            ShowWinScreenClientRpc(victoryText, survivingPigeon.flock, survivingPigeon.NetworkObjectId);
        }
        if (survivors <= 1)
        {
            gameover = true;
            //Someone Won the Game display credits
            victoryText = survivingPigeon.pigeonName + " has defeated all of his rivals and ascended to gigachad pigeon status";
            ShowWinScreenClientRpc(victoryText, -1, survivingPigeon.NetworkObjectId);
        }


    }
    public void CheckWinGamePVE()
    {
        if (gameover) return;


        gameover = true;
        //Someone Won the Game display credits
        string victoryText = "The Enjoyers have defeated all of thier rivals and won the game";
        ShowWinScreenClientRpc(victoryText, 1, 0);


    }
    public void OpenDialouge(Sprite talkingImage, string textToSay, string nameRee, int specialDialouge)
    {
        foreach (GameObject ree in specialDialougeObjects)
        {
            ree.SetActive(false);
        }
        specialDialougeObjects[specialDialouge].SetActive(true);

        upgradeUi.SetActive(false);
        dialougeUI.SetActive(true);
        dialougeText.text = textToSay;
        dialougeNameText.text = nameRee;
        dialougeImage.sprite = talkingImage;
    }
    public void CloseDialouge()
    {
        upgradeUi.SetActive(true);
        dialougeUI.SetActive(false);
    }
    public Vector3 GetSpawnPos()
    {
        return GetSpawnPos(Random.Range(0, spawnLocations.Count));
    }
    public Vector3 GetSpawnPos(int location)
    {
        Transform pos = spawnLocations[location];
        float spawnX = pos.position.x;
        float spawnY = pos.position.y;


        float spawnRange = 2f;

        if (Random.Range(0, 100) <= 50)
        {
            if (Random.Range(0, 100) <= 50)
            {
                spawnX += Random.Range(0, spawnRange);
                spawnY += Random.Range(-spawnRange, spawnRange);
            }
            else
            {
                spawnX += Random.Range(-0, -spawnRange);
                spawnY += Random.Range(-spawnRange, spawnRange);
            }
        }
        else
        {
            if (Random.Range(0, 100) <= 50)
            {
                spawnX += Random.Range(-spawnRange, spawnRange);
                spawnY += Random.Range(0, spawnRange);
            }
            else
            {
                spawnX += Random.Range(-spawnRange, spawnRange);
                spawnY += Random.Range(-0, -spawnRange);
            }
        }
        return new Vector3(spawnX, spawnY);
    }
    public void SetFullScreen()
    {
        Screen.fullScreen = !Screen.fullScreen;
    }
    public void ClosePauseMenu()
    {
        pauseMenuOpen = false;
        pauseMenu.SetActive(false);
        if (GameDataHolder.isSinglePlayer) Time.timeScale = 1;

    }
    public void AddUpgradeToDisply(int upgrade)
    {
        upgradeHolder.SetActive(true);
        if (upgrade >= 0) upgradeDisplays[upgrade].SetActive(true);
        else
        {
            Pigeon.Upgrades ree = (Pigeon.Upgrades)upgrade;
            if (ree == Upgrades.minionHelmet || ree == Upgrades.minionScript || ree == Upgrades.minionGoggles || ree == Upgrades.theChosen)
            {
                KtownManager.instance.AddUpgradeToDisply(ree);
            }
        }


    }
    public void ShowUpgradeDes(int desc)
    {
        upgradeDescUI.SetActive(true);


        if (desc >= 0)
        {

            upgradeDescText.text = upgradeDesc[desc];
            upgradeNameText.text = upgradeNames[desc];
        }
        else
        {
            switch ((Upgrades)desc)
            {
                case Upgrades.pigeonOfGrowth:
                    upgradeDescText.text = repeatableUpgradeDesc[0];
                    upgradeNameText.text = repeatableUpgradeNames[0];

                    break;
                case Upgrades.pigeonOfMomentum:
                    upgradeDescText.text = repeatableUpgradeDesc[1];
                    upgradeNameText.text = repeatableUpgradeNames[1];

                    break;
                case Upgrades.pigeonOfViolence:
                    upgradeDescText.text = repeatableUpgradeDesc[2];
                    upgradeNameText.text = repeatableUpgradeNames[2];

                    break;
            }
        }



    }
    public void CloseUpgradeDes()
    {
        upgradeDescText.text = "";
        upgradeDescUI.SetActive(false);
    }
    public void StartSpectating()
    {
        isSpectating = true;
        respawnTimer.gameObject.SetActive(false);
        for (int i = 0; i < allpigeons.Count; i++)
        {
            Pigeon pigeon = allpigeons[i];

            if (!pigeon.isKnockedOut.Value)
            {
                playerUI.SetActive(false);
                spectateScreen.SetActive(true);
                spectatingText.text = "Spectating " + pigeon.pigeonName;
                mainCamera.Follow = pigeon.transform;
                currentSpectate = i;
                break;
            }
        }

    }
    public void StartSpectating(int second)
    {
        if (GameDataHolder.gameMode == 0) playerUI.SetActive(false);
        isSpectating = true;
        spectateScreen.SetActive(true);
        for (int i = 0; i < allpigeons.Count; i++)
        {
            Pigeon pigeon = allpigeons[i];

            spectatingText.text = "Spectating " + pigeon.pigeonName;
            mainCamera.Follow = pigeon.transform;
            if (!pigeon.isKnockedOut.Value)
            {
                currentSpectate = i;
                break;
            }
        }
        StartCoroutine(RespawnTimer(second));

    }
    public void StopSpectating()
    {
        isSpectating = false;
        playerUI.SetActive(true);
        spectateScreen.SetActive(false);
        mainCamera.Follow = player.transform;
    }
    public void ReturnToMainMenu()
    {
        Time.timeScale = 1;
        NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene("MainMenu");
    }
    public void ShowUpgrades()
    {
        //when the player achives lvl 5 
        upgradeScreen.SetActive(true);
        selectingUpgrades = true;
        Dictionary<Pigeon.Upgrades, int> upgradesUsed = new Dictionary<Pigeon.Upgrades, int>();

        for (int i = 0; i < upgradesThatCanBeSelected.Length; i++)
        {
            Pigeon.Upgrades upgrade = Pigeon.Upgrades.regen;
            bool hasAbilitySlotUnlocked = false;


            for (int x = 0; x < 1000; x++)
            {
                if (player.level.Value >= 25 && Random.Range(0, 100) <= 20)
                {
                    upgrade = ((Upgrades)Random.Range(-1, -4));
                }
                else
                {
                    upgrade = allPigeonUpgrades[Random.Range(0, allPigeonUpgrades.Count)];

                }
                hasAbilitySlotUnlocked = false;

                switch (upgrade)
                {
                    case Upgrades.slam:
                        if (player.hasAbilityM2) hasAbilitySlotUnlocked = true; break;
                    case Upgrades.hiddinTalon:
                        if (player.hasAbilityQ) hasAbilitySlotUnlocked = true; break;
                    case Upgrades.mewing:
                        if (player.hasAbilityQ) hasAbilitySlotUnlocked = true; break;
                    case Upgrades.wholeGains:
                        if (player.hasAbilityE) hasAbilitySlotUnlocked = true; break;
                    case Upgrades.pigeonPoo:
                        if (player.hasAbilityE) hasAbilitySlotUnlocked = true; break;
                    case Upgrades.razorFeathers:
                        if (player.hasAbilityM2) hasAbilitySlotUnlocked = true; break;
                }

                if (upgrade == Upgrades.hiddinTalon && !player.pigeonUpgrades.ContainsKey(Upgrades.assassin)) continue;
                if (!upgradesUsed.ContainsKey(upgrade) && !player.pigeonUpgrades.ContainsKey(upgrade) && !hasAbilitySlotUnlocked) break;
            }

            if (player.pigeonUpgrades.ContainsKey(upgrade) || hasAbilitySlotUnlocked)
            {
                upgrade = ((Upgrades)Random.Range(-1, -4));
            }


            upgradesUsed.Add(upgrade, 1);
            upgradesThatCanBeSelected[i] = upgrade;
            if ((int)upgrade >= 0)
            {
                upgradeDescibers[i].desc = (int)upgrade;
                upgradeButtonImages[i].sprite = upgradeButtonSprites[(int)upgrade];
            }
            else
            {
                switch (upgrade)
                {
                    case Upgrades.pigeonOfGrowth:
                        upgradeDescibers[i].desc = -3;
                        upgradeButtonImages[i].sprite = repeatableUpgradeButtonSprites[0];
                        break;
                    case Upgrades.pigeonOfMomentum:
                        upgradeDescibers[i].desc = -2;
                        upgradeButtonImages[i].sprite = repeatableUpgradeButtonSprites[1];
                        break;
                    case Upgrades.pigeonOfViolence:
                        upgradeDescibers[i].desc = -1;
                        upgradeButtonImages[i].sprite = repeatableUpgradeButtonSprites[2];
                        break;
                }
            }

        }
    }
    public void AddExtraUpgradePick()
    {
        extraUpgradeSelectionGameobject.SetActive(true);
        mainCamera.Lens.OrthographicSize += 1;
    }
    public void IncreaseVeiwRange()
    {
        mainCamera.Lens.OrthographicSize += 1;

    }
    public void SelectUpgrade(int selected)
    {
        upgradeScreen.SetActive(false);
        upgradeDescUI.SetActive(false);
        selectingUpgrades = false;
        player.AddUpgrade(upgradesThatCanBeSelected[selected]);
    }
    public void StartCooldown(Pigeon.Upgrades ability, int seconds)
    {
        switch (ability)
        {
            case Upgrades.mewing:
                cooldownIcons[0].StartCooldown(seconds);
                break;
            case Upgrades.hiddinTalon:
                cooldownIcons[1].StartCooldown(seconds);
                break;
            case Upgrades.pigeonPoo:
                cooldownIcons[2].StartCooldown(seconds);
                break;
            case Upgrades.wholeGains:
                cooldownIcons[3].StartCooldown(seconds);
                break;
            case Upgrades.razorFeathers:
                cooldownIcons[4].StartCooldown(seconds);
                break;
            case Upgrades.slam:
                cooldownIcons[5].StartCooldown(seconds);
                break;
        }
    }
    public void ActivateAbility(Pigeon.Upgrades ability)
    {
        if (!player.pigeonUpgrades.ContainsKey(ability)) return;
        switch (ability)
        {
            case Upgrades.mewing:
                cooldownIcons[0].Show();
                break;
            case Upgrades.hiddinTalon:
                cooldownIcons[1].Show();
                break;
            case Upgrades.pigeonPoo:
                cooldownIcons[2].Show();
                break;
            case Upgrades.wholeGains:
                cooldownIcons[3].Show();
                break;
            case Upgrades.razorFeathers:
                cooldownIcons[4].Show();
                break;
            case Upgrades.slam:
                cooldownIcons[5].Show();
                break;
        }
    }
    public void SpectateNext()
    {
        for (int i = 0; i < 10; i++)
        {
            currentSpectate++;
            if (currentSpectate > allpigeons.Count - 1)
            {
                currentSpectate = 0;
            }
            if (allpigeons[currentSpectate].isKnockedOut.Value == false)
            {
                break;
            }
        }
        Pigeon pigeon = allpigeons[currentSpectate];
        if (pigeon)
        {
            mainCamera.Follow = pigeon.transform;
            spectatingText.text = "Spectating " + pigeon.pigeonName;
        }

    }
    public void SpectatePreviouse()
    {

        for (int i = 0; i < 10; i++)
        {
            currentSpectate--;
            if (currentSpectate < 0)
            {
                currentSpectate = allpigeons.Count - 1;
            }
            if (allpigeons[currentSpectate].isKnockedOut.Value == false)
            {
                break;
            }
        }

        Pigeon pigeon = allpigeons[currentSpectate];
        mainCamera.Follow = pigeon.transform;
        spectatingText.text = "Spectating " + pigeon.pigeonName;
    }
    public void DestroyFoodObject(food foodie)
    {
        DestroyFoodObjectServerRpc(foodie.NetworkObject);
    }
    public void SwapToMewingSong()
    {
        audioSource.volume = 0;
        mewingSorce.Play();
    }
    public void StopMewingSong()
    {
        mewingSorce.Stop();
        audioSource.volume = 1;
    }

    private void NetworkManager_OnClientDisconnectCallback(ulong clientId)
    {
        StartCoroutine(GameManager.instance.CheckMissingHost());
    }
    private void LevelUpPigeonAlliesPVE()
    {
        List<int> missingpigoens = new List<int>();
        for (int i = 0; i < allpigeons.Count; i++)
        {
            if (allpigeons[i] == null) missingpigoens.Add(i);
        }
        foreach (int i in missingpigoens)
        {
            allpigeons.RemoveAt(i);
        }


        RespawnDeadAlliesClientRpc();
        allpigeons = allpigeons.OrderByDescending(p => p.level.Value).ToList();
        for (int i = 0; i < allpigeons.Count; i++)
        {
            switch (i)
            {
                case 1:
                    allpigeons[i].ReciveCatchupXPClientRpc(0.3f);
                    break;
                case 2:
                    allpigeons[i].ReciveCatchupXPClientRpc(0.6f);
                    break;
                case 3:
                    allpigeons[i].ReciveCatchupXPClientRpc(0.9f);
                    break;
            }
        }
    }
    [ClientRpc]
    private void RespawnDeadAlliesClientRpc()
    {
        player.PVERespawn();
    }
    private void SpawnFood()
    {
        StartCoroutine(SpawnFoodDelay());
    }
    private void StartNextWave()
    {
        StartCoroutine(SpawnWave());
    }


    IEnumerator StartGracePeriod(int seconds)
    {
        gracePeriod = true;

        while (true)
        {
            for (int i = 0; i < seconds; i++)
            {
                gameObjectiveText.text = "Collect cones! " + (seconds - i).ToString() + " Seconds left";
                yield return new WaitForSeconds(1);

            }
            gameObjectiveText.text = "";
            gracePeriod = false;
            if (IsServer) StartNextWave();
            yield break;
        }
    }
    IEnumerator StartEvacCountDown(int seconds)
    {
        currentSecond.Value = seconds;
        SpawnWave();
        while (true)
        {
            yield return new WaitForSeconds(1);
            currentSecond.Value--;

            if (currentSecond.Value <= 0)
            {

                CheckWinGamePVE();
                yield break;

            }
            else
            {
                yield return null;

            }
        }
    }
    public IEnumerator CheckMissingHost()
    {
        yield return new WaitForSeconds(1);
        if (player == null && !gameover)
        {

            //server is sutting down
            hostDCUI.SetActive(true);
        }
    }
    IEnumerator RespawnTimer(int seconds)
    {
        respawnTimer.gameObject.SetActive(true);
        for (int i = 0; i < seconds; i++)
        {
            respawnTimer.text = "respawning in " + (seconds - i) + "...";
            yield return new WaitForSeconds(1);
        }

    }
    IEnumerator WaitForClients()
    {
        //Get total pigeon count
        int totalPigeons = GameDataHolder.playerCount + GameDataHolder.botsToSpawn + GameDataHolder.botsFlock1 + GameDataHolder.botsFlock2 + GameDataHolder.botsFlock3 + GameDataHolder.botsFlock4;

        while (true)
        {
            if (GameDataHolder.isSinglePlayer || GameDataHolder.playerCount == NetworkManager.Singleton.ConnectedClients.Count)
            {
                yield return new WaitForSeconds(1);
                if (GameDataHolder.gameMode == 0)
                {
                    foreach (ulong client in NetworkManager.Singleton.ConnectedClientsIds)
                    {
                        Vector3 spawnPos = GetSpawnPos();
                        GameObject player = Instantiate(playerPrefab, spawnPos, transform.rotation);
                        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(client, true);
                    }

                    for (int i = 0; i < GameDataHolder.botsToSpawn; i++)
                    {
                        Vector3 spawnPos = GetSpawnPos();
                        GameObject pigeon = Instantiate(pigeonPrefab, spawnPos, transform.rotation);
                        PigeonAI ai = pigeon.GetComponent<PigeonAI>();

                        if (GameDataHolder.botDifficulty == -1)
                        {
                            ai.SetAI(Random.Range(0, 3));

                        }
                        else
                        {
                            ai.SetAI(GameDataHolder.botDifficulty);

                        }
                        if (GameDataHolder.map == 6)
                        {
                            ai.skinHead = 12;
                        }
                        pigeon.GetComponent<NetworkObject>().Spawn();
                    }
                    for (int i = 0; i < GameDataHolder.botsFlock1; i++)
                    {
                        Vector3 spawnPos = GetSpawnPos();
                        GameObject pigeon = Instantiate(pigeonPrefab, spawnPos, transform.rotation);
                        PigeonAI ai = pigeon.GetComponent<PigeonAI>();
                        ai.flock = 1;

                        if (GameDataHolder.botDifficulty == -1)
                        {
                            ai.SetAI(Random.Range(0, 3));

                        }
                        else
                        {
                            ai.SetAI(GameDataHolder.botDifficulty);

                        }
                        if (GameDataHolder.map == 6)
                        {
                            ai.skinHead = 12;
                        }
                        pigeon.GetComponent<NetworkObject>().Spawn();
                    }
                    for (int i = 0; i < GameDataHolder.botsFlock2; i++)
                    {
                        Vector3 spawnPos = GetSpawnPos();
                        GameObject pigeon = Instantiate(pigeonPrefab, spawnPos, transform.rotation);
                        PigeonAI ai = pigeon.GetComponent<PigeonAI>();
                        ai.flock = 2;

                        if (GameDataHolder.botDifficulty == -1)
                        {
                            ai.SetAI(Random.Range(0, 3));

                        }
                        else
                        {
                            ai.SetAI(GameDataHolder.botDifficulty);

                        }


                        if (GameDataHolder.map == 6)
                        {
                            ai.skinHead = 12;
                        }
                        pigeon.GetComponent<NetworkObject>().Spawn();
                    }
                    for (int i = 0; i < GameDataHolder.botsFlock3; i++)
                    {
                        Vector3 spawnPos = GetSpawnPos();
                        GameObject pigeon = Instantiate(pigeonPrefab, spawnPos, transform.rotation);
                        PigeonAI ai = pigeon.GetComponent<PigeonAI>();
                        ai.flock = 3;

                        if (GameDataHolder.botDifficulty == -1)
                        {
                            ai.SetAI(Random.Range(0, 3));

                        }
                        else
                        {
                            ai.SetAI(GameDataHolder.botDifficulty);

                        }
                        ai.skinBase = 5;
                        if (GameDataHolder.map == 6)
                        {
                            ai.skinHead = 12;
                        }
                        pigeon.GetComponent<NetworkObject>().Spawn();
                    }
                    for (int i = 0; i < GameDataHolder.botsFlock4; i++)
                    {
                        Vector3 spawnPos = GetSpawnPos();
                        GameObject pigeon = Instantiate(pigeonPrefab, spawnPos, transform.rotation);
                        PigeonAI ai = pigeon.GetComponent<PigeonAI>();
                        ai.flock = 4;

                        if (GameDataHolder.botDifficulty == -1)
                        {
                            ai.SetAI(Random.Range(0, 3));

                        }
                        else
                        {
                            ai.SetAI(GameDataHolder.botDifficulty);

                        }
                        if (GameDataHolder.map == 6)
                        {
                            ai.skinHead = 12;
                        }
                        pigeon.GetComponent<NetworkObject>().Spawn();
                    }
                }
                else
                {
                    currentSecond.Value = 1000;
                    int spawnArea = Random.Range(0, spawnLocations.Count - 1);
                    foreach (ulong client in NetworkManager.Singleton.ConnectedClientsIds)
                    {
                        Vector3 spawnPos = GetSpawnPos(spawnArea);
                        GameObject player = Instantiate(playerPrefab, spawnPos, transform.rotation);
                        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(client, true);
                    }

                    for (int i = 0; i < GameDataHolder.botsToSpawn; i++)
                    {
                        Vector3 spawnPos = GetSpawnPos(spawnArea);
                        GameObject pigeon = Instantiate(pigeonPrefab, spawnPos, transform.rotation);
                        PigeonAI ai = pigeon.GetComponent<PigeonAI>();
                        ai.SetAI(2);
                        ai.flock = 1;
                        if (GameDataHolder.map == 6)
                        {
                            ai.skinHead = 12;
                        }
                        pigeon.GetComponent<NetworkObject>().Spawn();
                    }


                    spawnArea = Random.Range(0, spawnLocations.Count - 1);
                    for (int i = 0; i < 3; i++)
                    {
                        GameObject cone = Instantiate(unbuiltConePrefab, spawnLocations[spawnArea].transform.position, transform.rotation);
                        unbuiltConeGameobjects.Add(cone);
                        cone.GetComponent<NetworkObject>().Spawn();
                        spawnArea += Random.Range(5, 8);
                        if (spawnArea > spawnLocations.Count - 1)
                        {
                            spawnArea -= spawnLocations.Count;
                        }
                    }
                }


                /*
                //TestBots
                for (int i = 0; i < 10; i++)
                {
                    Vector3 spawnPos = GetSpawnPos();
                    GameObject pigeon = Instantiate(pigeonPrefab, spawnPos, transform.rotation);
                    PigeonAI ai = pigeon.GetComponent<PigeonAI>();
                    totalPigeons += 1;
                    ai.SetAI(2);
                    pigeon.GetComponent<NetworkObject>().Spawn();
                }
                for (int i = 0; i < 10; i++)
                {
                    Vector3 spawnPos = GetSpawnPos();
                    GameObject pigeon = Instantiate(pigeonPrefab, spawnPos, transform.rotation);
                    PigeonAI ai = pigeon.GetComponent<PigeonAI>();
                    totalPigeons += 1;
                    ai.SetAI(1);
                    pigeon.GetComponent<NetworkObject>().Spawn();
                }
                for (int i = 0; i < 10; i++)
                {
                    Vector3 spawnPos = GetSpawnPos();
                    GameObject pigeon = Instantiate(pigeonPrefab, spawnPos, transform.rotation);
                    PigeonAI ai = pigeon.GetComponent<PigeonAI>();
                    ai.SetAI(0);
                    totalPigeons += 1;
                    pigeon.GetComponent<NetworkObject>().Spawn();
                }
                */




                while (true)
                {
                    if (totalPigeons == pigeonStartData.Count)
                    {
                        currentSecond.Value = secondsTillSuddenDeath;

                        UpdatePigeonsForClientsClientRpc(pigeonStartData.ToArray(), GameDataHolder.botDifficulty, GameDataHolder.map, GameDataHolder.botsToSpawn, GameDataHolder.botsFlock1, GameDataHolder.botsFlock2, GameDataHolder.botsFlock3, GameDataHolder.botsFlock4);

                        if (GameDataHolder.gameMode == 0)
                        {
                            StartCoroutine(DepreciateIceCream());

                        }
                        else
                        {
                            StartCoroutine(SetupicecreamHealth());
                        }


                        yield break;
                    }
                    yield return null;
                }
            }


            yield return null;

        }
    }
    IEnumerator SpawnFoodDelay()
    {

        canSpawnFood = false;

        yield return new WaitForSeconds(2f);
        Vector3 pos = GetSpawnPos();

        if (GameDataHolder.gameMode != 1)
        {
            for (int i = 0; i < 1 + Mathf.RoundToInt(allpigeons.Count / 4); i++)
            {
                pos = GetSpawnPos();
                GameObject food = Instantiate(FoodPrefab, pos, transform.rotation);
                food.GetComponent<NetworkObject>().Spawn();
            }
        }
        else
        {
            for (int i = 0; i < 4; i++)
            {
                pos = GetSpawnPos();
                GameObject food = Instantiate(FoodPrefab, pos, transform.rotation);
                food.GetComponent<NetworkObject>().Spawn();
            }
        }

        canSpawnFood = true;
    }
    IEnumerator DepreciateIceCream()
    {
        yield return new WaitForSeconds(0.1f);
        gracePeriod = true;
        loadingPigeonsText.SetActive(false);
        playerUI.SetActive(true);
        HideLoadingPigeonsTextClientRpc();

        while (true)
        {
            yield return new WaitForSeconds(1);
            currentSecond.Value--;
            if (currentSecond.Value <= 0)
            {
                suddenDeathText.SetActive(true);
                iceCreamUI.SetActive(false);
                ActivateSuddenDeathUIClientRpc();
                isSuddenDeath.Value = true;
                yield break;
            }
            yield return null;
        }
    }

    IEnumerator SetupicecreamHealth()
    {
        yield return new WaitForSeconds(0.1f);
        loadingPigeonsText.SetActive(false);
        playerUI.SetActive(true);
        HideLoadingPigeonsTextClientRpc();
        currentSecond.Value = 1000;
    }
    IEnumerator SpawnWave()
    {
        waveNumber.Value++;
        enemiesRemaining.Value = Mathf.RoundToInt(waveNumber.Value * allpigeons.Count);
        int specialWaveType = Random.Range(0, 2);
        int amtofEnemies = enemiesRemaining.Value;

        bool minionWave = false;
        if (Random.Range(0, 100) <= 5)
        {
            minionWave = true;
            amtofEnemies += 3;
            enemiesRemaining.Value += 3;
        }

        int bestSpawnLocation = 0;
        float overallBestDistance = 0;
        for (int y = 0; y < 10; y++)
        {
            int currentSpawnLocation = Random.Range(0, spawnLocations.Count);
            float bestDis = (coneToDefend.transform.position - spawnLocations[currentSpawnLocation].transform.position).sqrMagnitude;
            foreach (Pigeon pigeon in allpigeons)
            {
                //calculateDist
                float distance = (pigeon.transform.position - spawnLocations[currentSpawnLocation].transform.position).sqrMagnitude;
                if (distance < bestDis)
                {
                    bestDis = distance;
                }
            }
            if (bestDis > overallBestDistance)
            {
                overallBestDistance = bestDis;
                bestSpawnLocation = currentSpawnLocation;
            }
        }


        for (int i = 0; i < amtofEnemies; i++)
        {
            yield return new WaitForSeconds(0.5f);

            Vector3 spawnPos = GetSpawnPos(bestSpawnLocation);


            GameObject pigeon = Instantiate(goonPrefab, spawnPos, transform.rotation);
            PigeonAI ai = pigeon.GetComponent<PigeonAI>();
            ai.SetAI(3);
            ai.flock = 2;

            if (minionWave)
            {
                ai.gameObject.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
                ai.damageTakenModifier = 1.5f;
                ai.skinBase = 5;
                ai.speedMod += 1;
                ai.flock = 3;
                ai.pigeonName = "Minion";
                if (Random.Range(0, 100) <= 20)
                {
                    ai.skinHead = 0;
                    ai.skinBody = 0;
                }
            }
            else
            {
                if (Random.Range(0, 1000) <= 3)
                {
                    ai.SetAI(2);
                    ai.pigeonName = "Ryan Gosling";
                    ai.skinBase = 8;
                    ai.skinHead = 2;
                    ai.skinBody = 2;
                    ai.damageTakenModifier = 0.5f;
                    ai.flock = 1;
                    enemiesRemaining.Value--;
                }
                else if (Random.Range(0, 1000) <= 5)
                {
                    ai.pigeonName = "Patrick Bateman";
                    ai.goonPriority = PigeonAI.GoonPriority.player;
                    ai.skinBase = 7;
                    ai.skinHead = 1;
                    ai.skinBody = 1;
                    ai.speedMod += 0.5f;
                    ai.damageTakenModifier = 0.5f;

                }
                else if (Random.Range(0, 100) <= 20)
                {
                    ai.skinBase = 12;
                }
            }

            ai.diesAfterDeath = true;

            if (waveNumber.Value == 5 || waveNumber.Value == 8 || waveNumber.Value == 3 || waveNumber.Value == 10)
            {
                switch (specialWaveType)
                {
                    case 0:
                        ai.AddUpgrade(Upgrades.slam);
                        ai.AddUpgrade(Upgrades.overclock);
                        break;
                    case 1:
                        ai.AddUpgrade(Upgrades.razorFeathers);
                        ai.AddUpgrade(Upgrades.overclock);
                        break;
                    case 2:
                        ai.AddUpgrade(Upgrades.mewing);
                        ai.AddUpgrade(Upgrades.overclock);

                        break;

                }
            }
            if (GameDataHolder.map == 6)
            {
                ai.skinHead = 12;
            }
            pigeon.GetComponent<NetworkObject>().Spawn();




            switch (GameDataHolder.botDifficulty)
            {
                case 0:
                    for (int x = 0; x < Mathf.RoundToInt(waveNumber.Value * 0.5f); x++)
                    {
                        ai.LevelUP();
                    }
                    break;
                case 1:
                    for (int x = 0; x < Mathf.RoundToInt(waveNumber.Value * 1f); x++)
                    {
                        ai.LevelUP();
                    }
                    break;
                case 2:
                    for (int x = 0; x < Mathf.RoundToInt(waveNumber.Value * 1.5f); x++)
                    {
                        ai.LevelUP();
                    }
                    break;
                case 3:
                    for (int x = 0; x < Mathf.RoundToInt(waveNumber.Value * 2f); x++)
                    {
                        ai.LevelUP();
                    }
                    break;
            }
        }
    }


    public void SpawnPigeonDuringGameplay(Vector3 pos, int diff, int skinBase, string pigeonName)
    {
        GameObject pigeon = Instantiate(pigeonPrefab, pos, transform.rotation);
        PigeonAI ai = pigeon.GetComponent<PigeonAI>();
        ai.speedMod -= 0.5f;
        ai.SetAI(diff);
        ai.flock = 0;
        ai.pigeonName = pigeonName;
        ai.skinBase = skinBase;
        ai.diesAfterDeath = true;
        pigeon.GetComponent<NetworkObject>().Spawn();


    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnMinionDuringGameplayServerRpc(Vector3 pos)
    {
        GameObject pigeon = Instantiate(pigeonPrefab, pos, transform.rotation);
        PigeonAI ai = pigeon.GetComponent<PigeonAI>();
        ai.SetAI(2);
        ai.flock = 3;
        ai.pigeonName = "Minion";
        ai.skinBase = 5;
        ai.skinBody = 0;
        ai.skinHead = 0;
        ai.diesAfterDeath = true;
        pigeon.GetComponent<NetworkObject>().Spawn();
        for (int x = 0; x < 30; x++)
        {
            ai.LevelUP();
        }
    }
}

