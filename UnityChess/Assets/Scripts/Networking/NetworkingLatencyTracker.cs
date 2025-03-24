using UnityEngine;
using Unity.Netcode;
using TMPro;

public class NetworkingLatencyTracker : NetworkBehaviour
{
    [SerializeField] private TMP_Text pingDisplay;

    private float pingInterval = 2f;
    private float timer = 0f;
    private float clientSentTime;

    private void Update()
    {
        if (!IsSpawned) return;

        timer += Time.deltaTime;

        if (timer >= pingInterval)
        {
            timer = 0f;

            if (IsServer)
            {
                float hostPing = NetworkManager.Singleton.IsHost ? 0f :
                    NetworkManager.Singleton.NetworkConfig.NetworkTransport
                        .GetCurrentRtt(NetworkManager.Singleton.LocalClientId);

                if (pingDisplay != null)
                    pingDisplay.text = $"Ping: {hostPing:F0} ms";
            }
            else if (IsClient)
            {
                clientSentTime = Time.time;
                SendPingRequestServerRpc(clientSentTime);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendPingRequestServerRpc(float timestamp, ServerRpcParams rpcParams = default)
    {
        // Respond to client with the timestamp they originally sent
        ReturnPingToClientClientRpc(timestamp);
    }

    [ClientRpc]
    private void ReturnPingToClientClientRpc(float sentTime)
    {
        float rttMilliseconds = (Time.time - sentTime) * 1000f;

        if (pingDisplay != null)
            pingDisplay.text = $"Ping: {rttMilliseconds:F0} ms";
    }
}
