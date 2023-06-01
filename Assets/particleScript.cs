using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class particleScript : NetworkBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        if (!IsOwner) return;
        StartCoroutine(Destroy());
    }

    private IEnumerator Destroy()
    {
        yield return new WaitForSeconds(5);
        Destroy(gameObject);
    }

}
