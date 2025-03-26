using Unity.Netcode;
using UnityEngine;
using UnityEngine.Analytics;
using System.Collections.Generic; 

public class GameEndHandler : NetworkBehaviour
{
    public static GameEndHandler Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void BroadcastGameOver(bool checkmate, string winner)
    {
        string result = checkmate ? $"{winner} wins by checkmate!" : "Draw by stalemate!";
        SendGameEndToClientsClientRpc(result);
    }

    [ClientRpc]
    private void SendGameEndToClientsClientRpc(string message)
    {
        UnityEngine.Debug.Log($"[GameEnd] {message}");

        // Disable all piece input
        BoardManager.Instance.SetActiveAllPieces(false);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowGameEndMessage(message);
        }

        //Log match end analytics event
        LogMatchEndAnalytics(message);
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
}
