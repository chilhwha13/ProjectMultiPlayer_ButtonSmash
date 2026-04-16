using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class ScoreboardManager : MonoBehaviour
{
    public static ScoreboardManager Instance;

    [Header("In-Game Scoreboard")]
    public TextMeshProUGUI scoreTextDisplay;

    [Header("Waiting Room Scroll View")]
    public GameObject playerSlotPrefab;    // ลาก Prefab PlayerNameSlot มาใส่ช่องนี้
    public Transform waitingRoomContent;   // ลากตัว Content ใน Scroll View มาใส่ช่องนี้

    private List<PlayerScore> players = new List<PlayerScore>();

    private void Awake()
    {
        Instance = this;
    }

    public void RegisterPlayer(PlayerScore player)
    {
        if (!players.Contains(player))
        {
            players.Add(player);
        }
        RefreshScoreboard();
    }

    public void RefreshScoreboard()
    {
        // 1. อัปเดตกระดานคะแนนตอนเล่นเกม (แบบเดิม)
        string board = "--- Current Scores ---\n";
        foreach (var player in players)
        {
            string pName = string.IsNullOrEmpty(player.playerName.Value.ToString()) ? $"Player {player.OwnerClientId}" : player.playerName.Value.ToString();
            board += $"{pName} : {player.currentScore.Value} Pts\n";
        }
        if (scoreTextDisplay != null) scoreTextDisplay.text = board;

        // 2. อัปเดต Scroll View ในหน้า Waiting Room
        UpdateWaitingRoomScrollView();
    }

    private void UpdateWaitingRoomScrollView()
    {
        if (playerSlotPrefab == null || waitingRoomContent == null) return;

        // ลบรายชื่อเก่าทิ้งให้หมดก่อน
        foreach (Transform child in waitingRoomContent)
        {
            Destroy(child.gameObject);
        }

        // เสกรายชื่อใหม่เข้าไปเรียงกัน
        foreach (var player in players)
        {
            string pName = string.IsNullOrEmpty(player.playerName.Value.ToString()) ? $"Player {player.OwnerClientId}" : player.playerName.Value.ToString();

            // สร้าง Prefab ใส่ลงใน Content
            GameObject newSlot = Instantiate(playerSlotPrefab, waitingRoomContent);

            // ดึง TextMeshPro มาแก้ข้อความ
            TextMeshProUGUI slotText = newSlot.GetComponent<TextMeshProUGUI>();
            if (slotText != null)
            {
                slotText.text = pName;
            }
        }
    }
}