using System.Diagnostics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Unity.Services.Analytics;
using UnityEngine.Analytics;
using Unity.Services.Core;
using System.Collections.Generic;
using Firebase.Auth;
using Firebase.Extensions;
using System;

public class NetworkBootstrap : MonoBehaviour
{
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private Button serverButton;
    public GameObject latencyTrackerPrefab;
    private StoreManager storeManager;

    private void Start()
    {
        FirebaseAuth.DefaultInstance.SignOut();
        PlayerPrefs.DeleteKey("last_firebase_uid");
        PlayerPrefs.Save();
        UnityEngine.Debug.Log("[AUTH] Signed out and cleared UID cache on startup testing purpose");

        // Setup UI button events
        hostButton.onClick.AddListener(StartHost);
        clientButton.onClick.AddListener(StartClient);
        serverButton.onClick.AddListener(StartServer);

        // Find StoreManager
        storeManager = FindObjectOfType<StoreManager>();

        // Register networking events
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }


    private void StartHost()
    {
        UserSession.CurrentUserId = "Player1";
        storeManager.InitializeStoreAfterLogin();
        NetworkManager.Singleton.StartHost();
        UnityEngine.Debug.Log("[UserSession] Host started as Player1");
    }

    private void StartClient()
    {
        UserSession.CurrentUserId = "Player2";
        storeManager.InitializeStoreAfterLogin();
        if (NetworkManager.Singleton.StartClient())
        {
            UnityEngine.Debug.Log("[UserSession] Client started as Player2");
        }
        else
        {
            UnityEngine.Debug.LogWarning("Client connection failed.");
        }
    }


    private void SignInAndContinue(Action onSuccess)
    {
        var auth = FirebaseAuth.DefaultInstance;
        string cachedUid = PlayerPrefs.GetString("last_firebase_uid", "");

        if (auth.CurrentUser != null)
        {
            string currentUid = auth.CurrentUser.UserId;

            if (currentUid != cachedUid && !string.IsNullOrEmpty(cachedUid))
            {
                UnityEngine.Debug.LogWarning($"[AUTH] Mismatch detected! Cached UID = {cachedUid}, Current UID = {currentUid}. Signing out...");
                auth.SignOut();
                SignInAndContinue(onSuccess);
                return;
            }

            UnityEngine.Debug.Log("[AUTH] Already signed in with correct UID: " + currentUid);
            storeManager.InitializeStoreAfterLogin();
            onSuccess?.Invoke();
            return;
        }

        auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                string newUid = auth.CurrentUser.UserId;
                UnityEngine.Debug.Log("[AUTH] Signed in anonymously as: " + newUid);

                PlayerPrefs.SetString("last_firebase_uid", newUid);
                PlayerPrefs.Save();

                storeManager.InitializeStoreAfterLogin();
                onSuccess?.Invoke();
            }
            else
            {
                UnityEngine.Debug.LogError("[AUTH] Failed to sign in anonymously: " + task.Exception?.Message);
            }
        });
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

                //Log match start analytics event
                string matchId = System.Guid.NewGuid().ToString();
                string userId = FirebaseAuth.DefaultInstance?.CurrentUser?.UserId ?? "anonymous";
                LogMatchStartAnalytics(matchId, "White", userId);
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

    //Analytics function for match start
    private void LogMatchStartAnalytics(string matchId, string playerSide, string userId)
    {
        Analytics.CustomEvent("match_started", new Dictionary<string, object>
        {
            { "match_id", matchId },
            { "player_side", playerSide },
            { "player_id", userId },
            { "timestamp", System.DateTime.UtcNow.ToString("o") }
        });

        UnityEngine.Debug.Log("[Analytics] Match started event sent.");
    }
}