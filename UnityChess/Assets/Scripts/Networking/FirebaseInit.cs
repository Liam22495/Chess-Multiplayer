using Firebase;
using Firebase.Extensions;
using UnityEngine;

public class FirebaseInit : MonoBehaviour
{
    private void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                Debug.Log("[Firebase] Initialized successfully");
            }
            else
            {
                Debug.LogError($"[Firebase] Initialization failed: {task.Result}");
            }
        });
    }
}
