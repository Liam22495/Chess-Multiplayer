using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class DLCStoreUI : MonoBehaviour
{
    [Header("DLC Settings")]
    public List<SkinDataSO> availableSkins;

    [Header("UI References")]
    public Transform storePanel;
    public GameObject skinButtonPrefab;
    public static DLCStoreUI Instance;


    private void Start()
    {
        DLCManager.Instance.LoadUnlockedSkins((unlockedSkins, selectedSkin) =>
        {
            foreach (var skin in availableSkins)
            {
                GameObject buttonObj = Instantiate(skinButtonPrefab, storePanel);
                SkinButtonUI buttonUI = buttonObj.GetComponent<SkinButtonUI>();

                bool isUnlocked = unlockedSkins.Contains(skin.skinId);
                buttonUI.Init(skin, isUnlocked);

                if (skin.skinId == selectedSkin)
                {
                    ApplySkin(skin);
                }
            }
        });
    }


    private void Awake()
    {
        Instance = this;
    }


    private void OnSkinClicked(SkinDataSO skin)
    {
        DLCManager.Instance.SelectSkin(skin.skinName);
        ApplySkin(skin);
    }

    public void ApplySkin(SkinDataSO skin)
    {
        BoardManager.Instance.SetBoardMaterial(skin.boardMaterial);
        BoardManager.Instance.SetPieceMaterials(skin.whitePieceMaterial, skin.blackPieceMaterial);
    }
}
