using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LeaderboardUI : MonoBehaviour
{
    [SerializeField] private Transform leaderboardContainer;
    [SerializeField] private GameObject leaderboardRowPrefab;
    [SerializeField] private TextMeshProUGUI loadingText;
    [SerializeField] private Button backButton;

    private readonly List<GameObject> instantiatedRows = new List<GameObject>();

    private void Start()
    {
        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
        }

        LoadAndDisplayLeaderboard();
    }

    private void LoadAndDisplayLeaderboard()
    {
        if (loadingText != null)
        {
            loadingText.text = "Cargando...";
        }

        if (FirebaseManager.Instance == null)
        {
            ShowErrorMessage("FirebaseManager not initialized.");
            return;
        }

        FirebaseManager.Instance.LoadLeaderboard(
            onLoaded: (entries) => DisplayLeaderboard(entries),
            onFailure: (error) => ShowErrorMessage($"Error: {error}")
        );
    }

    private void DisplayLeaderboard(List<LeaderboardEntry> entries)
    {
        // Clear previous rows
        foreach (GameObject row in instantiatedRows)
        {
            Destroy(row);
        }
        instantiatedRows.Clear();

        if (loadingText != null)
        {
            loadingText.text = string.Empty;
        }

        if (entries == null || entries.Count == 0)
        {
            if (loadingText != null)
            {
                loadingText.text = "Sin datos";
            }
            return;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            LeaderboardEntry entry = entries[i];
            GameObject rowInstance = null;

            if (leaderboardRowPrefab != null && leaderboardContainer != null)
            {
                rowInstance = Instantiate(leaderboardRowPrefab, leaderboardContainer);
                LeaderboardRow rowComponent = rowInstance.GetComponent<LeaderboardRow>();

                if (rowComponent != null)
                {
                    rowComponent.SetData(i + 1, entry.Username, entry.Score);
                }
            }
            else
            {
                // Fallback: create a simple text display
                if (leaderboardContainer != null)
                {
                    GameObject textObj = new GameObject($"Entry_{i + 1}");
                    textObj.transform.SetParent(leaderboardContainer, false);

                    TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
                    tmpText.text = $"#{i + 1} - {entry.Username}: {entry.Score}";
                    tmpText.fontSize = 36;

                    rowInstance = textObj;
                }
            }

            if (rowInstance != null)
            {
                instantiatedRows.Add(rowInstance);
            }
        }
    }

    private void ShowErrorMessage(string message)
    {
        if (loadingText != null)
        {
            loadingText.text = message;
        }
    }

    private void OnBackClicked()
    {
        SceneManager.LoadScene("Login");
    }

    private void OnDestroy()
    {
        if (backButton != null)
        {
            backButton.onClick.RemoveListener(OnBackClicked);
        }
    }
}
