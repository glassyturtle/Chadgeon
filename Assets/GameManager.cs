using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{

    public List<Pigeon.Upgrades> allPigeonUpgrades;

    public bool isSuddenDeath = false;
    public int currentSecound;
    public List<Pigeon> allpigeons = new List<Pigeon>();
    public Pigeon player;
    public GameObject pigeonPrefab;

    [SerializeField] AudioSource audioSource, clickSound;
    [SerializeField] AudioClip gigaChadSong;
    [SerializeField] int secondsTillSuddenDeath;
    [SerializeField] Image icecreamBar;
    [SerializeField] GameObject suddenDeathText, winScreen, loseScreen, defaultGUI, upgradeScreen, pauseMenu, cooldownIcon;
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

    [SerializeField] Image[] upgradeButtonImages;
    [SerializeField] TextMeshProUGUI[] upgradeButtonText;
    [SerializeField] Sprite[] upgradeButtonSprites;
    [SerializeField] int[] spriteLocationsForEachUpgrade;
    [SerializeField] string[] upgradeNames;

    bool canSpawnFood = true;
    private Pigeon.Upgrades[] upgradesThatCanBeSelected = new Pigeon.Upgrades[3];

    public void ChangeVolume()
    {
        AudioListener.volume = volumeSlider.value;
    }
    public void Win()
    {
        Time.timeScale = 0;
        winScreen.SetActive(true);
        defaultGUI.SetActive(false);
    }
    public void Lose()
    {
        Time.timeScale = 0;
        loseScreen.SetActive(true);
        defaultGUI.SetActive(false);
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
            defaultGUI.SetActive(true);
        }
        else
        {
            Time.timeScale = 0;
            pauseMenu.SetActive(true);
            defaultGUI.SetActive(false);
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



    private void Awake()
    {
        currentSecound = secondsTillSuddenDeath;  
        
        for (int i = 0; i < 0; i++)
        {
            GameObject pigeon = Instantiate(pigeonPrefab, new Vector3(Random.Range(-13f, 13f), Random.Range(-11f, 19f), 0), transform.rotation);
            PigeonAI ai = pigeon.GetComponent<PigeonAI>();
            ai.SetAI(SuperGM.difficulty);       
        }
    }
    private void Start()
    {
        StartCoroutine(DepreciateIceCream());
    }
    public void SetFullScreen()
    {
        Screen.fullScreen = !Screen.fullScreen;
    }
    private void Update()
    {
        if (!audioSource.isPlaying)
        {
            audioSource.clip = gigaChadSong;
            audioSource.loop = true;
            audioSource.Play();
        }
        if (!player) return;
        chageonName.text = "Chadgeon " + " lvl: " + player.level;
        if (canSpawnFood && !isSuddenDeath)
        {
            StartCoroutine(SpawnFoodDelay());
        }
        if(player.currentHP <= 0)
        {
            chadgeonDetialPic.sprite = criticalChad;
            hpBar.localScale = new Vector3(0, 1, 1);
            hpText.text = 0 + "/" + player.maxHp;
            chadgeonDetialPic.color = Color.white;

        }
        else
        {
            hpBar.localScale = new Vector3((float)player.currentHP / player.maxHp, 1, 1);
            hpText.text = player.currentHP + "/" + player.maxHp;

            if(player.currentHP >= player.maxHp / 2)
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
        xpBar.localScale = new Vector3((float)player.xp / player.xpTillLevelUp, 1, 1);
        xpText.text = player.xp + "/" + player.xpTillLevelUp;        

    }


    IEnumerator SpawnFoodDelay()
    {
        Instantiate(FoodPrefab, new Vector3(Random.Range(-13f, 13f), Random.Range(-11f, 19f), 0), transform.rotation);
        canSpawnFood = false;
        yield return new WaitForSeconds(0.35f);
        canSpawnFood = true;
    }
    IEnumerator DepreciateIceCream()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(1);
            currentSecound--;


            int minutes = Mathf.RoundToInt(currentSecound / 60);
            int seconds = currentSecound % 60;
            if(seconds < 10)
            {
                timeleftText.text = minutes + ":0" + seconds;

            }
            else
            {
                timeleftText.text = minutes + ":" + seconds;

            }

            icecreamBar.fillAmount = (float)currentSecound / secondsTillSuddenDeath;
            if (currentSecound <= 0)
            {
                suddenDeathText.SetActive(true);
                isSuddenDeath = true;
                yield break;

            }
            yield return null;
        }
    }
}
