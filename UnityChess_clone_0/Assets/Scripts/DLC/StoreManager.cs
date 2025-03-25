using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using static System.Net.Mime.MediaTypeNames;
using System.Diagnostics;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;



public class StoreManager : MonoBehaviour
{

    private FirebaseFirestore db;
    private FirebaseAuth auth;
    private FirebaseUser user;

    [System.Serializable]
    public class SkinData
    {
        public string name;
        public string imageUrl;
        public int price;
        public bool isPurchased;
        public string localPath;
    }



    [Header("UI References")]
    public GameObject skinItemPrefab;
    public Transform contentParent; // ScrollView > Viewport > Content

    // Sample data (mocked for now)
    private List<SkinData> allSkins = new List<SkinData>()
    {
        new SkinData
        {
            name = "Galaxy Skin",
            imageUrl = "https://firebasestorage.googleapis.com/v0/b/chessmultiplayer-ed534.firebasestorage.app/o/Galaxy.jpg?alt=media&token=b1bbaed4-c588-41e5-a848-f4fa0bdeae64",
            price = 100,
            isPurchased = false
        }
    };

    void Awake()
    {
        FirebaseFirestore.DefaultInstance.Settings.PersistenceEnabled = false;
    }

    void Start()
    {
        UnityEngine.Debug.Log($"Skins will be saved at: {UnityEngine.Application.persistentDataPath}");

        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;

        if (auth.CurrentUser != null)
        {
            user = auth.CurrentUser;
            UnityEngine.Debug.Log("[AUTH] User is already signed in.");
            LoadOwnedSkins(PopulateStoreUI);
        }
        else
        {
            auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    user = auth.CurrentUser;
                    UnityEngine.Debug.Log($"[AUTH] Signed in anonymously as: {user.UserId}");
                    LoadOwnedSkins(PopulateStoreUI);
                }
                else
                {
                    UnityEngine.Debug.LogError("[AUTH] Failed to sign in anonymously.");
                }
            });
        }
    }



    void PopulateStoreUI()
    {
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        foreach (var skin in allSkins)
        {
            GameObject item = Instantiate(skinItemPrefab, contentParent);

            var skinNameText = item.transform.Find("SkinNameText").GetComponent<TextMeshProUGUI>();
            var priceText = item.transform.Find("PriceText").GetComponent<TextMeshProUGUI>();
            var previewImage = item.transform.Find("PreviewImage").GetComponent<UnityEngine.UI.Image>();
            var purchaseBtn = item.transform.Find("PurchaseButton").GetComponent<Button>();
            var purchaseText = item.transform.Find("PurchaseButton/Text (TMP)").GetComponent<TextMeshProUGUI>();
            var useSkinBtn = item.transform.Find("UseSkinButton").GetComponent<Button>();
            var useSkinText = item.transform.Find("UseSkinButton/Text (TMP)").GetComponent<TextMeshProUGUI>();

            // Populate UI
            skinNameText.text = skin.name;
            priceText.text = $"Price: {skin.price} Credits";

            StartCoroutine(LoadImage(skin.imageUrl, previewImage));

            if (skin.isPurchased)
            {
                purchaseBtn.interactable = false;
                purchaseText.text = "Owned";

                useSkinBtn.gameObject.SetActive(true);
                useSkinBtn.onClick.AddListener(() =>
                {
                    UnityEngine.Debug.Log($"[SKIN] Selected skin: {skin.name}");
                    SendSelectedSkinToServer(skin.name);
                });
            }
            else
            {
                useSkinBtn.gameObject.SetActive(false);
                purchaseBtn.onClick.AddListener(() =>
                {
                    UnityEngine.Debug.Log($"Purchased {skin.name} for {skin.price} credits.");
                    StartCoroutine(DownloadAndSaveSkin(skin));

                    purchaseBtn.interactable = false;
                    purchaseText.text = "Owned";

                    useSkinBtn.gameObject.SetActive(true);
                    useSkinBtn.onClick.AddListener(() =>
                    {
                        UnityEngine.Debug.Log($"[SKIN] Selected skin: {skin.name}");
                        SendSelectedSkinToServer(skin.name);
                    });
                });
            }
        }
    }



    System.Collections.IEnumerator DownloadAndSaveSkin(SkinData skin)
    {
        string fileName = skin.name.Replace(" ", "_") + ".jpg";
        string savePath = System.IO.Path.Combine(UnityEngine.Application.persistentDataPath, fileName);

        // Skip download if file already exists
        if (System.IO.File.Exists(savePath))
        {
            UnityEngine.Debug.Log($"[INFO] Skin '{skin.name}' already exists at: {savePath}");
            skin.localPath = savePath;
            skin.isPurchased = true;

            UnityEngine.Debug.Log($"[DEBUG] Current Firebase User ID: {user?.UserId}");

            // Save ownership to Firestore
            if (user != null)
            {
                DocumentReference docRef = db.Collection("users").Document(user.UserId)
                    .Collection("ownedSkins").Document(skin.name.Replace(" ", "_"));


                Dictionary<string, object> skinData = new Dictionary<string, object>
            {
                { "name", skin.name },
                { "localPath", savePath },
                { "timestamp", Firebase.Firestore.FieldValue.ServerTimestamp }
            };

                docRef.SetAsync(skinData).ContinueWithOnMainThread(task =>
                {
                    if (task.IsCompleted && !task.IsFaulted && !task.IsCanceled)
                    {
                        UnityEngine.Debug.Log($"[FIRESTORE] Ownership of '{skin.name}' saved for user {user.UserId}");
                    }
                    else
                    {
                        UnityEngine.Debug.LogError($"[FIRESTORE] Failed to save ownership of '{skin.name}': {task.Exception?.Message}");
                    }
                });
            }

            yield break;
        }

        using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(skin.imageUrl))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                UnityEngine.Debug.LogError($"[ERROR] Failed to download skin: {www.error}");
            }
            else
            {
                Texture2D texture = ((UnityEngine.Networking.DownloadHandlerTexture)www.downloadHandler).texture;
                byte[] bytes = texture.EncodeToJPG();
                System.IO.File.WriteAllBytes(savePath, bytes);

                skin.localPath = savePath;
                skin.isPurchased = true;

                UnityEngine.Debug.Log($"[SUCCESS] Skin '{skin.name}' saved to: {savePath}");

                //Debug Firebase User ID
                UnityEngine.Debug.Log($"[DEBUG] Current Firebase User ID: {user?.UserId}");

                // Save ownership to Firestore
                if (user != null)
                {
                    DocumentReference docRef = db.Collection("users").Document(user.UserId)
                                                 .Collection("ownedSkins").Document(skin.name);

                    Dictionary<string, object> skinData = new Dictionary<string, object>
                {
                    { "name", skin.name },
                    { "localPath", savePath },
                    { "timestamp", Firebase.Firestore.FieldValue.ServerTimestamp }
                };

                    docRef.SetAsync(skinData).ContinueWithOnMainThread(task =>
                    {
                        if (task.IsCompleted && !task.IsFaulted && !task.IsCanceled)
                        {
                            UnityEngine.Debug.Log($"[FIRESTORE] Ownership of '{skin.name}' saved for user {user.UserId}");
                        }
                        else
                        {
                            UnityEngine.Debug.LogError($"[FIRESTORE] Failed to save ownership of '{skin.name}': {task.Exception?.Message}");
                        }
                    });
                }
            }
        }
    }


    System.Collections.IEnumerator LoadImage(string url, UnityEngine.UI.Image targetImage)

    {
        using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                UnityEngine.Debug.LogError($"Failed to load image: {www.error}");
            }
            else
            {
                Texture2D texture = ((UnityEngine.Networking.DownloadHandlerTexture)www.downloadHandler).texture;
                Sprite sprite = Sprite.Create(texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f));
                targetImage.sprite = sprite;
            }
        }
    }

    private void SendSelectedSkinToServer(string skinName)
    {
        if (SkinSyncManager.Instance == null)
        {
            UnityEngine.Debug.LogError("[SKIN] SkinSyncManager.Instance is NULL. Aborting skin sync.");
            return;
        }

        if (!Unity.Netcode.NetworkManager.Singleton.IsServer && !Unity.Netcode.NetworkManager.Singleton.IsClient)
        {
            UnityEngine.Debug.LogWarning("[SKIN] Network is not running. Skipping skin sync.");
            return;
        }

        var data = new SkinSyncManager.SkinSelectionData { skinName = skinName };
        string json = JsonUtility.ToJson(data);
        UnityEngine.Debug.Log("[SKIN] Sending selected skin to server: " + skinName);
        SkinSyncManager.Instance.SendSelectedSkinToServerRpc(json);
    }



    private void LoadOwnedSkins(System.Action callback)
    {
        if (user == null)
        {
            UnityEngine.Debug.LogWarning("[FIRESTORE] Cannot load owned skins — user is null.");
            callback?.Invoke();
            return;
        }

        UnityEngine.Debug.Log($"[FIRESTORE] Loading owned skins for user: {user.UserId}");

        db.Collection("users").Document(user.UserId)
          .Collection("ownedSkins")
          .GetSnapshotAsync().ContinueWithOnMainThread(task =>
          {
              if (task.IsFaulted || task.IsCanceled)
              {
                  UnityEngine.Debug.LogError("[FIRESTORE] Failed to load owned skins.");
                  callback?.Invoke();
                  return;
              }

              QuerySnapshot snapshot = task.Result;

              foreach (DocumentSnapshot doc in snapshot.Documents)
              {
                  string skinName = doc.Id;
                  UnityEngine.Debug.Log($"[FIRESTORE] Document found: {skinName}");


                  string localPath = null;
                  if (doc.TryGetValue<string>("localPath", out var path))
                  {
                      localPath = path;
                  }

                  SkinData skin = allSkins.Find(s => s.name.Replace(" ", "_") == skinName);
                  if (skin != null)
                  {
                      skin.isPurchased = true;
                      skin.localPath = localPath;
                      UnityEngine.Debug.Log($"[FIRESTORE] Marked '{skin.name}' as owned.");
                  }
              }

              callback?.Invoke();
          });
    }


}
