using UnityEngine;

[CreateAssetMenu(fileName = "NewSkin", menuName = "Chess/Skin")]
public class SkinDataSO : ScriptableObject
{
    public string skinName;
    public Material boardMaterial;
    public Material whitePieceMaterial;
    public Material blackPieceMaterial;
}
