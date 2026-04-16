using Unity.Netcode;
using UnityEngine;
using TMPro;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    [Header("UI References")]
    public TextMeshProUGUI timerText;         // ตัวจับเวลาตอนเล่น
    public TextMeshProUGUI countdownText;     // ตัวหนังสือใหญ่ๆ กลางจอ ไว้โชว์ 3..2..1..GO! (สร้างเพิ่มใน Canvas)
    public GameObject smashButtonObj;         // อ้างอิงปุ่มกดรัวๆ (เพื่อล็อคไม่ให้กดตอนนับถอยหลัง)

    [Header("Game Settings")]
    public float gameDuration = 30f;
    public float countdownDuration = 3f;      // เวลานับถอยหลังก่อนเริ่ม

    // สถานะเกม
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

        // เมื่อเริ่มนับถอยหลัง ให้ย้ายไปหน้าเล่นเกม แต่โชว์ตัวเลขใหญ่ๆ และล็อคปุ่มกด
        isCountingDown.OnValueChanged += (oldValue, newValue) =>
        {
            if (newValue)
            {
                LobbyAndRelayManager.Instance.waitingRoomPanel.SetActive(false);
                LobbyAndRelayManager.Instance.gameplayUIPanel.SetActive(true);

                if (smashButtonObj != null) smashButtonObj.SetActive(false); // ซ่อน/ล็อคปุ่มกด
                if (countdownText != null) countdownText.gameObject.SetActive(true); // โชว์ 3 2 1
            }
        };

        // เมื่อเกมเริ่มจริงๆ (นับถอยหลังจบ) ให้ปลดล็อคปุ่ม
        isGameActive.OnValueChanged += (oldValue, newValue) =>
        {
            if (newValue)
            {
                if (countdownText != null) countdownText.gameObject.SetActive(false); // ซ่อนเลข 3 2 1
                if (smashButtonObj != null) smashButtonObj.SetActive(true); // โชว์ปุ่มให้รัวนิ้วได้
            }
        };

        UpdateTimerUI(timeRemaining.Value);
    }

    private void Update()
    {
        if (!IsServer) return;

        // 1. ถ้านับถอยหลังอยู่
        if (isCountingDown.Value)
        {
            if (currentCountdown.Value > 0)
            {
                currentCountdown.Value -= Time.deltaTime;
            }
            else
            {
                // นับถอยหลังจบ -> เริ่มเกมจริงๆ
                isCountingDown.Value = false;
                timeRemaining.Value = gameDuration;
                isGameActive.Value = true;
            }
            return;
        }

        // 2. ถ้าเกมกำลังเล่นอยู่
        if (isGameActive.Value)
        {
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
    }

    // ฟังก์ชันอัปเดต UI หน้าจอ
    private void OnCountdownChanged(float oldValue, float newValue)
    {
        if (countdownText == null) return;

        int ceilTime = Mathf.CeilToInt(newValue);
        if (ceilTime > 0)
            countdownText.text = ceilTime.ToString();
        else
            countdownText.text = "GO!";
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

    // ฟังก์ชันกดเริ่มเกมจาก Host (เปลี่ยนจากเริ่มเลย เป็นเริ่มนับถอยหลัง)
    public void HostStartGameFromLobby()
    {
        if (!IsServer) return;
        currentCountdown.Value = countdownDuration;
        isCountingDown.Value = true;
    }

    public void HostStartRematch()
    {
        if (!IsServer) return;

        // Reset คะแนนทุกคน
        PlayerScore[] players = FindObjectsByType<PlayerScore>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            player.currentScore.Value = 0;
        }

        HideWinnerClientRpc();

        // เข้าสู่โหมดนับถอยหลังใหม่
        currentCountdown.Value = countdownDuration;
        isCountingDown.Value = true;
    }

    // (ส่วนหาผู้ชนะและ ClientRpc คงไว้แบบเดิมครับ)
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