using UnityEngine;

public class GothamManager : MonoBehaviour
{
    public static GothamManager instance;
    bool hasMomDied = false;
    bool hasDadDied = true;
    // Update is called once per frame
    private void Start()
    {
        instance = this;
    }
    public void DeathOfParents(bool isDad)
    {
        if (isDad)
        {
            hasDadDied = true;
        }
        else
        {
            hasMomDied = true;
        }
        if (hasDadDied && hasMomDied)
        {
            SteamIntegration.instance.UnlockAchivement("dark");
        }
    }
}
