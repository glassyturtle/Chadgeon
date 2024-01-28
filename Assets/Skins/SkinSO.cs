using UnityEngine;


[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/SkinData", order = 1)]
public class SkinSO : ScriptableObject
{
    public string skinName;
    public Sprite hop1;
    public Sprite hop2;
    public Sprite punch1;
    public Sprite punch2;
    public Sprite slam;
    public Sprite fly1;
    public Sprite fly2;
}
