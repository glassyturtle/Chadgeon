using UnityEngine;

public class CustomizationManager : MonoBehaviour
{
    public static CustomizationManager Instance { get; private set; }
    [SerializeField] private SkinSO[] baseSkinSOs;
    [SerializeField] private SkinSO[] headSkinSOs;
    [SerializeField] private SkinSO[] bodySkinSOs;

    private void Awake()
    {
        Instance = this;
    }

    public enum SpriteType
    {
        baseSkin,
        head,
        body
    }
    public Sprite GetSprite(SpriteType type, int skinID, int spriteIndex)
    {
        switch (type)
        {
            case SpriteType.head:
                return headSkinSOs[skinID].sprites[spriteIndex];
            case SpriteType.body:
                return bodySkinSOs[skinID].sprites[spriteIndex];
            case SpriteType.baseSkin:
                return baseSkinSOs[skinID].sprites[spriteIndex];
            default:
                break;
        }
        return null;
    }
}