using Unity.Netcode;
using UnityEngine;

public class PlayerScore : NetworkBehaviour
{
    // 1. ตัวแปรเก็บคะแนน ที่จะซิงค์อัตโนมัติจาก Server ไปยัง Client ทุกคน
    public NetworkVariable<int> currentScore = new NetworkVariable<int>(
        0, // ค่าเริ่มต้น
        NetworkVariableReadPermission.Everyone, // ทุกคนอ่านค่าได้ (เอาไปโชว์ UI)
        NetworkVariableWritePermission.Server   // Server เท่านั้นที่แก้ไขค่าได้ (ป้องกัน Client แฮ็กคะแนน)
    );

    public override void OnNetworkSpawn()
    {
        // ให้กระดานคะแนนอัปเดตทันทีที่มีตัวละครนี้โผล่มาในเกม (จะได้เห็นว่ามีคนเข้าห้อง)
        if (ScoreboardManager.Instance != null)
        {
            ScoreboardManager.Instance.RefreshScoreboard();
        }

        // ดักจับเหตุการณ์เมื่อคะแนนมีการเปลี่ยนแปลง
        currentScore.OnValueChanged += (int previousValue, int newValue) =>
        {
            // เอา Debug.Log ออก แล้วเปลี่ยนมาเรียกใช้ Scoreboard แทน
            if (ScoreboardManager.Instance != null)
            {
                ScoreboardManager.Instance.RefreshScoreboard();
            }
        };
    }

    // ---------------------------------------------------------
    // ฟังก์ชันนี้เดี๋ยวเราจะเอาไปผูกกับ "ปุ่มกดรัวๆ" ในหน้า UI
    // ---------------------------------------------------------
    public void OnClickSmashButton()
    {
        if (!IsOwner) return;

        // [เพิ่มส่วนนี้] เช็คว่าเกมยังเล่นอยู่ไหม ถ้าหมดเวลาแล้วให้กดไม่ติด
        if (GameManager.Instance != null && !GameManager.Instance.isGameActive.Value)
        {
            Debug.Log("Time is up! Cannot smash anymore.");
            return;
        }

        // ส่งคำสั่งไปขอบวกคะแนนที่ Server
        SubmitScoreServerRpc();
    }

    // ---------------------------------------------------------
    // [ServerRpc] คือคำสั่งที่ Client ส่งไปบอกให้ "Server เป็นคนรันโค้ดในปีกกานี้"
    // ---------------------------------------------------------
    [ServerRpc]
    private void SubmitScoreServerRpc()
    {
        // 4. Server ทำการบวกคะแนนให้ 
        // พอค่านี้เปลี่ยน มันจะแจ้งเตือน (OnValueChanged) ไปยังทุกคนอัตโนมัติ
        currentScore.Value += 1;
    }
}