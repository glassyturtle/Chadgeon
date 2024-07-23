using UnityEngine;

public class BruceWayneParentScript : MonoBehaviour
{
    [SerializeField] bool isDad;
    private void OnDestroy()
    {
        GothamManager.instance.DeathOfParents(isDad);
    }
}
