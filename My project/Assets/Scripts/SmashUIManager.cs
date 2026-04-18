using Unity.Netcode;
using UnityEngine;
using TMPro;

public class SmashUIManager : MonoBehaviour
{
    public static SmashUIManager Instance;

    [Header("Winner UI")]
    public GameObject winnerPanel;
    public TextMeshProUGUI winnerText;
    public Animator winnerAnimator;

    [Header("Gameplay UI")]
    public GameObject smashButton;
    public GameObject rematchButton;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (rematchButton != null) rematchButton.SetActive(false);
        if (winnerPanel != null) winnerPanel.SetActive(false);
    }

    public void OnPressSmashButton()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient) return;
        if (GameManager.Instance == null || !GameManager.Instance.isGameActive.Value) return;

        var player = NetworkManager.Singleton.LocalClient.PlayerObject;
        player?.GetComponent<PlayerScore>()?.OnClickSmashButton();
    }

    public void ShowWinner(string winnerName, int score)
    {
        if (winnerPanel != null) winnerPanel.SetActive(true);
        if (winnerText != null) winnerText.text = $"🏆 {winnerName} Wins!\nScore: {score}";
        if (smashButton != null) smashButton.SetActive(false);

        // โชว์ปุ่ม Rematch เฉพาะหน้าจอ Host
        if (NetworkManager.Singleton.IsServer)
        {
            if (rematchButton != null) rematchButton.SetActive(true);
        }
        else
        {
            if (rematchButton != null) rematchButton.SetActive(false);
        }

        if (winnerAnimator != null) winnerAnimator.SetTrigger("Show");
    }

    public void HideWinner()
    {
        if (winnerPanel != null) winnerPanel.SetActive(false);
        if (smashButton != null) smashButton.SetActive(true);
        if (rematchButton != null) rematchButton.SetActive(false);
    }

    public void OnClickNextLevel()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            LobbyAndRelayManager.Instance.HostLoadNextLevel();
        }
    }
}