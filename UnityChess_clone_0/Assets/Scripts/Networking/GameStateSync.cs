using Unity.Netcode;
using UnityChess;
using UnityEngine;

public class GameStateSync : NetworkBehaviour
{
    public static GameStateSync Instance;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    [ClientRpc]
    public void SendGameStateToClientClientRpc(string serializedGame, string sideToMove, ClientRpcParams clientRpcParams = default)
    {
        UnityEngine.Debug.Log($"[GameStateSync] Received game state. SideToMove: {sideToMove}");

        GameManager.Instance.LoadGame(serializedGame);
        GameManager.Instance.ForceSetSideToMove(sideToMove);

        // Force-refresh visuals
        BoardManager.Instance.ClearBoard();

        foreach ((Square square, Piece piece) in GameManager.Instance.CurrentPieces)
        {
            BoardManager.Instance.CreateAndPlacePieceGO(piece, square);
        }

        BoardManager.Instance.EnsureOnlyPiecesOfSideAreEnabled(GameManager.Instance.SideToMove);
        UIManager.Instance.ValidateIndicators();
    }
}
