using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace AnimalFall.Services.Auth
{
    public class AuthUIController : MonoBehaviour
    {
        [Header("Login Panel")]
        [SerializeField] private GameObject loginPanel;
        [SerializeField] private TMP_InputField loginEmailField;
        [SerializeField] private TMP_InputField loginPasswordField;
        [SerializeField] private Button loginButton;
        [SerializeField] private Button switchToRegisterButton;
        [SerializeField] private Button forgotPasswordButton;
        [SerializeField] private Button googleSignInButton;

        [Header("Register Panel")]
        [SerializeField] private GameObject registerPanel;
        [SerializeField] private TMP_InputField registerNameField;
        [SerializeField] private TMP_InputField registerEmailField;
        [SerializeField] private TMP_InputField registerPasswordField;
        [SerializeField] private TMP_InputField registerConfirmPasswordField;
        [SerializeField] private Button registerButton;
        [SerializeField] private Button switchToLoginButton;

        [Header("Feedback")]
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private GameObject loadingSpinner;

        [Header("Settings")]
        [SerializeField] private string mainSceneName = "MainScene";

        private void Start()
        {
            ShowLoginPanel();
            SetupButtonListeners();

            if (FirebaseAuthService.Instance != null)
            {
                FirebaseAuthService.Instance.OnLoginSuccess += OnLoginSuccess;
                FirebaseAuthService.Instance.OnLoginFailed += OnLoginFailed;

                if (FirebaseAuthService.Instance.IsLoggedIn)
                    SceneManager.LoadScene(mainSceneName);
            }
        }

        private void OnDestroy()
        {
            if (FirebaseAuthService.Instance != null)
            {
                FirebaseAuthService.Instance.OnLoginSuccess -= OnLoginSuccess;
                FirebaseAuthService.Instance.OnLoginFailed -= OnLoginFailed;
            }
        }

        private void SetupButtonListeners()
        {
            loginButton.onClick.AddListener(OnLoginClicked);
            registerButton.onClick.AddListener(OnRegisterClicked);
            switchToRegisterButton.onClick.AddListener(ShowRegisterPanel);
            switchToLoginButton.onClick.AddListener(ShowLoginPanel);
            forgotPasswordButton.onClick.AddListener(OnForgotPasswordClicked);
            googleSignInButton.onClick.AddListener(OnGoogleSignInClicked);
        }

        private void ShowLoginPanel()
        {
            loginPanel.SetActive(true);
            registerPanel.SetActive(false);
            ClearStatus();
        }

        private void ShowRegisterPanel()
        {
            loginPanel.SetActive(false);
            registerPanel.SetActive(true);
            ClearStatus();
        }

        private async void OnLoginClicked()
        {
            string email = loginEmailField.text.Trim();
            string password = loginPasswordField.text;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ShowStatus("Please fill in all fields.", true);
                return;
            }

            SetLoading(true);
            await FirebaseAuthService.Instance.LoginWithEmail(email, password);
            SetLoading(false);
        }

        private async void OnRegisterClicked()
        {
            string name = registerNameField.text.Trim();
            string email = registerEmailField.text.Trim();
            string password = registerPasswordField.text;
            string confirmPassword = registerConfirmPasswordField.text;

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email) ||
                string.IsNullOrEmpty(password))
            {
                ShowStatus("Please fill in all fields.", true);
                return;
            }

            if (password != confirmPassword)
            {
                ShowStatus("Passwords do not match.", true);
                return;
            }

            if (password.Length < 6)
            {
                ShowStatus("Password must be at least 6 characters.", true);
                return;
            }

            SetLoading(true);
            await FirebaseAuthService.Instance.RegisterWithEmail(email, password, name);
            SetLoading(false);
        }

        private async void OnForgotPasswordClicked()
        {
            string email = loginEmailField.text.Trim();
            if (string.IsNullOrEmpty(email))
            {
                ShowStatus("Enter your email first.", true);
                return;
            }

            SetLoading(true);
            bool sent = await FirebaseAuthService.Instance.ResetPassword(email);
            SetLoading(false);

            ShowStatus(sent ? "Password reset email sent!" : "Failed to send reset email.", !sent);
        }

        private async void OnGoogleSignInClicked()
        {
            SetLoading(true);
            await FirebaseAuthService.Instance.LoginWithGoogle();
            SetLoading(false);
        }

        private void OnLoginSuccess(Data.Schemas.UserProfile user)
        {
            ShowStatus($"Welcome, {user.displayName}!", false);
            SceneManager.LoadScene(mainSceneName);
        }

        private void OnLoginFailed(string error)
        {
            ShowStatus(error, true);
        }

        private void ShowStatus(string message, bool isError)
        {
            if (statusText == null) return;
            statusText.text = message;
            statusText.color = isError ? Color.red : Color.green;
        }

        private void ClearStatus()
        {
            if (statusText != null) statusText.text = "";
        }

        private void SetLoading(bool loading)
        {
            if (loadingSpinner != null) loadingSpinner.SetActive(loading);
            loginButton.interactable = !loading;
            registerButton.interactable = !loading;
        }
    }
}
