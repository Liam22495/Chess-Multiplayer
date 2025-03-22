using Firebase.Firestore;
using Firebase.Auth;
using Firebase.Extensions;
using System.Collections.Generic;
using UnityEngine;

public class DLCManager : MonoBehaviour
{
    public static DLCManager Instance;

    private FirebaseFirestore db;
    private string userId;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        userId = FirebaseAuth.DefaultInstance.CurrentUser?.UserId ?? "guest";
    }

    public void UnlockSkin(string skinName)
    {
        DocumentReference docRef = db.Collection("players").Document(userId);

        docRef.UpdateAsync("unlockedSkins", FieldValue.ArrayUnion(skinName)).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && !task.IsFaulted)
                Debug.Log($"[DLC] Unlocked skin: {skinName}");
            else
                Debug.LogWarning($"[DLC] Failed to unlock skin: {skinName}");
        });
    }

    public void SelectSkin(string skinName)
    {
        DocumentReference docRef = db.Collection("players").Document(userId);

        docRef.UpdateAsync(new Dictionary<string, object> {
            { "selectedSkin", skinName }
        }).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && !task.IsFaulted)
                Debug.Log($"[DLC] Selected skin: {skinName}");
            else
                Debug.LogWarning($"[DLC] Failed to select skin: {skinName}");
        });
    }
}

