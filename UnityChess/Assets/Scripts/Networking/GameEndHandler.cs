using Unity.Netcode;
using UnityEngine;

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
            UIManager.Instance.ShowGameEndMessage(message); // implement this if needed
        }
    }
}
