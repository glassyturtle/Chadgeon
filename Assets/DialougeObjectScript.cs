using UnityEngine;

public class DialougeObjectScript : MonoBehaviour
{
    [SerializeField] GameObject interactText;
    bool isInInteractRange = false;
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
            GameManager.instance.OpenDialouge(dialougeImage, textToSay[Random.Range(0, textToSay.Length)], thingNam);
        }
    }
}
