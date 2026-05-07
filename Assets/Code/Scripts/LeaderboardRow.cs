using TMPro;
using UnityEngine;

public class LeaderboardRow : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private TextMeshProUGUI usernameText;
    [SerializeField] private TextMeshProUGUI scoreText;

    public void SetData(int rank, string username, int score)
    {
        if (rankText != null)
        {
            rankText.text = $"#{rank}";
        }

        if (usernameText != null)
        {
            usernameText.text = username;
        }

        if (scoreText != null)
        {
            scoreText.text = score.ToString();
        }
    }
}
