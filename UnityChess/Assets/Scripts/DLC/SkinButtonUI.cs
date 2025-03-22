using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkinButtonUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text skinNameText;
    [SerializeField] private Button unlockButton;
    [SerializeField] private Button selectButton;

    private SkinDataSO skin;

    public void Init(SkinDataSO skinData, bool isUnlocked)
    {
        skin = skinData;
        skinNameText.text = skin.skinName;

        unlockButton.gameObject.SetActive(!isUnlocked);
        selectButton.gameObject.SetActive(isUnlocked);

        unlockButton.onClick.RemoveAllListeners();
        selectButton.onClick.RemoveAllListeners();

        unlockButton.onClick.AddListener(() => DLCManager.Instance.UnlockSkin(skin.skinId));
        selectButton.onClick.AddListener(() =>
        {
            DLCManager.Instance.SelectSkin(skin.skinId);
            DLCStoreUI.Instance.ApplySkin(skin);
        });
    }
}
