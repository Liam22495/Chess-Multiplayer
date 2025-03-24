using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using static System.Net.Mime.MediaTypeNames;
using System.Net.Mime;
using System.Diagnostics;

public class StoreManager : MonoBehaviour
{
    [System.Serializable]
    public class SkinData
    {
        public string name;
        public string imageUrl;
        public int price;
        public bool isPurchased;
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
        PopulateStoreUI();
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
                UnityEngine.Debug.Log($"Purchased {skin.name} for {skin.price} credits.");
            });
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
