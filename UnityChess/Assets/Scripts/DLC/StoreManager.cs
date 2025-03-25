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

    void Start()
    {
        UnityEngine.Debug.Log($"Skins will be saved at: {UnityEngine.Application.persistentDataPath}");

        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;

        if (auth.CurrentUser != null)
        {
            user = auth.CurrentUser;
            PopulateStoreUI();
        }
        else
        {
            auth.SignInAnonymouslyAsync().ContinueWith(task =>
            {
                if (task.IsCompleted && !task.IsCanceled && !task.IsFaulted)
                {
                    user = auth.CurrentUser;
                    UnityEngine.Debug.Log($"[AUTH] Signed in anonymously as: {user.UserId}");
                    PopulateStoreUI();
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
        foreach (var skin in allSkins)
        {
            GameObject item = Instantiate(skinItemPrefab, contentParent);

            item.transform.Find("SkinNameText").GetComponent<TextMeshProUGUI>().text = skin.name;

            item.transform.Find("PriceText").GetComponent<TextMeshProUGUI>().text = $"Price: {skin.price} Credits";

            StartCoroutine(LoadImage(skin.imageUrl, item.transform.Find("PreviewImage").GetComponent<UnityEngine.UI.Image>()));

            item.transform.Find("PurchaseButton").GetComponent<Button>().onClick.AddListener(() =>
            {
                StartCoroutine(DownloadAndSaveSkin(skin));
                UnityEngine.Debug.Log($"Purchased {skin.name} for {skin.price} credits.");
            });


        }
    }

    System.Collections.IEnumerator DownloadAndSaveSkin(SkinData skin)
    {
        string fileName = skin.name.Replace(" ", "_") + ".jpg";
        string savePath = System.IO.Path.Combine(UnityEngine.Application.persistentDataPath, fileName);

        if (System.IO.File.Exists(savePath))
        {
            UnityEngine.Debug.Log($"[INFO] Skin '{skin.name}' already exists at: {savePath}");
            skin.localPath = savePath;
            skin.isPurchased = true;

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
                        UnityEngine.Debug.LogError($"[FIRESTORE] Failed to save ownership of '{skin.name}'");
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
                            UnityEngine.Debug.LogError($"[FIRESTORE] Failed to save ownership of '{skin.name}'");
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
}
