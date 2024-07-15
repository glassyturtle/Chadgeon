using UnityEngine;
using UnityEngine.EventSystems;

public class UpgradeDescriber : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public bool isKtown = false;
    public int desc;
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isKtown) KtownManager.instance.ShowUpgradeDes(desc);
        else GameManager.instance.ShowUpgradeDes(desc);
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        GameManager.instance.CloseUpgradeDes();
    }
}
