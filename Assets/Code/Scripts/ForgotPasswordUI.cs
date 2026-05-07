using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ForgotPasswordUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField emailInput;
    [SerializeField] private Button sendButton;
    [SerializeField] private Button backButton;
    [SerializeField] private TextMeshProUGUI confirmationText;
    [SerializeField] private TextMeshProUGUI errorText;

    private void Start()
    {
        if (sendButton != null)
        {
            sendButton.onClick.AddListener(OnSendClicked);
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
            backButton.gameObject.SetActive(false);
        }

        if (confirmationText != null)
        {
            confirmationText.text = string.Empty;
        }

        if (errorText != null)
        {
            errorText.text = string.Empty;
        }
    }

    private void OnSendClicked()
    {
        string email = emailInput != null ? emailInput.text : string.Empty;

        if (string.IsNullOrEmpty(email))
        {
            ShowError("Email is required.");
            return;
        }

        if (FirebaseManager.Instance == null)
        {
            ShowError("FirebaseManager is not initialized.");
            return;
        }

        FirebaseManager.Instance.SendPasswordReset(email,
            onSuccess: () =>
            {
                ShowError(string.Empty);
                ShowConfirmation("Correo enviado");
                Debug.Log("Password reset email sent to " + email);
                
                if (sendButton != null)
                {
                    sendButton.gameObject.SetActive(false);
                }
                
                if (backButton != null)
                {
                    backButton.gameObject.SetActive(true);
                }
            },
            onFailure: (error) =>
            {
                ShowConfirmation(string.Empty);
                ShowError(error);
            }
        );
    }

    private void OnBackClicked()
    {
        SceneManager.LoadScene("Login");
    }

    private void ShowConfirmation(string message)
    {
        if (confirmationText != null)
        {
            confirmationText.text = message;
        }
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
        if (sendButton != null)
        {
            sendButton.onClick.RemoveListener(OnSendClicked);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(OnBackClicked);
        }
    }
}
