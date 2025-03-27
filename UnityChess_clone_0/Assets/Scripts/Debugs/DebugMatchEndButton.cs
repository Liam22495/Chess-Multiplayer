using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using UnityChess;


public class DebugMatchEndButton : MonoBehaviour
{
    [SerializeField] private Button forceEndButton;

    private void Start()
    {
        if (forceEndButton != null)
        {
            forceEndButton.onClick.AddListener(HandleForceEndClick);
        }
        else
        {
            UnityEngine.Debug.LogWarning("[DEBUG] Force End Button not assigned.");
        }
    }

    private void HandleForceEndClick()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            string side = GameManager.Instance.SideToMove.ToString(); // Could use Side.White if testing
            GameEndHandler.Instance.BroadcastGameOver(true, side);
            UnityEngine.Debug.Log("[DEBUG] Force End Match triggered by Host.");
        }
        else
        {
            RequestForceEndServerRpc();
            UnityEngine.Debug.Log("[DEBUG] Force End Match request sent by Client.");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestForceEndServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        Side callerSide = TurnManager.Instance.GetAssignedSide(senderId);
        string side = callerSide.ToString();

        GameEndHandler.Instance.BroadcastGameOver(true, side);
        UnityEngine.Debug.Log($"[DEBUG] Force End Match executed on server for client {senderId} as side {side}.");
    }
}
