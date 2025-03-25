using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class SkinSyncManager : NetworkBehaviour
{
    public static SkinSyncManager Instance;

    private Dictionary<ulong, string> playerSkins = new Dictionary<ulong, string>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SendSelectedSkinToServerRpc(string jsonSkinData, ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        SkinSelectionData data = JsonUtility.FromJson<SkinSelectionData>(jsonSkinData);

        UnityEngine.Debug.Log("[SkinSync] Server received skin '" + data.skinName + "' from client " + senderId);

        // Save locally
        playerSkins[senderId] = data.skinName;

        // Broadcast to all clients
        BroadcastSkinToClientsClientRpc(senderId, jsonSkinData);
    }

    [ClientRpc]
    private void BroadcastSkinToClientsClientRpc(ulong clientId, string jsonSkinData)
    {
        SkinSelectionData data = JsonUtility.FromJson<SkinSelectionData>(jsonSkinData);
        UnityEngine.Debug.Log("[SkinSync] Client received skin update: " + clientId + " -> " + data.skinName);

        //Store it on all clients
        playerSkins[clientId] = data.skinName;
    }


    [System.Serializable]
    public class SkinSelectionData
    {
        public string skinName;
    }

    public string GetSkinForClient(ulong clientId)
    {
        if (playerSkins.ContainsKey(clientId))
            return playerSkins[clientId];

        return null;
    }

}
