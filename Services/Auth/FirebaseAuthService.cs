using System;
using System.Threading.Tasks;
using UnityEngine;
using AnimalFall.Data.Schemas;

#if FIREBASE_AUTH
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
#endif

namespace AnimalFall.Services.Auth
{
    public class FirebaseAuthService : MonoBehaviour
    {
        public static FirebaseAuthService Instance { get; private set; }

        public event Action<UserProfile> OnLoginSuccess;
        public event Action<string> OnLoginFailed;
        public event Action OnLogout;

        public bool IsInitialized { get; private set; }
        public bool IsLoggedIn { get; private set; }
        public UserProfile CurrentUser { get; private set; }

#if FIREBASE_AUTH
        private FirebaseAuth auth;
        private FirebaseFirestore db;
#endif

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            InitializeFirebase();
        }

        private async void InitializeFirebase()
        {
#if FIREBASE_AUTH
            try
            {
                var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
                if (dependencyStatus == DependencyStatus.Available)
                {
                    auth = FirebaseAuth.DefaultInstance;
                    db = FirebaseFirestore.DefaultInstance;
                    IsInitialized = true;

                    auth.StateChanged += OnAuthStateChanged;

                    if (auth.CurrentUser != null)
                    {
                        await LoadUserProfile(auth.CurrentUser.UserId);
                        IsLoggedIn = true;
                    }

                    Debug.Log("[FirebaseAuth] Initialized successfully.");
                }
                else
                {
                    Debug.LogError($"[FirebaseAuth] Could not resolve dependencies: {dependencyStatus}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseAuth] Init error: {e.Message}");
            }
#else
            await Task.CompletedTask;
            IsInitialized = true;
            Debug.Log("[FirebaseAuth] Running in offline mode (FIREBASE_AUTH not defined).");
#endif
        }

        public async Task<bool> RegisterWithEmail(string email, string password, string displayName)
        {
#if FIREBASE_AUTH
            try
            {
                var result = await auth.CreateUserWithEmailAndPasswordAsync(email, password);
                var user = result.User;

                var profile = new UserProfileChange { DisplayName = displayName };
                await user.UpdateUserProfileAsync(profile);

                var userProfile = new UserProfile(user.UserId, displayName, email);
                await SaveUserProfile(userProfile);
                CurrentUser = userProfile;
                IsLoggedIn = true;
                OnLoginSuccess?.Invoke(userProfile);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseAuth] Register error: {e.Message}");
                OnLoginFailed?.Invoke(GetFriendlyErrorMessage(e));
                return false;
            }
#else
            await Task.CompletedTask;
            CurrentUser = new UserProfile("offline_user", displayName, email);
            IsLoggedIn = true;
            OnLoginSuccess?.Invoke(CurrentUser);
            return true;
#endif
        }

        public async Task<bool> LoginWithEmail(string email, string password)
        {
#if FIREBASE_AUTH
            try
            {
                var result = await auth.SignInWithEmailAndPasswordAsync(email, password);
                await LoadUserProfile(result.User.UserId);
                IsLoggedIn = true;
                OnLoginSuccess?.Invoke(CurrentUser);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseAuth] Login error: {e.Message}");
                OnLoginFailed?.Invoke(GetFriendlyErrorMessage(e));
                return false;
            }
#else
            await Task.CompletedTask;
            CurrentUser = new UserProfile("offline_user", "Player", email);
            IsLoggedIn = true;
            OnLoginSuccess?.Invoke(CurrentUser);
            return true;
#endif
        }

        public async Task<bool> LoginWithGoogle()
        {
#if FIREBASE_AUTH && GOOGLE_SIGN_IN
            try
            {
                var googleUser = await GoogleSignIn.DefaultInstance.SignIn();
                var credential = GoogleAuthProvider.GetCredential(googleUser.IdToken, null);
                var result = await auth.SignInWithCredentialAsync(credential);

                await LoadUserProfile(result.User.UserId);

                if (CurrentUser == null)
                {
                    var userProfile = new UserProfile(
                        result.User.UserId,
                        result.User.DisplayName ?? "Player",
                        result.User.Email ?? ""
                    );
                    await SaveUserProfile(userProfile);
                    CurrentUser = userProfile;
                }

                IsLoggedIn = true;
                OnLoginSuccess?.Invoke(CurrentUser);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseAuth] Google login error: {e.Message}");
                OnLoginFailed?.Invoke("Google sign-in failed. Please try again.");
                return false;
            }
