using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro; // สำหรับจัดการ UI Input Field
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class LobbyAndRelayManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField joinInput; // ช่องกรอกรหัสสำหรับฝั่งคนจอย

    private Lobby currentLobby;
    private const int MaxPlayers = 4; // จำนวนผู้เล่นสูงสุดในห้อง

    async void Start()
    {
        // 1. เริ่มต้นระบบ Unity Services และ Login แบบไม่ระบุตัวตน
        await InitializeAndSignIn();
    }

    private async Task InitializeAndSignIn()
    {
        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            AuthenticationService.Instance.SignedIn += () =>
            {
                Debug.Log("Signed in as: " + AuthenticationService.Instance.PlayerId);
            };
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    // ---------------------------------------------------------
    // ฝั่ง HOST: กดปุ่มสร้างห้อง
    // ---------------------------------------------------------
    public async void CreateLobbyAndStartHost()
    {
        try
        {
            Debug.Log("กำลังสร้างห้อง...");

            // 1. ขอพื้นที่เซิร์ฟเวอร์ Relay
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(MaxPlayers - 1);

            // 2. ดึงรหัส Relay (แก้ Error AllocationId แล้ว)
            string relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            // 3. สร้าง Lobby และยัด Relay Join Code ลงไปในข้อมูลห้องซ่อนไว้
            CreateLobbyOptions lobbyOptions = new CreateLobbyOptions
            {
                IsPrivate = false,
                Data = new Dictionary<string, DataObject>
                {
                    { "RelayCode", new DataObject(DataObject.VisibilityOptions.Public, relayJoinCode) }
                }
            };
            currentLobby = await LobbyService.Instance.CreateLobbyAsync("My Button Smash Lobby", MaxPlayers, lobbyOptions);

            // โชว์ LOBBY CODE ของจริงที่ให้เพื่อนเอาไปกรอก
            Debug.Log("Created Lobby! Name: " + currentLobby.Name);
            Debug.Log("===== LOBBY CODE (เอาไปให้เพื่อนกรอก): " + currentLobby.LobbyCode + " =====");

            // เริ่มระบบปั๊มหัวใจเลี้ยงห้องไม่ให้โดนลบ
            KeepLobbyAlive(currentLobby.Id);

            // 4. นำข้อมูล Relay ไปตั้งค่าให้ Netcode
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            // 5. เริ่มเกมในฐานะ Host
            NetworkManager.Singleton.StartHost();
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("Failed to create lobby: " + e.Message);
        }
    }

    // ---------------------------------------------------------
    // ฝั่ง CLIENT: ระบบปุ่ม Join
    // ---------------------------------------------------------
    public void OnClickJoinButton()
    {
        // เช็คก่อนว่าผู้เล่นพิมพ์รหัสมาหรือยัง
        if (joinInput != null && !string.IsNullOrEmpty(joinInput.text))
        {
            Debug.Log("กำลังจอยห้องด้วยรหัส: " + joinInput.text);
            JoinLobbyWithCode(joinInput.text);
        }
        else
        {
            Debug.LogWarning("กรุณากรอกรหัสห้องก่อน!");
        }
    }

    private async void JoinLobbyWithCode(string lobbyCode)
    {
        try
        {
            // 1. เข้าร่วม Lobby ด้วยรหัส
            currentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode);
            Debug.Log("Joined Lobby: " + currentLobby.Name);

            // 2. ดึงรหัส Relay Join Code ออกมาจากข้อมูลของ Lobby
            string relayJoinCode = currentLobby.Data["RelayCode"].Value;

            // 3. นำรหัสไปเชื่อมต่อกับ Relay
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);

            // 4. นำข้อมูลไปตั้งค่าให้ Netcode
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );

            // 5. เริ่มเกมในฐานะ Client
            NetworkManager.Singleton.StartClient();
            Debug.Log("เชื่อมต่อสำเร็จ กำลังเข้าสู่เกม...");
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("Failed to join lobby: " + e.Message);
        }
    }

    // ---------------------------------------------------------
    // ระบบป้องกันห้องโดนลบอัตโนมัติ
    // ---------------------------------------------------------
    private async void KeepLobbyAlive(string lobbyId)
    {
        while (currentLobby != null)
        {
            await Task.Delay(15000); // รอ 15 วินาที
            if (currentLobby != null)
            {
                await LobbyService.Instance.SendHeartbeatPingAsync(lobbyId);
                // Debug.Log("ส่ง Heartbeat ป้องกันห้องโดนลบ..."); // ปิดไว้จะได้ไม่รก Console
            }
        }
    }
}