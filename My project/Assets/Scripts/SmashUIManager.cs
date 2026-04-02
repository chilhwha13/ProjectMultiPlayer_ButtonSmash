using Unity.Netcode;
using UnityEngine;
using TMPro;

public class SmashUIManager : MonoBehaviour // กลับมาใช้ MonoBehaviour ปกติ
{
    public static SmashUIManager Instance;

    [Header("Winner UI")]
    public GameObject winnerPanel;
    public TextMeshProUGUI winnerText;
    public Animator winnerAnimator;

    [Header("Gameplay")]
    public GameObject smashButton;
    public GameObject rematchButton;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // ซ่อนปุ่มกด Rematch ไว้ก่อนตอนเริ่ม Scene
        if (rematchButton != null) rematchButton.SetActive(false);
    }

    public void OnPressSmashButton()
    {
        // เช็คว่าเข้าห้องแล้วหรือยังผ่าน NetworkManager
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient) return;
        if (!GameManager.Instance.isGameActive.Value) return;

        var player = NetworkManager.Singleton.LocalClient.PlayerObject;
        player?.GetComponent<PlayerScore>()?.OnClickSmashButton();
    }

    public void ShowWinner(ulong winnerId, int score)
    {
        if (winnerPanel != null) winnerPanel.SetActive(true);
        if (winnerText != null) winnerText.text = $"🏆 Player {winnerId} Wins!\nScore: {score}";
        if (smashButton != null) smashButton.SetActive(false);

        // 🔥 เปิดปุ่ม Rematch เฉพาะบนหน้าจอของคนที่เป็น Host เท่านั้น!
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

        // ซ่อนปุ่ม Rematch อีกครั้งเมื่อเริ่มเกมรอบใหม่
        if (rematchButton != null) rematchButton.SetActive(false);
    }

    // ================= REMATCH =================

    public void OnClickRematch()
    {
        // ส่งคำสั่งไปยัง GameManager เพื่อให้ Server สั่งรีเซ็ตเกม
        if (NetworkManager.Singleton.IsServer)
        {
            GameManager.Instance.HostStartRematch();
        }
    }
}