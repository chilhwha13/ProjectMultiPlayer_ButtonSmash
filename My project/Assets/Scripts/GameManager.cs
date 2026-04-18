using Unity.Netcode;
using UnityEngine;
using TMPro;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    [Header("UI References")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI countdownText; // โชว์เลข 3 2 1
    public GameObject smashButtonObj;     // ตัวปุ่มกด

    [Header("Game Settings")]
    public float gameDuration = 30f;
    public float countdownDuration = 3f;

    public NetworkVariable<bool> isCountingDown = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> currentCountdown = new NetworkVariable<float>(3f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> isGameActive = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> timeRemaining = new NetworkVariable<float>(30f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        timeRemaining.OnValueChanged += OnTimeChanged;
        currentCountdown.OnValueChanged += OnCountdownChanged;
        isGameActive.OnValueChanged += OnGameActiveChanged;

        // เมื่อหลุดมาหน้า GameScene ให้ Host สั่งเริ่มนับถอยหลังทันที
        if (IsServer)
        {
            currentCountdown.Value = countdownDuration;
            isCountingDown.Value = true;
        }

        // เรียกอัปเดต UI ครั้งแรกสุด
        UpdateTimerUI(timeRemaining.Value);
        OnCountdownChanged(0, currentCountdown.Value);
        OnGameActiveChanged(false, isGameActive.Value);
    }

    private void Update()
    {
        if (!IsServer) return;

        if (isCountingDown.Value)
        {
            if (currentCountdown.Value > 0)
            {
                currentCountdown.Value -= Time.deltaTime;
            }
            else
            {
                // นับเสร็จ เริ่มเกม
                isCountingDown.Value = false;
                timeRemaining.Value = gameDuration;
                isGameActive.Value = true;
            }
            return;
        }

        if (isGameActive.Value)
        {
            if (timeRemaining.Value > 0)
            {
                timeRemaining.Value -= Time.deltaTime;
            }
            else
            {
                // หมดเวลา หาคนชนะ
                timeRemaining.Value = 0;
                isGameActive.Value = false;
                DetermineWinner();
            }
        }
    }

    private void OnCountdownChanged(float oldValue, float newValue)
    {
        if (countdownText == null) return;

        int ceilTime = Mathf.CeilToInt(newValue);
        if (isCountingDown.Value)
        {
            countdownText.gameObject.SetActive(true);
            countdownText.text = ceilTime > 0 ? ceilTime.ToString() : "GO!";
        }
        else
        {
            countdownText.gameObject.SetActive(false);
        }
    }

    private void OnGameActiveChanged(bool oldValue, bool newValue)
    {
        // ถ้าเกมเริ่ม ให้เปิดปุ่มทุบ ถ้ายังไม่เริ่มให้ซ่อนไว้
        if (smashButtonObj != null) smashButtonObj.SetActive(newValue);
        if (countdownText != null && newValue) countdownText.gameObject.SetActive(false);
    }

    private void OnTimeChanged(float oldValue, float newValue)
    {
        UpdateTimerUI(newValue);
    }

    private void UpdateTimerUI(float time)
    {
        if (timerText == null) return;
        timerText.text = time > 0 ? "Time: " + Mathf.CeilToInt(time) : "TIME'S UP!";
    }

    public void HostStartRematch()
    {
        if (!IsServer) return;

        PlayerScore[] players = FindObjectsByType<PlayerScore>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            player.currentScore.Value = 0; // รีเซ็ตคะแนน
        }

        HideWinnerClientRpc(); // ซ่อนป้ายประกาศผล

        // เริ่มนับถอยหลังใหม่
        currentCountdown.Value = countdownDuration;
        isCountingDown.Value = true;
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
            ShowWinnerClientRpc(winner.playerName.Value.ToString(), highestScore);
        }
    }

    [ClientRpc]
    private void ShowWinnerClientRpc(string winnerName, int score)
    {
        SmashUIManager.Instance?.ShowWinner(winnerName, score);
    }

    [ClientRpc]
    private void HideWinnerClientRpc()
    {
        SmashUIManager.Instance?.HideWinner();
    }
}