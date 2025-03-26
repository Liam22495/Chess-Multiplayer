using UnityEngine;
using UnityEngine.UI;
using Firebase.Auth;
using Firebase.Firestore;
using System;
using System.Linq;
using System.Threading.Tasks;
using UnityChess;

public class GameStateRestorer : MonoBehaviour
{
    [SerializeField] private Button loadLastGameButton;

    private void Start()
    {
        if (loadLastGameButton != null)
        {
            loadLastGameButton.onClick.AddListener(LoadMostRecentGameFromFirebase);
        }
    }

    private void LoadMostRecentGameFromFirebase()
    {
        var user = FirebaseAuth.DefaultInstance?.CurrentUser;
        if (user == null)
        {
            UnityEngine.Debug.LogWarning("[Firebase] Cannot load game — user is null.");
            return;
        }

        var db = FirebaseFirestore.DefaultInstance;
        var userId = user.UserId;

        db.Collection("users").Document(userId).Collection("savedGames")
            .OrderByDescending("timestamp")
            .Limit(1)
            .GetSnapshotAsync()
            .ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully && task.Result.Count > 0)
                {
                    var doc = task.Result.Documents.First();
                    string savedGame = doc.ContainsField("gameState") ? doc.GetValue<string>("gameState") : null;

                    if (!string.IsNullOrEmpty(savedGame))
                    {
                        UnityEngine.Debug.Log("[Firebase] Restoring game...");
                        GameManager.Instance.LoadGame(savedGame);
                        BoardManager.Instance.ClearBoard();

                        foreach ((Square square, Piece piece) in GameManager.Instance.CurrentPieces)
                        {
                            BoardManager.Instance.CreateAndPlacePieceGO(piece, square);
                        }

                        BoardManager.Instance.EnsureOnlyPiecesOfSideAreEnabled(GameManager.Instance.SideToMove);
                        UIManager.Instance?.ValidateIndicators();
                    }
                    else
                    {
                        UnityEngine.Debug.LogWarning("[Firebase] No gameState found in document.");
                    }
                }
                else
                {
                    UnityEngine.Debug.LogError("[Firebase] Failed to load last saved game: " + task.Exception?.Message);
                }
            });
    }
}