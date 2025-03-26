using UnityEngine;
using UnityEngine.UI;

public class DebugMatchEndButton : MonoBehaviour
{
    [SerializeField] private Button forceEndButton;

    private void Start()
    {
        if (forceEndButton != null)
        {
            forceEndButton.onClick.AddListener(() =>
            {
                GameEndHandler.Instance.BroadcastGameOver(true, "White"); // Simulate White wins
                UnityEngine.Debug.Log("[DEBUG] Force End Match triggered.");
            });
        }
        else
        {
            UnityEngine.Debug.LogWarning("[DEBUG] Force End Button not assigned.");
        }
    }
}
