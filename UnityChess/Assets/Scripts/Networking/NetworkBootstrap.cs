using System.Diagnostics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using static System.Net.Mime.MediaTypeNames;

public class NetworkBootstrap : MonoBehaviour
{
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private Button serverButton;
    public GameObject latencyTrackerPrefab;

    private void Start()
    {
        hostButton.onClick.AddListener(StartHost);
        clientButton.onClick.AddListener(StartClient);
        serverButton.onClick.AddListener(StartServer);

        // Subscribe to connection events
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }


    private void StartHost()
    {
        NetworkManager.Singleton.StartHost();
        UnityEngine.Debug.Log("Hosting...");
    }

    private void StartClient()
    {
        if (!NetworkManager.Singleton.StartClient())
            UnityEngine.Debug.LogWarning("Failed to connect as client.");
        else
            UnityEngine.Debug.Log("Connecting as client...");
    }


    private void StartServer()
    {
        NetworkManager.Singleton.StartServer();
        UnityEngine.Debug.Log("Server started.");
    }


    private void OnClientConnected(ulong clientId)
    {
        UnityEngine.Debug.Log($"Client connected: {clientId}");

        if (NetworkManager.Singleton.IsHost && TurnManager.Instance != null)
        {
            var hostId = NetworkManager.Singleton.LocalClientId;
            var clientIds = NetworkManager.Singleton.ConnectedClientsIds;

            if (clientIds.Count >= 2)
            {
                var otherClient = clientIds.FirstOrDefault(id => id != hostId);
                TurnManager.Instance.AssignPlayers(hostId, otherClient);
            }

            if (clientId != hostId)
            {
                string currentGame = GameManager.Instance.SerializeGame();
                string side = GameManager.Instance.SideToMove.ToString();

                GameStateSync.Instance.SendGameStateToClientClientRpc(currentGame, side, new ClientRpcParams
                {
                    Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
                });
            }
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        UnityEngine.Debug.LogWarning($"Client disconnected: {clientId}");
    }

}
