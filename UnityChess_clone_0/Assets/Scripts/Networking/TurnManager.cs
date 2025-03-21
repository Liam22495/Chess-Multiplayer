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
            side = currentTurnSide.ToString()
        });

        SendTurnJsonToClientsClientRpc(json);
    }

    [ClientRpc]
    private void SendTurnJsonToClientsClientRpc(string json)
    {
        UnityEngine.Debug.Log($"[TurnManager] JSON Turn Update: {json}");

        TurnInfo receivedTurn = JsonUtility.FromJson<TurnInfo>(json);
        currentTurnClientId = receivedTurn.turnOwnerClientId;
        currentTurnSide = receivedTurn.side == "White" ? Side.White : Side.Black;
        BoardManager.Instance.SyncInteractablePiecesForTurn(currentTurnSide);
    }

    [System.Serializable]
    public class TurnInfo
    {
        public ulong turnOwnerClientId;
        public string side;
    }
}
