using Unity.Netcode;
using UnityEngine;

public class HostDCScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_OnClientDisconnectCallback;
        gameObject.SetActive(false);
    }


    private void NetworkManager_OnClientDisconnectCallback(ulong clientId)
    {
        Debug.Log("yee" + clientId + "  " + NetworkManager.Singleton.LocalClientId);
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            //server is sutting down
            gameObject.SetActive(true);
        }
    }
}
