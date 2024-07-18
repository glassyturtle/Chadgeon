using UnityEngine;

public class CreditsScrollScript : MonoBehaviour
{
    public void StartScroll()
    {
        LeanTween.cancel(gameObject);
        transform.localPosition = new Vector3(0, -1100, 0);
        LeanTween.moveLocalY(gameObject, 14200, 190);
    }
}
