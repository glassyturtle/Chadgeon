using UnityEngine;
using UnityEngine.Tilemaps;

public class BuildingScript : MonoBehaviour
{
    [SerializeField] TilemapRenderer sr;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Pigeon pigeonEntered = collision.gameObject.GetComponent<Pigeon>();
        if (pigeonEntered != null && GameManager.instance.player == pigeonEntered)
        {
            sr.enabled = false;
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        Pigeon pigeonEntered = collision.gameObject.GetComponent<Pigeon>();
        if (pigeonEntered != null && GameManager.instance.player == pigeonEntered)
        {
            sr.enabled = true;
        }
    }
}
