using Firebase.Firestore;
using Firebase.Auth;
using Firebase.Extensions;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;

public class DLCManager : MonoBehaviour
{
    public static DLCManager Instance;

    private FirebaseFirestore db;
    private string userId;


    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    private void Start()
    {
        db = FirebaseFirestore.DefaultInstance;

        userId = FirebaseAuth.DefaultInstance.CurrentUser?.UserId ?? "guest";

        UnityEngine.Debug.Log($"[DLC] UserID = {userId}");
    }

    public void UnlockSkin(string skinName)
    {
        if (db == null || string.IsNullOrEmpty(userId))
        {
            UnityEngine.Debug.LogWarning("[DLC] Firebase not ready — ignoring unlock.");
            return;
        }

        DocumentReference docRef = db.Collection("players").Document(userId);

        docRef.UpdateAsync("unlockedSkins", FieldValue.ArrayUnion(skinName)).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && !task.IsFaulted)
                UnityEngine.Debug.Log($"[DLC] Unlocked skin: {skinName}");
            else
                UnityEngine.Debug.LogWarning($"[DLC] Failed to unlock skin: {skinName}");
        });
    }

    public void SelectSkin(string skinName)
    {
        if (db == null || string.IsNullOrEmpty(userId))
        {
            UnityEngine.Debug.LogWarning("[DLC] Firebase not ready — ignoring selection.");
            return;
        }

        DocumentReference docRef = db.Collection("players").Document(userId);

        docRef.UpdateAsync(new Dictionary<string, object> {
            { "selectedSkin", skinName }
        }).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && !task.IsFaulted)
                UnityEngine.Debug.Log($"[DLC] Selected skin: {skinName}");
            else
                UnityEngine.Debug.LogWarning($"[DLC] Failed to select skin: {skinName}");
        });
    }

    public void LoadUnlockedSkins(System.Action<List<string>, string> callback)
    {
        if (db == null || string.IsNullOrEmpty(userId))
        {
            UnityEngine.Debug.LogWarning("[DLC] Firebase not ready — returning default unlocked skin.");
            callback?.Invoke(new List<string> { "GreenSkinSet" }, "GreenSkinSet");
            return;
        }

        DocumentReference docRef = db.Collection("players").Document(userId);

        docRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && !task.IsFaulted)
            {
                DocumentSnapshot snapshot = task.Result;
                List<string> unlocked = new List<string>();
                string selected = "";

                if (snapshot.Exists)
                {
                    if (snapshot.TryGetValue("unlockedSkins", out List<string> unlockedList))
                        unlocked = unlockedList;

                    if (snapshot.TryGetValue("selectedSkin", out string selectedSkin))
                        selected = selectedSkin;
                }

                callback?.Invoke(unlocked, selected);
            }
            else
            {
                UnityEngine.Debug.LogWarning("[DLC] Failed to load unlocked skins.");
                callback?.Invoke(new List<string>(), "");
            }
        });
    }

}
