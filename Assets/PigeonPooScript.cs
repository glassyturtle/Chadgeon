using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class PigeonPooScript : NetworkBehaviour
{

    private void Start()
    {
        if (IsOwner)
            StartCoroutine(DestroyAfterSeconds());
    }
    IEnumerator DestroyAfterSeconds()
    {
        yield return new WaitForSeconds(30);
        Destroy(gameObject);
    }
}
