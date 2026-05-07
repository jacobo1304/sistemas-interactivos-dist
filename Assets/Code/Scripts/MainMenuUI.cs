using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button leaderboardButton;
    [SerializeField] private Button logoutButton;
    [SerializeField] private TextMeshProUGUI usernameText;

    private void Start()
    {
        if (playButton != null)
        {
            playButton.onClick.AddListener(OnPlayClicked);
        }

        if (leaderboardButton != null)
        {
            leaderboardButton.onClick.AddListener(OnLeaderboardClicked);
        }

        if (logoutButton != null)
        {
            logoutButton.onClick.AddListener(OnLogoutClicked);
        }

        // Display username
        if (usernameText != null && FirebaseManager.Instance != null)
        {
            usernameText.text = $"Welcome, {FirebaseManager.Instance.Username}!";
        }
    }

    private void OnPlayClicked()
    {
        SceneManager.LoadScene("Game");
    }

    private void OnLeaderboardClicked()
    {
        SceneManager.LoadScene("Leaderboard");
    }

    private void OnLogoutClicked()
    {
        if (FirebaseManager.Instance != null)
        {
            FirebaseManager.Instance.Logout();
        }

        SceneManager.LoadScene("Login");
    }

    private void OnDestroy()
    {
        if (playButton != null)
        {
            playButton.onClick.RemoveListener(OnPlayClicked);
        }

        if (leaderboardButton != null)
        {
            leaderboardButton.onClick.RemoveListener(OnLeaderboardClicked);
        }

        if (logoutButton != null)
        {
            logoutButton.onClick.RemoveListener(OnLogoutClicked);
        }
    }
}
