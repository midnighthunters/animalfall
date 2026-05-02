// ============================================================
//  FirebaseManager.cs  –  Animal Fall
//  Handles:
//    • Anonymous + Google sign-in (graceful fallback)
//    • Player profile schema (FirebaseUserProfile)
//    • RTDB save-data sync
//    • Mock mode when Firebase SDK is absent (Editor / CI)
// ============================================================

using System;
using System.Collections;
using UnityEngine;

// ── User profile written to RTDB ──────────────────────────────
[Serializable]
public class FirebaseUserProfile
{
    public string uid          = "";
    public string displayName  = "Player";
    public string email        = "";
    public int    coins        = 0;
    public int    gems         = 0;
    public int    highestLevel = 0;
    public string lastSeen     = "";
    public string platform     = "";
    public string appVersion   = "";
}

// ── Mock leaderboard entry ────────────────────────────────────
[Serializable]
public class LeaderboardEntry
{
    public string uid;
    public string displayName;
    public int    score;
    public int    level;
}

// ── Manager ───────────────────────────────────────────────────
public class FirebaseManager : MonoBehaviour
{
    // ── Singleton ──────────────────────────────────────────────
    public static FirebaseManager Instance { get; private set; }

    // ── State ──────────────────────────────────────────────────
    public bool  IsSignedIn  { get; private set; }
    public string UserId     { get; private set; } = "";
    public string DisplayName{ get; private set; } = "Player";

    /// <summary>True while we are not yet connected (checking auth).</summary>
    public bool  IsInitializing { get; private set; } = true;

    // ── Mock data injected at design time or via tests ─────────
    [Header("Mock / Offline Data")]
    [SerializeField] private bool forceMockMode = false;   // tick in Editor to skip real Firebase
    [SerializeField] private string mockUserId      = "mock_uid_001";
    [SerializeField] private string mockDisplayName = "TestPlayer";
    [SerializeField] private int    mockCoins       = 500;
    [SerializeField] private int    mockGems        = 10;
    [SerializeField] private int    mockHighestLevel= 3;

    // ── Lifecycle ──────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        StartCoroutine(InitializeAsync());
    }

    // ── Init ──────────────────────────────────────────────────
    private IEnumerator InitializeAsync()
    {
#if FIREBASE_ENABLED
        // ── Real Firebase path ─────────────────────────────────
        var dependencyTask = Firebase.FirebaseApp.CheckAndFixDependenciesAsync();
        yield return new WaitUntil(() => dependencyTask.IsCompleted);

        if (dependencyTask.Result == Firebase.DependencyStatus.Available)
        {
            Firebase.Auth.FirebaseAuth auth = Firebase.Auth.FirebaseAuth.DefaultInstance;
            auth.StateChanged += OnFirebaseAuthStateChanged;

            // Sign in anonymously if no current user
            if (auth.CurrentUser == null)
            {
                var signInTask = auth.SignInAnonymouslyAsync();
                yield return new WaitUntil(() => signInTask.IsCompleted);

                if (signInTask.Exception != null)
                    Debug.LogWarning($"[Firebase] Anon sign-in failed: {signInTask.Exception.Flatten().Message}");
            }
            else
            {
                HandleSignedIn(auth.CurrentUser.UserId, auth.CurrentUser.DisplayName ?? "Player");
            }
        }
        else
        {
            Debug.LogError($"[Firebase] Dependency check failed: {dependencyTask.Result}");
            FallbackToMock();
        }
#else
        // ── Mock path ──────────────────────────────────────────
        yield return new WaitForSeconds(0.5f);   // simulate latency
        FallbackToMock();
#endif
        IsInitializing = false;
    }

    // ── Auth callbacks ────────────────────────────────────────
#if FIREBASE_ENABLED
    private void OnFirebaseAuthStateChanged(object sender, EventArgs e)
    {
        var auth = Firebase.Auth.FirebaseAuth.DefaultInstance;
        if (auth.CurrentUser != null)
            HandleSignedIn(auth.CurrentUser.UserId, auth.CurrentUser.DisplayName ?? "Player");
        else
            HandleSignedOut();
    }
