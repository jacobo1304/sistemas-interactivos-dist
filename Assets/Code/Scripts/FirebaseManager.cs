using System;
using System.Collections.Generic;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance { get; private set; }

    private FirebaseAuth auth;
    private FirebaseDatabase database;

    public string Username { get; private set; }
    public int HighScore { get; private set; }
    public bool IsLoggedIn
    {
        get
        {
            return auth != null && auth.CurrentUser != null;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeFirebase();
    }

    private void InitializeFirebase()
    {
        try
        {
            // Initialize Database first with explicit URL
            string databaseUrl = "https://actividad-5-sid-jacobo-default-rtdb.firebaseio.com";
            database = FirebaseDatabase.GetInstance(databaseUrl);
            
            // Then initialize Auth
            auth = FirebaseAuth.DefaultInstance;
            
            Debug.Log("Firebase initialized successfully.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"FirebaseManager: Failed to initialize Firebase: {ex.Message}");
        }
    }

    /// <summary>
    /// Registers a new user with email, password, and username.
    /// Saves username and highScore (0) to the database.
    /// </summary>
    public void RegisterUser(string email, string password, string username, Action onSuccess, Action<string> onFailure)
    {
        if (auth == null)
        {
            onFailure?.Invoke("Firebase Auth is not initialized.");
            return;
        }

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(username))
        {
            onFailure?.Invoke("Email, password, and username are required.");
            return;
        }

        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                string errorMessage = ExtractFirebaseErrorMessage(task.Exception);
                onFailure?.Invoke(errorMessage);
                return;
            }

            if (task.IsCompleted)
            {
                FirebaseUser newUser = task.Result.User;
                SaveUserDataToDatabase(newUser.UserId, username, onSuccess, onFailure);
            }
        });
    }

    /// <summary>
    /// Logs in a user with email and password.
    /// Loads username and highScore from the database on success.
    /// </summary>
    public void LoginUser(string email, string password, Action onSuccess, Action<string> onFailure)
    {
        if (auth == null)
        {
            onFailure?.Invoke("Firebase Auth is not initialized.");
            return;
        }

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            onFailure?.Invoke("Email and password are required.");
            return;
        }

        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                string errorMessage = ExtractFirebaseErrorMessage(task.Exception);
                onFailure?.Invoke(errorMessage);
                return;
            }

            if (task.IsCompleted)
            {
                FirebaseUser user = task.Result.User;
                LoadUserDataFromDatabase(user.UserId, onSuccess, onFailure);
            }
        });
    }

    /// <summary>
    /// Logs out the current user.
    /// </summary>
    public void Logout()
    {
        if (auth != null)
        {
            auth.SignOut();
            Username = null;
            HighScore = 0;
        }
    }

    /// <summary>
    /// Sends a password reset email to the specified email address.
    /// </summary>
    public void SendPasswordReset(string email, Action onSuccess, Action<string> onFailure)
    {
        if (auth == null)
        {
            onFailure?.Invoke("Firebase Auth is not initialized.");
            return;
        }

        if (string.IsNullOrEmpty(email))
        {
            onFailure?.Invoke("Email is required.");
            return;
        }

        auth.SendPasswordResetEmailAsync(email).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                string errorMessage = ExtractFirebaseErrorMessage(task.Exception);
                onFailure?.Invoke(errorMessage);
                return;
            }

            if (task.IsCompleted)
            {
                onSuccess?.Invoke();
            }
        });
    }

    private void SaveUserDataToDatabase(string userId, string username, Action onSuccess, Action<string> onFailure)
    {
        if (database == null)
        {
            onFailure?.Invoke("Firebase Database is not initialized.");
            return;
        }

        var userRef = database.GetReference($"users/{userId}");

        var userDict = new Dictionary<string, object>
        {
            { "username", username },
            { "highScore", 0 }
        };

        Debug.Log($"[Firebase] Attempting to save user {userId} with username {username}");

        userRef.SetValueAsync(userDict).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                string errorMessage = ExtractFirebaseErrorMessage(task.Exception);
                Debug.LogError($"[Firebase] Failed to save user data: {errorMessage}");
                onFailure?.Invoke(errorMessage);
                return;
            }

            if (task.IsCompleted)
            {
                Debug.Log($"[Firebase] Successfully saved user {userId}");
                Username = username;
                HighScore = 0;
                onSuccess?.Invoke();
            }
        });
    }

    private void LoadUserDataFromDatabase(string userId, Action onSuccess, Action<string> onFailure)
    {
        if (database == null)
        {
            onFailure?.Invoke("Firebase Database is not initialized.");
            return;
        }

        var userRef = database.GetReference($"users/{userId}");

        userRef.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                string errorMessage = ExtractFirebaseErrorMessage(task.Exception);
                onFailure?.Invoke(errorMessage);
                return;
            }

            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                if (snapshot.Exists)
                {
                    if (snapshot.Child("username").Exists)
                    {
                        Username = snapshot.Child("username").Value.ToString();
                    }

                    if (snapshot.Child("highScore").Exists)
                    {
                        if (long.TryParse(snapshot.Child("highScore").Value.ToString(), out long scoreValue))
                        {
                            HighScore = (int)scoreValue;
                        }
                    }

                    onSuccess?.Invoke();
                }
                else
                {
                    onFailure?.Invoke("User data not found in database.");
                }
            }
        });
    }

    /// <summary>
    /// Saves the score to the user's profile if it's higher than the stored high score.
    /// </summary>
    public void SaveScoreIfHigher(int newScore, Action onSuccess = null, Action<string> onFailure = null)
    {
        if (auth == null || database == null || auth.CurrentUser == null)
        {
            onFailure?.Invoke("User not authenticated.");
            return;
        }

        string userId = auth.CurrentUser.UserId;
        var userRef = database.GetReference($"users/{userId}/highScore");

        userRef.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                string errorMessage = ExtractFirebaseErrorMessage(task.Exception);
                onFailure?.Invoke(errorMessage);
                return;
            }

            if (task.IsCompleted)
            {
                int storedScore = 0;
                DataSnapshot snapshot = task.Result;

                if (snapshot.Exists && long.TryParse(snapshot.Value.ToString(), out long storedValue))
                {
                    storedScore = (int)storedValue;
                }

                if (newScore > storedScore)
                {
                    userRef.SetValueAsync(newScore).ContinueWithOnMainThread(updateTask =>
                    {
                        if (updateTask.IsCompleted)
                        {
                            HighScore = newScore;
                            onSuccess?.Invoke();
                        }
                        else if (updateTask.IsFaulted)
                        {
                            string errorMessage = ExtractFirebaseErrorMessage(updateTask.Exception);
                            onFailure?.Invoke(errorMessage);
                        }
                    });
                }
                else
                {
                    onSuccess?.Invoke();
                }
            }
        });
    }

    /// <summary>
    /// Saves the user's score to the leaderboard if it's higher than their existing leaderboard score.
    /// </summary>
    public void SaveScoreToLeaderboard(string username, int score, Action onSuccess = null, Action<string> onFailure = null)
    {
        if (auth == null || database == null || auth.CurrentUser == null)
        {
            onFailure?.Invoke("User not authenticated.");
            return;
        }

        string userId = auth.CurrentUser.UserId;
        var leaderboardRef = database.GetReference($"leaderboard/{userId}");

        leaderboardRef.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                string errorMessage = ExtractFirebaseErrorMessage(task.Exception);
                onFailure?.Invoke(errorMessage);
                return;
            }

            if (task.IsCompleted)
            {
                int existingScore = 0;
                DataSnapshot snapshot = task.Result;

                if (snapshot.Exists && snapshot.Child("score").Exists)
                {
                    if (long.TryParse(snapshot.Child("score").Value.ToString(), out long existingValue))
                    {
                        existingScore = (int)existingValue;
                    }
                }

                if (score > existingScore)
                {
                    var scoreData = new Dictionary<string, object>
                    {
                        { "username", username },
                        { "score", score }
                    };

                    leaderboardRef.SetValueAsync(scoreData).ContinueWithOnMainThread(updateTask =>
                    {
                        if (updateTask.IsCompleted)
                        {
                            onSuccess?.Invoke();
                        }
                        else if (updateTask.IsFaulted)
                        {
                            string errorMessage = ExtractFirebaseErrorMessage(updateTask.Exception);
                            onFailure?.Invoke(errorMessage);
                        }
                    });
                }
                else
                {
                    onSuccess?.Invoke();
                }
            }
        });
    }

    /// <summary>
    /// Loads the top 10 leaderboard scores, ordered by score descending.
    /// </summary>
    public void LoadLeaderboard(Action<List<LeaderboardEntry>> onLoaded, Action<string> onFailure = null)
    {
        if (database == null)
        {
            onFailure?.Invoke("Firebase Database is not initialized.");
            return;
        }

        var leaderboardRef = database.GetReference("leaderboard");

        leaderboardRef.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                string errorMessage = ExtractFirebaseErrorMessage(task.Exception);
                onFailure?.Invoke(errorMessage);
                return;
            }

            if (task.IsCompleted)
            {
                var entries = new List<LeaderboardEntry>();
                DataSnapshot snapshot = task.Result;

                if (snapshot.Exists)
                {
                    foreach (var childSnapshot in snapshot.Children)
                    {
                        if (childSnapshot.Child("username").Exists && childSnapshot.Child("score").Exists)
                        {
                            string username = childSnapshot.Child("username").Value.ToString();

                            if (int.TryParse(childSnapshot.Child("score").Value.ToString(), out int score))
                            {
                                entries.Add(new LeaderboardEntry { Username = username, Score = score });
                            }
                        }
                    }
                }

                entries.Sort((a, b) => b.Score.CompareTo(a.Score));

                if (entries.Count > 10)
                {
                    entries = entries.GetRange(0, 10);
                }

                onLoaded?.Invoke(entries);
            }
        });
    }

    private string ExtractFirebaseErrorMessage(AggregateException ex)
    {
        if (ex == null)
        {
            return "An unknown error occurred.";
        }

        foreach (var innerEx in ex.InnerExceptions)
        {
            if (innerEx is FirebaseException firebaseEx)
            {
                return firebaseEx.Message;
            }
        }

        return ex.Message;
    }
}

/// <summary>
/// Data class for leaderboard entries.
/// </summary>
public class LeaderboardEntry
{
    public string Username { get; set; }
    public int Score { get; set; }
}
