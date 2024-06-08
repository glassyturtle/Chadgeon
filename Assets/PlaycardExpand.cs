using UnityEngine;

public class PlaycardExpand : MonoBehaviour
{
    public void OnEnable()
    {
        transform.localScale = Vector3.one;
    }
    public void Expand()
    {
        LeanTween.scale(gameObject, new Vector3(1.2f, 1.2f, 1.2f), 0.1f);
    }
    public void Despan()
    {
        LeanTween.scale(gameObject, new Vector3(1f, 1f, 1f), 0.1f);

    }
}
