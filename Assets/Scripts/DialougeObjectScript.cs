using UnityEngine;

public class DialougeObjectScript : MonoBehaviour
{
    [SerializeField] GameObject interactText;
    bool isInInteractRange = false;
    [SerializeField] bool isCollector;
    [SerializeField] bool isFatherIce = false;
    [SerializeField] bool isMinion = false;
    [SerializeField] Sprite dialougeImage;
    [SerializeField] string[] textToSay;
    [SerializeField] string thingNam;

    private void Start()
    {
        interactText.SetActive(false);
        isInInteractRange = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Pigeon pigeon = collision.GetComponent<Pigeon>();
        if (pigeon && pigeon == GameManager.instance.player)
        {
            interactText.SetActive(true);
            isInInteractRange = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        Pigeon pigeon = collision.GetComponent<Pigeon>();
        if (pigeon && pigeon == GameManager.instance.player)
        {
            interactText.SetActive(false);
            isInInteractRange = false;
        }
    }


    private void Update()
    {
        if (!isInInteractRange) return;

        if (Input.GetKeyDown(KeyCode.F))
        {
            //Interacts with the dirtPile
            if (isCollector && GameManager.instance.player.pigeonUpgrades.ContainsKey(Pigeon.Upgrades.minionHelmet) && !GameManager.instance.player.pigeonUpgrades.ContainsKey(Pigeon.Upgrades.minionGoggles))
            {
                GameManager.instance.OpenDialouge(dialougeImage, "Oh, i see you have a fine piece of the past. I would love to purchase it from you but the developer has not added curency in this level... i could trade you for a much more valuable item if you are interested.", thingNam, 1);
            }
            else if (isFatherIce && GameManager.instance.player.level.Value >= 20)
            {
                if (KtownManager.instance.hasChosen.Value)
                {
                    GameManager.instance.OpenDialouge(dialougeImage, "The ice-cream god has already chosen a pigeon.", thingNam, 3);

                }
                else
                {
                    GameManager.instance.OpenDialouge(dialougeImage, "The ice-cream gods see you as a worthy candidate to inherit thier power. However, you must offer them one thing in return... your soul. *ahem* its not that bad. they just want your levels", thingNam, 2);
                }
            }
            else if (isMinion && GameManager.instance.player.pigeonUpgrades.ContainsKey(Pigeon.Upgrades.minionScript))
            {
                GameManager.instance.OpenDialouge(dialougeImage, "No! this cannot be. You know too much!", thingNam, 4);
            }
            else
            {
                GameManager.instance.OpenDialouge(dialougeImage, textToSay[Random.Range(0, textToSay.Length)], thingNam, 0);

            }
        }
    }
}