#endif

    private void HandleSignedIn(string uid, string name)
    {
        UserId      = uid;
        DisplayName = string.IsNullOrEmpty(name) ? "Player" : name;
        IsSignedIn  = true;
        Debug.Log($"[Firebase] Signed in as {DisplayName} ({uid})");
        EventBus.Publish(new OnFirebaseAuthReady { isSignedIn = true, userId = uid });

        // Pull cloud save (merge with local)
        PullSaveData();
    }

    private void HandleSignedOut()
    {
        IsSignedIn = false;
        Debug.Log("[Firebase] Signed out.");
        EventBus.Publish(new OnFirebaseAuthReady { isSignedIn = false, userId = "" });
    }

    private void FallbackToMock()
    {
        if (forceMockMode || Application.isEditor)
        {
            InjectMockData();
        }
        else
        {
            // Offline guest – still playable, no cloud sync
            Debug.Log("[Firebase] Running in offline/guest mode.");
            EventBus.Publish(new OnFirebaseAuthReady { isSignedIn = false, userId = "" });
        }
    }

    // ── Mock Data injection ───────────────────────────────────
    private void InjectMockData()
    {
        UserId      = mockUserId;
        DisplayName = mockDisplayName;
        IsSignedIn  = true;

        // Seed the local save with mock values if this is a fresh run
        if (SaveManager.Instance != null)
        {
            var data = SaveManager.Instance.Data;
            if (data.coins == 0)        data.coins        = mockCoins;
            if (data.gems  == 0)        data.gems         = mockGems;
            if (data.highestUnlockedLevel == 0)
                data.highestUnlockedLevel = mockHighestLevel;
            data.playerId    = mockUserId;
            data.displayName = mockDisplayName;
        }

        Debug.Log($"[Firebase] Mock mode: signed in as {mockDisplayName} ({mockUserId})");
        EventBus.Publish(new OnFirebaseAuthReady { isSignedIn = true, userId = mockUserId });
    }

    // ── RTDB Push / Pull ──────────────────────────────────────
    public void PushSaveData(PlayerSaveData saveData)
    {
        if (!IsSignedIn || string.IsNullOrEmpty(UserId)) return;

#if FIREBASE_ENABLED
        var db   = Firebase.Database.FirebaseDatabase.DefaultInstance;
        var path = $"users/{UserId}/save";
        string json = JsonUtility.ToJson(saveData);
        db.GetReference(path).SetRawJsonValueAsync(json).ContinueWith(task =>
        {
            if (task.Exception != null)
                Debug.LogWarning($"[Firebase] PushSaveData failed: {task.Exception.Flatten().Message}");
        });
#else
        Debug.Log($"[Firebase] Mock push for user {UserId}");
#endif
    }

    public void PullSaveData()
    {
        if (!IsSignedIn || string.IsNullOrEmpty(UserId)) return;

#if FIREBASE_ENABLED
        var db   = Firebase.Database.FirebaseDatabase.DefaultInstance;
        var path = $"users/{UserId}/save";
        db.GetReference(path).GetValueAsync().ContinueWith(task =>
        {
            if (task.Exception != null) { Debug.LogWarning("[Firebase] PullSaveData failed."); return; }
            string json = task.Result.GetRawJsonValue();
            if (!string.IsNullOrEmpty(json))
            {
                // Merge: keep higher values (cloud wins for coins/progress, local wins for settings)
                PlayerSaveData cloud = JsonUtility.FromJson<PlayerSaveData>(json);
                MergeWithLocal(cloud);
            }
        });
#else
        Debug.Log("[Firebase] Mock pull – no cloud data.");
#endif
    }

    private void MergeWithLocal(PlayerSaveData cloud)
    {
        if (SaveManager.Instance == null) return;
        var local = SaveManager.Instance.Data;

        // Take max of coins/progress
        local.coins                = Mathf.Max(local.coins,  cloud.coins);
        local.gems                 = Mathf.Max(local.gems,   cloud.gems);
        local.highestUnlockedLevel = Mathf.Max(local.highestUnlockedLevel, cloud.highestUnlockedLevel);
        local.playerId             = UserId;
        local.displayName          = DisplayName;

        SaveManager.Instance.Save();
        Debug.Log("[Firebase] Cloud save merged into local.");
    }

    // ── Leaderboard helpers ──────────────────────────────────
    /// <summary>Post a level score to the RTDB leaderboard node.</summary>
    public void PostScore(int levelIndex, int score)
    {
        if (!IsSignedIn) return;

#if FIREBASE_ENABLED
        var entry = new LeaderboardEntry
        {
            uid         = UserId,
            displayName = DisplayName,
            score       = score,
            level       = levelIndex
        };
        string path = $"leaderboard/level_{levelIndex}/{UserId}";
        Firebase.Database.FirebaseDatabase.DefaultInstance
            .GetReference(path)
            .SetRawJsonValueAsync(JsonUtility.ToJson(entry));
#else
        Debug.Log($"[Firebase] Mock leaderboard post: level={levelIndex} score={score}");
#endif
    }
}
