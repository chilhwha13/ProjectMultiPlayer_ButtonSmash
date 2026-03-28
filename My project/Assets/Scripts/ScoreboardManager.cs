using UnityEngine;
using TMPro;

public class ScoreboardManager : MonoBehaviour
{
    // สร้าง Singleton เพื่อให้สคริปต์อื่นเรียกใช้ตัวนี้ได้ง่ายๆ
    public static ScoreboardManager Instance;

    public TextMeshProUGUI scoreTextDisplay;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void RefreshScoreboard()
    {
        // 1. ค้นหา PlayerPrefab ทั้งหมดที่อยู่ใน Scene
        PlayerScore[] allPlayers = FindObjectsByType<PlayerScore>(FindObjectsSortMode.None);

        // 2. เปลี่ยนหัวข้อเป็นภาษาอังกฤษ
        string boardText = "--- Current Scores ---\n";

        // 3. เปลี่ยนข้อความแสดงคะแนนเป็นภาษาอังกฤษ
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