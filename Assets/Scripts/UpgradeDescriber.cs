using UnityEngine;
using UnityEngine.EventSystems;

public class UpgradeDescriber : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public GameManager gm;
    public int desc;
    public void OnPointerEnter(PointerEventData eventData)
    {
        gm.ShowUpgradeDes(desc);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        gm.CloseUpgradeDes();
    }
}
