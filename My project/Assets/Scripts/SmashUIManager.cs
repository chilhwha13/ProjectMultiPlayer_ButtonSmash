using Unity.Netcode;
using UnityEngine;

public class SmashUIManager : MonoBehaviour
{
    // ฟังก์ชันนี้จะเอาไปใส่ใน OnClick ของปุ่ม
    public void OnPressSmashButton()
    {
        // 1. เช็คก่อนว่าระบบเน็ตเวิร์คทำงานอยู่ไหม และเราเชื่อมต่อเป็น Client/Host หรือยัง
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
        {
            // 2. ตามหา "ตัวละครของฉัน (Local Player)" ที่ระบบเพิ่งเสกมาให้
            var myPlayerObject = NetworkManager.Singleton.LocalClient.PlayerObject;

            if (myPlayerObject != null)
            {
                // 3. ดึงสคริปต์ PlayerScore ที่ติดอยู่กับตัวละครเราออกมา
                PlayerScore myScoreScript = myPlayerObject.GetComponent<PlayerScore>();

                if (myScoreScript != null)
                {
                    // 4. สั่งให้ฟังก์ชันกดปุ่มทำงาน!
                    myScoreScript.OnClickSmashButton();
                }
            }
        }
        else
        {
            Debug.LogWarning("ยังไม่ได้เข้าเกม หรือยังไม่ได้จอยห้อง!");
        }
    }
}