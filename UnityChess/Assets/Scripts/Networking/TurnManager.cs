using System.Diagnostics;
using Unity.Netcode;
using UnityEngine;
using UnityChess;


public class TurnManager : NetworkBehaviour
{
    public static TurnManager Instance;

    private ulong whitePlayerId;
    private ulong blackPlayerId;

    private ulong currentTurnClientId;
    private Side currentTurnSide;
    public string syncedSideToMove;


    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    public void AssignPlayers(ulong hostId, ulong clientId)
    {
        whitePlayerId = hostId;
        blackPlayerId = clientId;
        currentTurnClientId = whitePlayerId;
        currentTurnSide = Side.White;

        SendTurnInfoToClients();
    }

    public bool IsClientTurn(ulong clientId)
    {
        return clientId == currentTurnClientId;
    }

    public void SwitchTurn()
    {
        if (currentTurnClientId == whitePlayerId)
        {
            currentTurnClientId = blackPlayerId;
            currentTurnSide = Side.Black;
        }
        else
        {
            currentTurnClientId = whitePlayerId;
            currentTurnSide = Side.White;
        }

        SendTurnInfoToClients();
    }

    private void SendTurnInfoToClients()
    {
        string json = JsonUtility.ToJson(new TurnInfo
        {
            turnOwnerClientId = currentTurnClientId,
            side = currentTurnSide.ToString(),
            syncedSideToMove = GameManager.Instance.SideToMove.ToString()
        });

        SendTurnJsonToClientsClientRpc(json);
    }

    public bool SideToMoveIsWhite()
    {
        return currentTurnSide == Side.White;
    }


    [ClientRpc]
    private void SendTurnJsonToClientsClientRpc(string json)
    {
        UnityEngine.Debug.Log($"[TurnManager] JSON Turn Update: {json}");

        TurnInfo receivedTurn = JsonUtility.FromJson<TurnInfo>(json);

        // Update local turn tracking
        currentTurnClientId = receivedTurn.turnOwnerClientId;
        currentTurnSide = receivedTurn.side == "White" ? Side.White : Side.Black;

        // Sync actual turn state in GameManager
        GameManager.Instance.ForceSetSideToMove(receivedTurn.syncedSideToMove);

        // Update UI
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ValidateIndicators();
        }

        // Enable all VisualPieces — forcefully — AFTER everything else
        VisualPiece[] allPieces = GameObject.FindObjectsOfType<VisualPiece>(true);
        foreach (var piece in allPieces)
        {
            piece.enabled = true;
            UnityEngine.Debug.Log($"[TurnManager] Final force-enable {piece.PieceColor} {piece.name}");
        }

        //Now apply correct piece restrictions for the turn
        BoardManager.Instance.EnsureOnlyPiecesOfSideAreEnabled(GameManager.Instance.SideToMove);
    }




    [System.Serializable]
    public class TurnInfo
    {
        public ulong turnOwnerClientId;
        public string side;
        public string syncedSideToMove;
    }
}
