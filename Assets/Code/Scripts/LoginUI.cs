using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoginUI : MonoBehaviour
{
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject mainMenuPanel;

    [SerializeField] private TMP_InputField emailInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private Button loginButton;
    [SerializeField] private Button registerButton;
    [SerializeField] private Button forgotPasswordButton;
    [SerializeField] private TextMeshProUGUI errorText;

    private void Start()
    {
        SetupLoginForm();
        SetupMainMenu();

        // Check if there's an active session
        if (FirebaseManager.Instance != null && FirebaseManager.Instance.IsLoggedIn)
        {
            ShowMainMenu();
        }
        else
        {
            ShowLoginForm();
        }
    }

    private void SetupLoginForm()
    {
        if (loginButton != null)
        {
            loginButton.onClick.AddListener(OnLoginClicked);
        }

        if (registerButton != null)
        {
            registerButton.onClick.AddListener(OnRegisterClicked);
        }

        if (forgotPasswordButton != null)
        {
            forgotPasswordButton.onClick.AddListener(OnForgotPasswordClicked);
        }

        if (errorText != null)
        {
            errorText.text = string.Empty;
        }
    }

    private void SetupMainMenu()
    {
        // Main menu buttons will be set up by MainMenuUI component
    }

    private void ShowLoginForm()
    {
        if (loginPanel != null)
        {
            loginPanel.SetActive(true);
        }

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false);
        }
    }

    private void ShowMainMenu()
    {
        if (loginPanel != null)
        {
            loginPanel.SetActive(false);
        }

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }
    }

    private void OnLoginClicked()
    {
        string email = emailInput != null ? emailInput.text : string.Empty;
        string password = passwordInput != null ? passwordInput.text : string.Empty;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ShowLoginError("Email and password are required.");
            return;
        }

        if (FirebaseManager.Instance == null)
        {
            ShowLoginError("FirebaseManager is not initialized.");
            return;
        }

        FirebaseManager.Instance.LoginUser(email, password,
            onSuccess: () =>
            {
                ShowLoginError(string.Empty);
                Debug.Log($"Login successful. Welcome, {FirebaseManager.Instance.Username}!");
                ShowMainMenu();
            },
            onFailure: (error) =>
            {
                ShowLoginError(error);
            }
        );
    }

    private void OnRegisterClicked()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Register");
    }

    private void OnForgotPasswordClicked()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("ForgotPassword");
    }

    private void ShowLoginError(string message)
    {
        if (errorText != null)
        {
            errorText.text = message;
        }
    }

    private void OnDestroy()
    {
        if (loginButton != null)
        {
            loginButton.onClick.RemoveListener(OnLoginClicked);
        }

        if (registerButton != null)
        {
            registerButton.onClick.RemoveListener(OnRegisterClicked);
        }

        if (forgotPasswordButton != null)
        {
            forgotPasswordButton.onClick.RemoveListener(OnForgotPasswordClicked);
        }
    }
}
