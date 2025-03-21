using Unity.Netcode;
using UnityChess;
using UnityEngine;

public class MoveHandler : NetworkBehaviour
{
    public static MoveHandler Instance;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SubmitMoveServerRpc(string moveJson, ServerRpcParams rpcParams = default)
    {
        ulong senderClientId = rpcParams.Receive.SenderClientId;

        if (!TurnManager.Instance.IsClientTurn(senderClientId))
        {
            UnityEngine.Debug.LogWarning($"[MoveHandler] Client {senderClientId} tried to move out of turn.");
            return;
        }

        MoveData move = JsonUtility.FromJson<MoveData>(moveJson);

        Square from = new Square(move.from);
        Square to = new Square(move.to);

        // Validate move on server
        if (!GameManager.Instance.GetGame().TryGetLegalMove(from, to, out Movement legalMove))
        {
            UnityEngine.Debug.LogWarning($"[MoveHandler] REJECTED: No legal move from {from} to {to}. Turn: {TurnManager.Instance.IsClientTurn(senderClientId)} SideToMove: {GameManager.Instance.SideToMove}");

            UnityEngine.Debug.LogWarning($"[MoveHandler] Invalid move from {move.from} to {move.to} by client {senderClientId}");
            return;
        }

        // Execute move on server
        GameManager.Instance.ApplyNetworkMove(legalMove, move.promotionPiece);

        // Broadcast to all clients
        BroadcastMoveToClientsClientRpc(moveJson);

        // Switch turn
        TurnManager.Instance.SwitchTurn();
    }

    [ClientRpc]
    private void BroadcastMoveToClientsClientRpc(string moveJson)
    {
        MoveData move = JsonUtility.FromJson<MoveData>(moveJson);
        GameManager.Instance.ApplyMoveVisualsOnly(move);
    }
}
