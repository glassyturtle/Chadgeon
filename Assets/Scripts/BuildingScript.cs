using UnityEngine;

public class BuildingScript : MonoBehaviour
{

    private void OnTriggerEnter2D(Collider2D collision)
    {
        LeanTween.cancel(gameObject);
        Pigeon pigeonEntered = collision.gameObject.GetComponent<Pigeon>();
        if (pigeonEntered != null && GameManager.instance.player == pigeonEntered)
        {
            LeanTween.alpha(gameObject, 0, 0.3f);
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        LeanTween.cancel(gameObject);
        Pigeon pigeonEntered = collision.gameObject.GetComponent<Pigeon>();
        if (pigeonEntered != null && GameManager.instance.player == pigeonEntered)
        {
            LeanTween.alpha(gameObject, 1, 0.3f).setDelay(1);
        }
    }
}
