using UnityEngine;
using TMPro;
using System.Collections.Generic;
using Unity.Netcode;

public class ScoreboardManager : MonoBehaviour
{
    public static ScoreboardManager Instance;

    [Header("In-Game Scoreboard (For GameScene)")]
    public TextMeshProUGUI scoreTextDisplay;

    [Header("Waiting Room UI (For LobbyScene)")]
    public GameObject playerSlotPrefab;
    public Transform waitingRoomContent;

    private List<PlayerScore> players = new List<PlayerScore>();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // เมื่อโหลดเปลี่ยน Scene ไปมา ให้ดึงรายชื่อผู้เล่นกลับมาใส่ลิสต์ใหม่ให้ครบ
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
        {
            players.Clear();
            PlayerScore[] existingPlayers = FindObjectsByType<PlayerScore>(FindObjectsSortMode.None);
            foreach (var p in existingPlayers)
            {
                RegisterPlayer(p);
            }
        }
    }

    public void RegisterPlayer(PlayerScore player)
    {
        if (!players.Contains(player))
        {
            players.Add(player);
        }
        RefreshScoreboard();
    }

    public void UnregisterPlayer(PlayerScore player)
    {
        if (players.Contains(player))
        {
            players.Remove(player);
        }
        RefreshScoreboard();
    }

    public void RefreshScoreboard()
    {
        // 1. อัปเดตกระดานคะแนน (GameScene)
        if (scoreTextDisplay != null)
        {
            string board = "--- Current Scores ---\n";
            foreach (var player in players)
            {
                string pName = string.IsNullOrEmpty(player.playerName.Value.ToString()) ? $"Player {player.OwnerClientId}" : player.playerName.Value.ToString();
                board += $"{pName} : {player.currentScore.Value} Pts\n";
            }
            scoreTextDisplay.text = board;
        }

        // 2. อัปเดตรายชื่อใน ScrollView (LobbyScene)
        UpdateWaitingRoomScrollView();
    }

    private void UpdateWaitingRoomScrollView()
    {
        if (playerSlotPrefab == null || waitingRoomContent == null) return;

        foreach (Transform child in waitingRoomContent)
        {
            Destroy(child.gameObject);
        }

        foreach (var player in players)
        {
            string pName = string.IsNullOrEmpty(player.playerName.Value.ToString()) ? $"Player {player.OwnerClientId}" : player.playerName.Value.ToString();
            GameObject newSlot = Instantiate(playerSlotPrefab, waitingRoomContent);
            TextMeshProUGUI slotText = newSlot.GetComponent<TextMeshProUGUI>();
            if (slotText != null)
            {
                slotText.text = pName;
            }
        }
    }
}