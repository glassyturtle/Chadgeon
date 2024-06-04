using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AbilityCooldownIconScript : MonoBehaviour
{
    [SerializeField] Image coolDownBar;
    [SerializeField] TMP_Text coolDownText;

    public void Show()
    {
        gameObject.SetActive(true);
    }
    public void StartCooldown(int cooldownTime)
    {
        if (gameObject.activeInHierarchy)
            StartCoroutine(CoolDown(cooldownTime));
    }
    IEnumerator CoolDown(int seconds)
    {
        float currentSecond = 0;
        while (true)
        {
            currentSecond += Time.deltaTime;
            coolDownBar.fillAmount = currentSecond / seconds;
            coolDownText.text = Mathf.FloorToInt(seconds + 1 - currentSecond).ToString();

            if (currentSecond >= seconds)
            {
                //cooldown stoped
                coolDownBar.fillAmount = 1;
                coolDownText.text = "";
                yield break;
            }
            yield return null;

        }
    }
}
