using UnityEngine;
using UnityEngine.UI;

public class RankScript : MonoBehaviour
{
    [SerializeField] Image backgroundSprite, rankIconSprite;

    [SerializeField] RankIconSO rankIcons;
    [SerializeField] RankIconSO[] rankBorders;

    public void UpdateRank(int rank)
    {


        if (rank >= 980)
        {
            backgroundSprite.sprite = rankBorders[6].rankIcons[9];
            rankIconSprite.sprite = rankIcons.rankIcons[13];
        }
        else
        {
            int rankBorder = Mathf.FloorToInt(rank / 140f);

            int rankIcon = Mathf.FloorToInt((rank % 140) / 10);

            int rankBorderType = rank % 10;

            backgroundSprite.sprite = rankBorders[rankBorder].rankIcons[rankBorderType];
            rankIconSprite.sprite = rankIcons.rankIcons[rankIcon];
        }
    }
}
