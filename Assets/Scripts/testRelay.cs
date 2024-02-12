using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

public class testRelay : MonoBehaviour
{
    private async void Start()
    {
        try
        {
            await UnityServices.InitializeAsync();
            AuthenticationService.Instance.SignedIn += () =>
            {
                Debug.Log("signed in " + AuthenticationService.Instance.PlayerId);
            };
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

        }
        catch (RelayServiceException e)
        {
            Debug.LogException(e);
        }



    }


    public async void CreateRelay()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(12);

            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            GameDataHolder.joinCode = joinCode;



            RelayServerData relayServerData = new RelayServerData(allocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
            NetworkManager.Singleton.StartHost();
            NetworkManager.Singleton.SceneManager.LoadScene("LobbyMenu", LoadSceneMode.Single);

        }
        catch (RelayServiceException e)
        {
            Debug.LogException(e);
        }
    }

    public async void JoinRelay(string joinCode)
    {
        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            GameDataHolder.joinCode = joinCode;



            RelayServerData relayServerData = new RelayServerData(joinAllocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
            NetworkManager.Singleton.StartClient();
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
        }
    }
}
