using UnityEngine;
using TMPro;

public class ScoreboardManager : MonoBehaviour
{

    public static ScoreboardManager Instance;

    public TextMeshProUGUI scoreTextDisplay;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void RefreshScoreboard()
    {
        PlayerScore[] allPlayers = FindObjectsByType<PlayerScore>(FindObjectsSortMode.None);
        string boardText = "--- Current Scores ---\n";

        foreach (PlayerScore player in allPlayers)
        {
            boardText += $"Player {player.OwnerClientId} : {player.currentScore.Value} Pts\n";
        }

        // 4. เอาข้อความไปแสดงบนจอ
        if (scoreTextDisplay != null)
        {
            scoreTextDisplay.text = boardText;
        }
    }
}