#else
            await Task.CompletedTask;
            CurrentUser = new UserProfile("google_offline", "Google Player", "google@test.com");
            IsLoggedIn = true;
            OnLoginSuccess?.Invoke(CurrentUser);
            return true;
#endif
        }

        public async Task<bool> ResetPassword(string email)
        {
#if FIREBASE_AUTH
            try
            {
                await auth.SendPasswordResetEmailAsync(email);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseAuth] Password reset error: {e.Message}");
                return false;
            }
#else
            await Task.CompletedTask;
            Debug.Log("[FirebaseAuth] Password reset simulated for: " + email);
            return true;
#endif
        }

        public void Logout()
        {
#if FIREBASE_AUTH
            auth?.SignOut();
#endif
            CurrentUser = null;
            IsLoggedIn = false;
            OnLogout?.Invoke();
        }

        public async Task SavePlayerProgress(PlayerProgress progress)
        {
#if FIREBASE_AUTH
            if (!IsLoggedIn || auth.CurrentUser == null) return;
            try
            {
                var docRef = db.Collection("users")
                              .Document(auth.CurrentUser.UserId)
                              .Collection("progress")
                              .Document("data");
                await docRef.SetAsync(progress.ToDictionary());
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseAuth] Save progress error: {e.Message}");
            }
#else
            await Task.CompletedTask;
#endif
        }

        public async Task UpdateLeaderboard(int score, int level)
        {
#if FIREBASE_AUTH
            if (!IsLoggedIn || CurrentUser == null) return;
            try
            {
                var entry = new LeaderboardEntry(
                    CurrentUser.uid,
                    CurrentUser.displayName,
                    score,
                    level
                );
                var docRef = db.Collection("leaderboard").Document(CurrentUser.uid);
                await docRef.SetAsync(entry.ToDictionary());
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseAuth] Leaderboard update error: {e.Message}");
            }
#else
            await Task.CompletedTask;
#endif
        }

#if FIREBASE_AUTH
        private async Task LoadUserProfile(string uid)
        {
            try
            {
                var docRef = db.Collection("users").Document(uid);
                var snapshot = await docRef.GetSnapshotAsync();
                if (snapshot.Exists)
                {
                    CurrentUser = snapshot.ConvertTo<UserProfile>();
                    CurrentUser.lastLoginAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    await docRef.UpdateAsync("lastLoginAt", CurrentUser.lastLoginAt);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseAuth] Load profile error: {e.Message}");
            }
        }

        private async Task SaveUserProfile(UserProfile profile)
        {
            try
            {
                var docRef = db.Collection("users").Document(profile.uid);
                await docRef.SetAsync(profile.ToDictionary());
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseAuth] Save profile error: {e.Message}");
            }
        }

        private void OnAuthStateChanged(object sender, EventArgs e)
        {
            if (auth.CurrentUser == null && IsLoggedIn)
            {
                IsLoggedIn = false;
                CurrentUser = null;
                OnLogout?.Invoke();
            }
        }

        private string GetFriendlyErrorMessage(Exception e)
        {
            string msg = e.Message.ToLower();
            if (msg.Contains("email-already-in-use"))
                return "This email is already registered. Try logging in instead.";
            if (msg.Contains("wrong-password") || msg.Contains("invalid-credential"))
                return "Incorrect password. Please try again.";
            if (msg.Contains("user-not-found"))
                return "No account found with this email.";
            if (msg.Contains("weak-password"))
                return "Password is too weak. Use at least 6 characters.";
            if (msg.Contains("invalid-email"))
                return "Please enter a valid email address.";
            if (msg.Contains("too-many-requests"))
                return "Too many attempts. Please try again later.";
            return "Authentication failed. Please try again.";
        }
#else
        private async Task LoadUserProfile(string uid)
        {
            await Task.CompletedTask;
        }
#endif
    }
}
