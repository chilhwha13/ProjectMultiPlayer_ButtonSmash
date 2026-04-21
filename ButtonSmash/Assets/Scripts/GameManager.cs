using System.Collections;
using Unity.Netcode;
using UnityEngine;
using TMPro;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    [Header("UI References")]
    public GameObject mainGamePanel;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI countdownText;
    public GameObject smashButtonObj;

    [Header("Game Settings")]
    public float gameDuration = 30f;
    public float countdownDuration = 3f;

    [Header("Countdown Visual Settings")]
    public Color countdownNumberColor = Color.yellow;
    public Color goTextColor = Color.green;
    public float startScale = 3.5f;
    public float endScale = 1.0f;
    public float animDuration = 0.4f;

    public NetworkVariable<bool> isCountingDown = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> currentCountdown = new NetworkVariable<float>(3f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> isGameActive = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> timeRemaining = new NetworkVariable<float>(30f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private int _lastCeilTime = -1;
    private Coroutine _countdownAnimCoroutine;

    private void Awake() { Instance = this; }

    public override void OnNetworkSpawn()
    {
        timeRemaining.OnValueChanged += OnTimeChanged;
        currentCountdown.OnValueChanged += OnCountdownChanged;
        isCountingDown.OnValueChanged += OnCountingDownChanged;
        isGameActive.OnValueChanged += OnGameActiveChanged;

        OnCountingDownChanged(false, isCountingDown.Value);
        OnGameActiveChanged(false, isGameActive.Value);

        if (IsServer)
        {
            currentCountdown.Value = countdownDuration;
            isCountingDown.Value = true;
        }
    }

    private void Update()
    {
        if (!IsServer) return;

        if (isCountingDown.Value)
        {
            if (currentCountdown.Value > 0)
                currentCountdown.Value -= Time.deltaTime;
            else
            {
                isCountingDown.Value = false;
                timeRemaining.Value = gameDuration;
                isGameActive.Value = true;
            }
        }
        else if (isGameActive.Value)
        {
            if (timeRemaining.Value > 0)
                timeRemaining.Value -= Time.deltaTime;
            else
            {
                timeRemaining.Value = 0;
                isGameActive.Value = false;
                DetermineWinner();
            }
        }
    }

    private void OnCountingDownChanged(bool oldValue, bool newValue)
    {
        if (newValue && countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
            _lastCeilTime = -1;
        }
    }

    private void OnGameActiveChanged(bool oldValue, bool newValue)
    {
        if (newValue && countdownText != null) countdownText.gameObject.SetActive(false);
        if (newValue && smashButtonObj != null) smashButtonObj.SetActive(true);
    }

    private void OnCountdownChanged(float oldValue, float newValue)
    {
        if (countdownText == null || !isCountingDown.Value) return;

        int ceilTime = Mathf.CeilToInt(newValue);
        if (ceilTime != _lastCeilTime && ceilTime >= 0)
        {
            _lastCeilTime = ceilTime;

            // ล้างค่า Rich Text เก่าและตั้งสีใหม่จาก Inspector
            countdownText.text = ceilTime > 0 ? ceilTime.ToString() : "GO!";
            countdownText.color = ceilTime > 0 ? countdownNumberColor : goTextColor;

            // รันแอนิเมชันแบบใช้ตัวแปร Coroutine เพื่อป้องกันการรันซ้อน
            if (_countdownAnimCoroutine != null) StopCoroutine(_countdownAnimCoroutine);
            _countdownAnimCoroutine = StartCoroutine(AnimateCountdownText());
        }
    }

    private IEnumerator AnimateCountdownText()
    {
        Transform t = countdownText.transform;
        // เช็ค CanvasGroup ถ้าไม่มีให้ข้ามไป (ไม่พัง)
        CanvasGroup cg = countdownText.GetComponent<CanvasGroup>();

        float elapsed = 0;
        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float p = elapsed / animDuration;
            float curve = 1.0f - Mathf.Pow(1.0f - p, 3);

            t.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, curve);
            if (cg != null) cg.alpha = Mathf.Lerp(0.5f, 1f, p);

            yield return null;
        }
        t.localScale = Vector3.one * endScale;
        if (cg != null) cg.alpha = 1f;
    }

    private void OnTimeChanged(float oldValue, float newValue)
    {
        if (timerText != null) timerText.text = $"⏱ {Mathf.CeilToInt(newValue)}";
    }

    private void DetermineWinner()
    {
        PlayerScore[] players = FindObjectsByType<PlayerScore>(FindObjectsSortMode.None);
        PlayerScore winner = null;
        int max = -1;
        foreach (var p in players) { if (p.currentScore.Value > max) { max = p.currentScore.Value; winner = p; } }
        if (winner != null) ShowWinnerClientRpc(winner.playerName.Value.ToString(), max);
    }

    [ClientRpc]
    private void ShowWinnerClientRpc(string winnerName, int score) => SmashUIManager.Instance?.ShowWinner(winnerName, score);
}