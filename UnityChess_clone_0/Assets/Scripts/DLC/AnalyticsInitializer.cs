using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Analytics;
using System.Collections.Generic;

/// <summary>
/// Initializes Unity Analytics services on game start.
/// Attach this script to a persistent GameObject in your main scene (e.g., NetworkBootstrap).
/// </summary>
public class AnalyticsInitializer : MonoBehaviour
{
    private async void Start()
    {
        try
        {
            // Initialize Unity Services
            await UnityServices.InitializeAsync();

            // Start collecting analytics data
            AnalyticsService.Instance.StartDataCollection();

            UnityEngine.Debug.Log("[Analytics] Unity Analytics initialized successfully.");
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"[Analytics] Failed to initialize Unity Analytics: {e.Message}");
        }
    }
}
