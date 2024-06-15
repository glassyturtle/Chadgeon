using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class KtownManager : NetworkBehaviour
{
    public static KtownManager instance;

    [SerializeField] private List<DirtPileScript> dirtPiles;

    [SerializeField] private GameObject digParticle;


    private void Awake()
    {
        instance = this;
    }


    public IEnumerator RespawnDigSites()
    {
        while (true)
        {
            yield return new WaitForSeconds(20);
            if (GameManager.instance.gracePeriod)
            {
                int dirt = Random.Range(0, dirtPiles.Count);
                Debug.Log(!dirtPiles[dirt].isActiveAndEnabled);
                if (!dirtPiles[dirt].isActiveAndEnabled)
                {
                    ActivateDirtClientRpc(dirt, false);
                }
            }

            yield return null;
        }
    }

    public void DigDirt(DirtPileScript dirt)
    {
        DigDirtServerRpc(dirt.transform.position, dirtPiles.IndexOf(dirt));
    }


    [ServerRpc(RequireOwnership = false)]
    private void DigDirtServerRpc(Vector3 location, int dirtPileIndex)
    {
        GameObject particl = Instantiate(digParticle, location, transform.rotation);
        Debug.Log(particl.name);
        particl.GetComponent<NetworkObject>().Spawn();
        if (Random.Range(0, 100) <= 50)
        {
            //spawns worm
            GameManager.instance.SpawnPigeonDuringGameplay(location, 0, 16, "Worm");
        }
        ActivateDirtClientRpc(dirtPileIndex, true);
    }

    [ClientRpc]
    private void ActivateDirtClientRpc(int index, bool hide)
    {
        if (hide)
        {
            dirtPiles[index].gameObject.SetActive(false);
        }
        else
        {
            dirtPiles[index].gameObject.SetActive(true);
        }
    }
}
