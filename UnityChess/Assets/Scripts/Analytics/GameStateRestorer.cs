using UnityEngine;
using UnityEngine.UI;
using Firebase.Firestore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Extensions;
using Unity.Netcode;
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
        if (string.IsNullOrEmpty(UserSession.CurrentUserId))
        {
            UnityEngine.Debug.LogWarning("[Firebase] Cannot load game — UserSession.CurrentUserId is empty.");
            return;
        }

        var db = Firebase.Firestore.FirebaseFirestore.DefaultInstance;
        var userId = UserSession.CurrentUserId;

        UnityEngine.Debug.Log($"[Firebase] Attempting to load last game for: {userId}");

        db.Collection("users").Document(userId).Collection("savedGames")
            .OrderByDescending("timestamp")
            .Limit(1)
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    UnityEngine.Debug.Log("[Firebase] Query completed.");

                    if (task.Result.Count == 0)
                    {
                        UnityEngine.Debug.LogWarning($"[Firebase] No saved games found for user: {userId}");
                        return;
                    }

                    var doc = task.Result.Documents.First();
                    UnityEngine.Debug.Log("[Firebase] Saved game document found.");

                    string savedGame = doc.ContainsField("gameState") ? doc.GetValue<string>("gameState") : null;

                    if (!string.IsNullOrEmpty(savedGame))
                    {
                        UnityEngine.Debug.Log("[Firebase] Valid 'gameState' found, attempting to load...");

                        GameManager.Instance.LoadGame(savedGame);
                        BoardManager.Instance.ClearBoard();

                        foreach ((Square square, Piece piece) in GameManager.Instance.CurrentPieces)
                        {
                            BoardManager.Instance.CreateAndPlacePieceGO(piece, square);
                        }

                        BoardManager.Instance.EnsureOnlyPiecesOfSideAreEnabled(GameManager.Instance.SideToMove);
                        UIManager.Instance?.ValidateIndicators();

                        if (NetworkManager.Singleton.IsHost)
                        {
                            string sideToMove = GameManager.Instance.SideToMove.ToString();
                            GameStateSync.Instance.SendGameStateToClientClientRpc(savedGame, sideToMove);
                            UnityEngine.Debug.Log("[Firebase] Synced restored game state to all clients.");

                            //Reassign host and client player roles after restoring the game
                            if (TurnManager.Instance != null)
                            {
                                var hostId = NetworkManager.Singleton.LocalClientId;
                                var clientIds = NetworkManager.Singleton.ConnectedClientsIds;

                                if (clientIds.Count >= 2)
                                {
                                    var otherClient = clientIds.FirstOrDefault(id => id != hostId);
                                    TurnManager.Instance.AssignPlayers(hostId, otherClient);
                                    UnityEngine.Debug.Log("[Restore] TurnManager reassigned host and client player sides.");
                                }
                            }
                        }

                        UnityEngine.Debug.Log("[Firebase] Game state restored successfully.");
                    }
                    else
                    {
                        UnityEngine.Debug.LogWarning("[Firebase] 'gameState' field was missing or empty.");
                    }
                }
                else
                {
                    UnityEngine.Debug.LogError("[Firebase] Failed to load last saved game: " + task.Exception?.Message);
                }
            });
    }

}