using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class ScoreboardManager : MonoBehaviour
{
    public static ScoreboardManager Instance;

    public TextMeshProUGUI scoreTextDisplay;

    [Header("Waiting Room Reference")]
    public TextMeshProUGUI waitingRoomPlayerListDisplay; // ช่อง Text สำหรับแสดงชื่อคนในห้องรอ

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
        string board = "--- Current Scores ---\n";
        string waitingList = "Players in Lobby:\n";

        foreach (var player in players)
        {
            string pName = string.IsNullOrEmpty(player.playerName.Value.ToString()) ? $"Player {player.OwnerClientId}" : player.playerName.Value.ToString();

            board += $"{pName} : {player.currentScore.Value} Pts\n";
            waitingList += $"- {pName}\n";
        }

        if (scoreTextDisplay != null) scoreTextDisplay.text = board;

        // อัปเดตรายชื่อในหน้า Waiting Room ด้วย
        if (waitingRoomPlayerListDisplay != null) waitingRoomPlayerListDisplay.text = waitingList;
    }
}