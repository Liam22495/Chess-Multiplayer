using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
public class DLCStoreUI : MonoBehaviour
{
    [Header("DLC Settings")]
    public List<SkinDataSO> availableSkins;

    [Header("UI References")]
    public Transform storePanel;
    public GameObject skinButtonPrefab;

    private void Start()
    {
        foreach (var skin in availableSkins)
        {
            GameObject buttonObj = Instantiate(skinButtonPrefab, storePanel);
            buttonObj.GetComponentInChildren<UnityEngine.UI.Text>().text = skin.skinName;


            Button btn = buttonObj.GetComponent<Button>();
            btn.onClick.AddListener(() => OnSkinClicked(skin));
        }
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
