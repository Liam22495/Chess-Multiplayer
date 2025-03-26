using UnityEngine;
using Unity.Services.Core;
using UnityEngine.Analytics;
using System.Collections.Generic;

/// <summary>
/// Initializes Unity Analytics services on game start.
/// Attach this script to a persistent GameObject in your main scene (e.g., NetworkBootstrap).
/// </summary>
public class AnalyticsInitializer : MonoBehaviour
{

        private void Start()
        {
            UnityEngine.Analytics.Analytics.enabled = true;
            UnityEngine.Debug.Log("[Analytics] Classic Unity Analytics is enabled.");
        }


        public static void LogMatchStart(string matchId, string playerSide, string playerId)
    {
        Analytics.CustomEvent("match_started", new Dictionary<string, object>
        {
            { "match_id", matchId },
            { "player_side", playerSide },
            { "player_id", playerId },
            { "timestamp", System.DateTime.UtcNow.ToString("o") }
        });

        UnityEngine.Debug.Log("[Analytics] Match started event sent.");
    }
}
