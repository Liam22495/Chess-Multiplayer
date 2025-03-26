using Unity.Netcode;
using UnityEngine;
using UnityEngine.Analytics;
using System.Collections.Generic; 

public class GameEndHandler : NetworkBehaviour
{
    public static GameEndHandler Instance;

    private bool gameHasEnded = false; 


    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void BroadcastGameOver(bool checkmate, string winner)
    {
        if (gameHasEnded) return;
        gameHasEnded = true;

        string result = checkmate ? $"{winner} wins by checkmate!" : "Draw by stalemate!";
        SendGameEndToClientsClientRpc(result);
    }


    [ClientRpc]
    private void SendGameEndToClientsClientRpc(string message)
    {
        UnityEngine.Debug.Log($"[GameEnd] {message}");

        BoardManager.Instance.SetActiveAllPieces(false);

        // Show result message in UI
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowGameEndMessage(message);
        }

        // Log match end analytics event
        LogMatchEndAnalytics(message);
        SaveGameStateToFirestore(message);
    }


    //Analytics event logger for match end
    private void LogMatchEndAnalytics(string resultMessage)
    {
        string matchResult = resultMessage.Contains("Draw") ? "draw" : "win";
        string winnerSide = resultMessage.Contains("White") ? "White" :
                            resultMessage.Contains("Black") ? "Black" : "Unknown";

        // Temporary matchId — can later replace with persistent ID
        string matchId = "match-" + System.DateTime.UtcNow.Ticks;

        Analytics.CustomEvent("match_ended", new Dictionary<string, object>
        {
            { "match_id", matchId },
            { "result", matchResult },
            { "winner", winnerSide },
            { "timestamp", System.DateTime.UtcNow.ToString("o") }
        });

        UnityEngine.Debug.Log("[Analytics] Match end event sent.");
    }

    private void SaveGameStateToFirestore(string resultMessage)
    {
        var user = Firebase.Auth.FirebaseAuth.DefaultInstance?.CurrentUser;
        if (user == null)
        {
            UnityEngine.Debug.LogWarning("[Firebase] No signed-in user. Skipping game save.");
            return;
        }

        string gameState = GameManager.Instance.SerializeGame();
        string userId = user.UserId;

        var db = Firebase.Firestore.FirebaseFirestore.DefaultInstance;
        var gameData = new Dictionary<string, object>
    {
        { "gameState", gameState },
        { "timestamp", Firebase.Firestore.FieldValue.ServerTimestamp },
        { "result", resultMessage }
    };

        db.Collection("users").Document(userId).Collection("savedGames").AddAsync(gameData).ContinueWith(task =>
        {
            if (task.IsCompletedSuccessfully)
                UnityEngine.Debug.Log("[Firebase] Game state saved.");
            else
                UnityEngine.Debug.LogError("[Firebase] Failed to save game state: " + task.Exception?.Message);
        });
    }


    private void OnEnable()
    {
        GameManager.NewGameStartedEvent += ResetGameOverState;
    }

    private void OnDisable()
    {
        GameManager.NewGameStartedEvent -= ResetGameOverState;
    }

    private void ResetGameOverState()
    {
        gameHasEnded = false;
        UnityEngine.Debug.Log("[GameEndHandler] Game state reset — ready for next match.");
    }

}
