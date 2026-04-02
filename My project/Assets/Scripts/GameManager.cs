using Unity.Netcode;
using UnityEngine;
using TMPro;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    [Header("UI")]
    public TextMeshProUGUI timerText;

    [Header("Game Settings")]
    public float gameDuration = 30f;

    public NetworkVariable<float> timeRemaining = new NetworkVariable<float>(
        30f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> isGameActive = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        timeRemaining.OnValueChanged += OnTimeChanged;

        if (IsServer)
        {
            StartGame();
        }

        UpdateTimerUI(timeRemaining.Value);
    }

    private void Update()
    {
        if (!IsServer || !isGameActive.Value) return;

        if (timeRemaining.Value > 0)
        {
            timeRemaining.Value -= Time.deltaTime;
        }
        else
        {
            timeRemaining.Value = 0;
            isGameActive.Value = false;

            DetermineWinner();
        }
    }

    private void OnTimeChanged(float oldValue, float newValue)
    {
        UpdateTimerUI(newValue);
    }

    private void UpdateTimerUI(float time)
    {
        if (timerText == null) return;

        if (time > 0)
            timerText.text = "Time: " + Mathf.CeilToInt(time);
        else
            timerText.text = "TIME'S UP!";
    }

    private void StartGame()
    {
        timeRemaining.Value = gameDuration;
        isGameActive.Value = true;
    }

    private void DetermineWinner()
    {
        PlayerScore[] players = FindObjectsByType<PlayerScore>(FindObjectsSortMode.None);

        PlayerScore winner = null;
        int highestScore = -1;

        foreach (var player in players)
        {
            if (player.currentScore.Value > highestScore)
            {
                highestScore = player.currentScore.Value;
                winner = player;
            }
        }

        if (winner != null)
        {
            ShowWinnerClientRpc(winner.OwnerClientId, highestScore);
        }
    }

    [ClientRpc]
    private void ShowWinnerClientRpc(ulong winnerId, int score)
    {
        SmashUIManager.Instance?.ShowWinner(winnerId, score);
    }

    // ================= REMATCH (HOST ONLY) =================

    public void HostStartRematch()
    {
        if (!IsServer) return;

        Debug.Log("HOST STARTED REMATCH");

        timeRemaining.Value = gameDuration;
        isGameActive.Value = true;

        PlayerScore[] players = FindObjectsByType<PlayerScore>(FindObjectsSortMode.None);

        foreach (var player in players)
        {
            player.currentScore.Value = 0;
        }

        HideWinnerClientRpc();
    }

    [ClientRpc]
    private void HideWinnerClientRpc()
    {
        SmashUIManager.Instance?.HideWinner();
    }
}