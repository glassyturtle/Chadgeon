using Unity.Netcode;
using UnityEngine;

public class SmoothNetworkTransform : NetworkBehaviour
{
    public Transform targetTransform;
    public float positionLerpRate = 5f;

    private NetworkVariable<Vector3> networkPosition = new NetworkVariable<Vector3>();

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            networkPosition.Value = targetTransform.position;
        }

        // Subscribe to network variable changes
        networkPosition.OnValueChanged += OnNetworkPositionChanged;
    }

    void Update()
    {
        if (IsServer)
        {
            networkPosition.Value = targetTransform.position;
        }
        else
        {
            targetTransform.position = Vector3.Lerp(targetTransform.position, networkPosition.Value, Time.deltaTime * positionLerpRate);
        }
    }

    private void OnNetworkPositionChanged(Vector3 oldPosition, Vector3 newPosition)
    {
        // Implement logic if needed when the position changes
    }


    public override void OnDestroy()
    {
        // Unsubscribe from network variable changes
        networkPosition.OnValueChanged -= OnNetworkPositionChanged;
    }
}

