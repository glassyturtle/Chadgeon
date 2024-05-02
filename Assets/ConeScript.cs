using UnityEngine;

public class ConeScript : MonoBehaviour
{
    [SerializeField] GameObject interactText;
    bool isInInteractRange = false;


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
            //Interacts with the cone
            GameManager.instance.ConstructIceCreamConeServerRpc(transform.position);
        }
    }
}
