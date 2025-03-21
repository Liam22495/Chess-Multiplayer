using UnityChess;
using UnityEngine;

[System.Serializable]
public class MoveData
{
    public string from;
    public string to;
    public string promotionPiece;

    public MoveData(Square start, Square end, string promotionPiece = "")
    {
        from = start.ToString().ToLower();
        to = end.ToString().ToLower();
        this.promotionPiece = promotionPiece;
    }
}
