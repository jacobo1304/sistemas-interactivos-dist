using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class RegisterUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField emailInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private Button registerButton;
    [SerializeField] private Button backButton;
    [SerializeField] private TextMeshProUGUI errorText;

    private void Start()
    {
        if (registerButton != null)
        {
            registerButton.onClick.AddListener(OnRegisterClicked);
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
        }

        if (errorText != null)
        {
            errorText.text = string.Empty;
        }
    }

    private void OnRegisterClicked()
    {
        string email = emailInput != null ? emailInput.text : string.Empty;
        string password = passwordInput != null ? passwordInput.text : string.Empty;
        string username = usernameInput != null ? usernameInput.text : string.Empty;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(username))
        {
            ShowError("Email, password, and username are required.");
            return;
        }

        if (password.Length < 6)
        {
            ShowError("Password must be at least 6 characters.");
            return;
        }

        if (FirebaseManager.Instance == null)
        {
            ShowError("FirebaseManager is not initialized.");
            return;
        }

        FirebaseManager.Instance.RegisterUser(email, password, username,
            onSuccess: () =>
            {
                ShowError(string.Empty);
                Debug.Log("Registration successful!");
                SceneManager.LoadScene("Login");
            },
            onFailure: (error) =>
            {
                ShowError(error);
            }
        );
    }

    private void OnBackClicked()
    {
        SceneManager.LoadScene("Login");
    }

    private void ShowError(string message)
    {
        if (errorText != null)
        {
            errorText.text = message;
        }
    }

    private void OnDestroy()
    {
        if (registerButton != null)
        {
            registerButton.onClick.RemoveListener(OnRegisterClicked);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(OnBackClicked);
        }
    }
}
