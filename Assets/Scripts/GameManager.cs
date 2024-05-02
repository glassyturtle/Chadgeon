using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Pigeon;

public class GameManager : NetworkBehaviour
{
    public static GameManager instance;
    public List<Pigeon.Upgrades> allPigeonUpgrades;

    public NetworkVariable<bool> isSuddenDeath = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> currentSecond = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<float> borderSize = new NetworkVariable<float>(0.40f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public Pigeon player;
    public List<Pigeon> allpigeons = new List<Pigeon>();
    public GameObject pigeonPrefab;
    public CinemachineCamera mainCamera;


    [SerializeField] AudioSource audioSource, clickSound;
    [SerializeField] AudioClip gigaChadSong;
    [SerializeField] int secondsTillSuddenDeath;
    [SerializeField] Image icecreamBar, healthBar, xpBar, sprintBar, staminaCooldownBar;
    [SerializeField] Button endScreenMainMenuButton;
    [SerializeField] Transform borderTransform;

    [SerializeField] GameObject endScreen, playerUI, gameUI, upgradeScreen, pauseMenu, spectateScreen, minimapUI, sprintUI, churchDoor;
    [SerializeField] TMP_Text endGameDescriptionText, spectatingText, upgradeDescText, upgradeNameText, featherChargeCounter, gameObjectiveText;
    [SerializeField] GameObject upgradeDescUI;
    [SerializeField] private GameObject[] upgradeDisplays;
    [SerializeField] private UpgradeDescriber[] upgradeDescibers;
    [SerializeField] private List<Transform> spawnLocations;
    [SerializeField] AbilityCooldownIconScript[] cooldownIcons;
    [SerializeField] GameObject FoodPrefab, upgradeHolder, loadingPigeonsText, suddenDeathText, iceCreamUI, hostDCUI, unbuiltConePrefab, builtConePrefab;
    [SerializeField] TextMeshProUGUI hpText, timeleftText;
    [SerializeField] TextMeshProUGUI levelText;
    [SerializeField] TextMeshProUGUI chageonName;
    [SerializeField] GameObject playerPrefab;
    [SerializeField] GameObject extraUpgradeSelectionGameobject;
    [SerializeField] Image[] upgradeButtonImages;
    [SerializeField] TextMeshProUGUI[] upgradeButtonText;
    [SerializeField] Sprite[] upgradeButtonSprites;
    [SerializeField] string[] upgradeNames;
    [SerializeField] string[] upgradeDesc;
    [SerializeField] Sprite[] repeatableUpgradeButtonSprites;
    [SerializeField] string[] repeatableUpgradeNames;
    [SerializeField] string[] repeatableUpgradeDesc;
    bool gameover = false;
    bool canSpawnFood = true;
    bool pauseMenuOpen = false;
    private Pigeon.Upgrades[] upgradesThatCanBeSelected = new Pigeon.Upgrades[4];
    private int currentSpectate = 0;
    bool uiOpen = true;
    bool gracePeriod = true;
    private bool hasOpenedChurch = false;
    private List<GameObject> unbuiltConeGameobjects = new();

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
        instance = this;
        endScreenMainMenuButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene("MainMenu");
        });

        if (GameDataHolder.multiplayerName != "") chageonName.text = GameDataHolder.multiplayerName;
        else chageonName.text = "Chadgeon ";

        if (GameDataHolder.gameMode == "Supremacy")
        {
            gameObjectiveText.gameObject.SetActive(false);
        }
        else
        {
            iceCreamUI.SetActive(false);
            gracePeriod = false;
            gameObjectiveText.gameObject.SetActive(true);
        }
    }
    public override void OnNetworkSpawn()
    {

        if (IsServer)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += SceneManager_OnLoadEventCompleted;
        }
    }


    private void SceneManager_OnLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        StartCoroutine(WaitForClients());
    }
    IEnumerator WaitForClients()
    {
        //Get total pigeon count
        int totalPigeons = GameDataHolder.playerCount + GameDataHolder.botsToSpawn + GameDataHolder.botsFlock1 + GameDataHolder.botsFlock2 + GameDataHolder.botsFlock3 + GameDataHolder.botsFlock4;

        while (true)
        {
            if (GameDataHolder.playerCount == NetworkManager.Singleton.ConnectedClients.Count)
            {
                yield return new WaitForSeconds(1);
                if (GameDataHolder.gameMode == "Supremacy")
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
                        ai.SetAI(GameDataHolder.botDifficulty);
                        pigeon.GetComponent<NetworkObject>().Spawn();
                    }
                    for (int i = 0; i < GameDataHolder.botsFlock1; i++)
                    {
                        Vector3 spawnPos = GetSpawnPos();
                        GameObject pigeon = Instantiate(pigeonPrefab, spawnPos, transform.rotation);
                        PigeonAI ai = pigeon.GetComponent<PigeonAI>();
                        ai.flock = 1;
                        ai.SetAI(GameDataHolder.botDifficulty);
                        pigeon.GetComponent<NetworkObject>().Spawn();
                    }
                    for (int i = 0; i < GameDataHolder.botsFlock2; i++)
                    {
                        Vector3 spawnPos = GetSpawnPos();
                        GameObject pigeon = Instantiate(pigeonPrefab, spawnPos, transform.rotation);
                        PigeonAI ai = pigeon.GetComponent<PigeonAI>();
                        ai.flock = 2;
                        ai.SetAI(GameDataHolder.botDifficulty);
                        pigeon.GetComponent<NetworkObject>().Spawn();
                    }
                    for (int i = 0; i < GameDataHolder.botsFlock3; i++)
                    {
                        Vector3 spawnPos = GetSpawnPos();
                        GameObject pigeon = Instantiate(pigeonPrefab, spawnPos, transform.rotation);
                        PigeonAI ai = pigeon.GetComponent<PigeonAI>();
                        ai.flock = 3;
                        ai.SetAI(GameDataHolder.botDifficulty);
                        pigeon.GetComponent<NetworkObject>().Spawn();
                    }
                    for (int i = 0; i < GameDataHolder.botsFlock4; i++)
                    {
                        Vector3 spawnPos = GetSpawnPos();
                        GameObject pigeon = Instantiate(pigeonPrefab, spawnPos, transform.rotation);
                        PigeonAI ai = pigeon.GetComponent<PigeonAI>();
                        ai.flock = 4;
                        ai.SetAI(GameDataHolder.botDifficulty);
                        pigeon.GetComponent<NetworkObject>().Spawn();
                    }
                }
                else
                {
                    currentSecond.Value = 500;
                    int spawnArea = Random.Range(0, spawnLocations.Count);
                    foreach (ulong client in NetworkManager.Singleton.ConnectedClientsIds)
                    {
                        Vector3 spawnPos = GetSpawnPos(spawnArea);
                        GameObject player = Instantiate(playerPrefab, spawnPos, transform.rotation);
                        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(client, true);
                    }
                    spawnArea = Random.Range(0, spawnLocations.Count);
                    for (int i = 0; i < 3; i++)
                    {
                        GameObject cone = Instantiate(unbuiltConePrefab, spawnLocations[spawnArea].transform.position, transform.rotation);
                        unbuiltConeGameobjects.Add(cone);
                        cone.GetComponent<NetworkObject>().Spawn();
                        spawnArea += Random.Range(3, 7);
                        if (spawnArea > spawnLocations.Count)
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
                        UpdatePigeonsForClientsClientRpc(pigeonStartData.ToArray());



                        if (GameDataHolder.gameMode == "Supremacy")
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
    [ClientRpc]
    private void UpdatePigeonsForClientsClientRpc(PigeonInitializeProperties[] data)
    {
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
    public void AddUpgradeToDisply(int upgrade)
    {
        upgradeHolder.SetActive(true);
        upgradeDisplays[upgrade].SetActive(true);
    }
    public void ShowUpgradeDes(int desc)
    {
        upgradeDescUI.SetActive(true);


        if (desc >= 0)
        {
            if (!upgradeScreen.activeSelf)
            {
                upgradeNameText.gameObject.SetActive(true);
                upgradeNameText.text = upgradeNames[desc];
            }
            upgradeDescText.text = upgradeDesc[desc];
        }
        else
        {
            switch ((Upgrades)desc)
            {
                case Upgrades.pigeonOfGrowth:
                    if (!upgradeScreen.activeSelf)
                    {
                        upgradeNameText.gameObject.SetActive(true);
                        upgradeNameText.text = repeatableUpgradeNames[0];
                    }
                    upgradeDescText.text = repeatableUpgradeDesc[0];
                    break;
                case Upgrades.pigeonOfMomentum:
                    if (!upgradeScreen.activeSelf)
                    {
                        upgradeNameText.gameObject.SetActive(true);
                        upgradeNameText.text = repeatableUpgradeNames[1];
                    }
                    upgradeDescText.text = repeatableUpgradeDesc[1];
                    break;
                case Upgrades.pigeonOfViolence:
                    if (!upgradeScreen.activeSelf)
                    {
                        upgradeNameText.gameObject.SetActive(true);
                        upgradeNameText.text = repeatableUpgradeNames[2];
                    }
                    upgradeDescText.text = repeatableUpgradeDesc[2];
                    break;
            }
        }



    }
    public void CloseUpgradeDes()
    {
        upgradeNameText.gameObject.SetActive(false);

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
                spectatingText.text = "Spectating " + pigeon.pigeonName;
                mainCamera.Follow = pigeon.transform;
                break;
            }
        }
    }
    public void ReturnToMainMenu()
    {
        NetworkManager.Singleton.Shutdown();
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
            bool hasAbilitySlotUnlocked = false;


            for (int x = 0; x < 1000; x++)
            {
                upgrade = allPigeonUpgrades[Random.Range(0, allPigeonUpgrades.Count)];
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
                upgrade = ((Upgrades)Random.Range(-1, -3));
            }


            upgradesUsed.Add(upgrade, 1);
            upgradesThatCanBeSelected[i] = upgrade;
            if ((int)upgrade >= 0)
            {
                upgradeDescibers[i].desc = (int)upgrade;
                upgradeButtonImages[i].sprite = upgradeButtonSprites[(int)upgrade];
                upgradeButtonText[i].text = upgradeNames[(int)upgrade];
            }
            else
            {
                switch (upgrade)
                {
                    case Upgrades.pigeonOfGrowth:
                        upgradeDescibers[i].desc = -3;
                        upgradeButtonImages[i].sprite = repeatableUpgradeButtonSprites[0];
                        upgradeButtonText[i].text = repeatableUpgradeNames[0];
                        break;
                    case Upgrades.pigeonOfMomentum:
                        upgradeDescibers[i].desc = -2;
                        upgradeButtonImages[i].sprite = repeatableUpgradeButtonSprites[1];
                        upgradeButtonText[i].text = repeatableUpgradeNames[1];
                        break;
                    case Upgrades.pigeonOfViolence:
                        upgradeDescibers[i].desc = -1;
                        upgradeButtonImages[i].sprite = repeatableUpgradeButtonSprites[2];
                        upgradeButtonText[i].text = repeatableUpgradeNames[2];
                        break;
                }
            }

        }
    }
    public void AddExtraUpgradePick()
    {
        extraUpgradeSelectionGameobject.SetActive(true);
        mainCamera.Lens.OrthographicSize = 8;
    }
    public void SelectUpgrade(int selected)
    {
        clickSound.Play();
        upgradeScreen.SetActive(false);
        upgradeDescUI.SetActive(false);
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
        while (true)
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
        mainCamera.Follow = pigeon.transform;
        spectatingText.text = "Spectating " + pigeon.pigeonName;
    }
    public void SpectatePreviouse()
    {

        while (true)
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
    [ServerRpc]
    public void ConstructIceCreamConeServerRpc(Vector3 pos)
    {
        GameObject cone = Instantiate(builtConePrefab, pos, transform.rotation);
        cone.GetComponent<NetworkObject>().Spawn();
        foreach (GameObject unbuiltCones in unbuiltConeGameobjects)
        {
            Destroy(unbuiltCones);
        }
        iceCreamUI.SetActive(true);

        gracePeriod = true;
        gameObjectiveText.text = "Get Cones";
    }
    [ClientRpc]
    public void ActiveateIcecreamUIClientRpc()
    {
        iceCreamUI.SetActive(true);

    }


    public Vector3 GetSpawnPos()
    {
        return GetSpawnPos(Random.Range(0, spawnLocations.Count));
    }
    public Vector3 GetSpawnPos(int location)
    {
        Transform pos = spawnLocations[Random.Range(0, spawnLocations.Count)];
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
    private void Update()
    {
        if (currentSecond.Value == -1) return;
        int minutes = Mathf.RoundToInt(currentSecond.Value / 60);
        borderTransform.localScale = new Vector3(borderSize.Value, borderSize.Value, 0);



        if (GameDataHolder.gameMode == "Supremacy")
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

        }
        else
        {
            timeleftText.text = Mathf.RoundToInt((currentSecond.Value / 500) * 100) + "%";
            icecreamBar.fillAmount = (float)currentSecond.Value / 500;

        }




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

        healthBar.fillAmount = (float)player.currentHP.Value / player.maxHp.Value;
        hpText.text = (float)player.currentHP.Value + "/" + player.maxHp.Value;
        xpBar.fillAmount = (float)player.xp / player.xpTillLevelUp;
        levelText.text = player.level.Value.ToString();

        if (IsServer && canSpawnFood && !isSuddenDeath.Value && gracePeriod)
        {
            SpawnFoodServerRpc();
            if (!hasOpenedChurch && currentSecond.Value < 120)
            {
                hasOpenedChurch = true;
                OpenChurchDoorClientRpc();
            }
        }
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

        yield return new WaitForSeconds(0.5f);
        Vector3 pos = GetSpawnPos();

        GameObject food = Instantiate(FoodPrefab, pos, transform.rotation);
        food.GetComponent<NetworkObject>().Spawn();
        canSpawnFood = true;
    }
    IEnumerator DepreciateIceCream()
    {
        yield return new WaitForSeconds(0.1f);
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
        currentSecond.Value = 500;
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
    }

    [ClientRpc]
    private void OpenChurchDoorClientRpc()
    {
        if (churchDoor) churchDoor.SetActive(false);
    }
}
