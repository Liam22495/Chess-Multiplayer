using Firebase;
using Firebase.Auth;
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

                SignInAnonymously();
            }
            else
            {
                Debug.LogError($"[Firebase] Initialization failed: {task.Result}");
            }
        });
    }

    private void SignInAnonymously()
    {
        FirebaseAuth.DefaultInstance.SignInAnonymouslyAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                Debug.Log($"[Firebase] Signed in anonymously | UID: {FirebaseAuth.DefaultInstance.CurrentUser.UserId}");
            }
            else
            {
                Debug.LogError("[Firebase] Anonymous sign-in failed");
            }
        });
    }
}
