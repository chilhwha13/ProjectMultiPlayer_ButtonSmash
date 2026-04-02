using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class ScoreboardManager : MonoBehaviour
{
    public static ScoreboardManager Instance;

    public TextMeshProUGUI scoreTextDisplay;

    private List<PlayerScore> players = new List<PlayerScore>();

    private void Awake()
    {
        Instance = this;
    }

    public void RegisterPlayer(PlayerScore player)
    {
        players.Add(player);
        RefreshScoreboard();
    }

    public void RefreshScoreboard()
    {
        string board = "--- Current Scores ---\n";

        foreach (var player in players)
        {
            board += $"Player {player.OwnerClientId} : {player.currentScore.Value} Pts\n";
        }

        scoreTextDisplay.text = board;
    }
}