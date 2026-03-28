using Unity.Netcode;
using UnityEngine;
using TMPro;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    [Header("UI References")]
    public TextMeshProUGUI timerText;

    // 1. ตัวแปรเก็บเวลา (Server เป็นคนลดเวลา, ทุกคนอ่านค่าไปโชว์)
    public NetworkVariable<float> timeRemaining = new NetworkVariable<float>(
        30f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // 2. ตัวแปรเช็คสถานะว่าเกมกำลังเล่นอยู่หรือไม่
    public NetworkVariable<bool> isGameActive = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        // เมื่อ Host สร้างห้องเสร็จ ให้เริ่มจับเวลาทันที
        if (IsServer)
        {
            isGameActive.Value = true;
            timeRemaining.Value = 30f; // ตั้งเวลา 30 วินาที
        }
    }

    private void Update()
    {
        // 3. อัปเดต UI หน้าจอของทุกคน
        if (timerText != null)
        {
            if (timeRemaining.Value > 0)
            {
                timerText.text = "Time: " + Mathf.CeilToInt(timeRemaining.Value).ToString();
            }
            else
            {
                timerText.text = "TIME'S UP!";
            }
        }

        // 4. ส่วนของการคำนวณเวลา (ให้ Server ทำงานคนเดียว)
        if (!IsServer || !isGameActive.Value) return;

        if (timeRemaining.Value > 0)
        {
            // ลดเวลาลงตามเฟรมเรต
            timeRemaining.Value -= Time.deltaTime;
        }
        else
        {
            // หมดเวลา! สั่งปิดระบบเกม
            timeRemaining.Value = 0;
            isGameActive.Value = false;
        }
    }
}