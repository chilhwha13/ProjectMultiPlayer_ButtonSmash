using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
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
    public static LobbyAndRelayManager Instance;

    // ตัวแปร Static เก็บชื่อไว้ชั่วคราวก่อนเข้าห้อง
    public static string PlayerName = "Player";

    [Header("UI Panels")]
    public GameObject nameInputPanel;     // หน้า 1: ตั้งชื่อ
    public GameObject lobbyMenuPanel;     // หน้า 2: สร้าง/เข้าร่วม
    public GameObject waitingRoomPanel;   // หน้า 3: ห้องรอ
    public GameObject gameplayUIPanel;    // หน้า 4: UI ตอนเล่นเกม (ปุ่ม Smash, Timer)

    [Header("Name Input UI")]
    public TMP_InputField nameInput;

    [Header("Lobby Menu UI")]
    public TMP_InputField joinInput;

    [Header("Waiting Room UI")]
    public TextMeshProUGUI lobbyCodeText; // โชว์โค้ดให้ก๊อปปี้
    public GameObject hostStartGameButton;// ปุ่มเริ่มเกม (โชว์เฉพาะ Host)

    private Lobby currentLobby;
    private const int MaxPlayers = 4;

    private void Awake()
    {
        Instance = this;
    }

    async void Start()
    {
        // เริ่มต้นด้วยการเปิดหน้าต่างตั้งชื่อ
        SwitchToPanel(nameInputPanel);
        gameplayUIPanel.SetActive(false);

        await InitializeAndSignIn();
    }

    // ================= UI FLOW =================

    public void OnConfirmNameClicked()
    {
        if (!string.IsNullOrEmpty(nameInput.text))
        {
            PlayerName = nameInput.text;
            SwitchToPanel(lobbyMenuPanel); // ไปหน้า สร้าง/จอย
        }
    }

    private void SwitchToPanel(GameObject activePanel)
    {
        nameInputPanel.SetActive(false);
        lobbyMenuPanel.SetActive(false);
        waitingRoomPanel.SetActive(false);

        if (activePanel != null) activePanel.SetActive(true);
    }

    public void CopyLobbyCode()
    {
        if (currentLobby != null)
        {
            GUIUtility.systemCopyBuffer = currentLobby.LobbyCode;
            Debug.Log("คัดลอกโค้ดแล้ว: " + currentLobby.LobbyCode);
        }
    }

    public void HostStartGame()
    {
        // เมื่อ Host กดปุ่มเริ่มเกม
        if (NetworkManager.Singleton.IsServer)
        {
            GameManager.Instance.HostStartGameFromLobby();
        }
    }

    // ================= NETWORKING =================

    private async Task InitializeAndSignIn()
    {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    public async void CreateLobbyAndStartHost()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(MaxPlayers - 1);
            string relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            CreateLobbyOptions lobbyOptions = new CreateLobbyOptions
            {
                IsPrivate = false,
                Data = new Dictionary<string, DataObject>
                {
                    { "RelayCode", new DataObject(DataObject.VisibilityOptions.Public, relayJoinCode) }
                }
            };
            currentLobby = await LobbyService.Instance.CreateLobbyAsync("My Button Smash Lobby", MaxPlayers, lobbyOptions);

            KeepLobbyAlive(currentLobby.Id);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            NetworkManager.Singleton.StartHost();

            // เปลี่ยนหน้าจอไปที่ Waiting Room และตั้งค่า UI สำหรับ Host
            lobbyCodeText.text = "Lobby Code: " + currentLobby.LobbyCode;
            hostStartGameButton.SetActive(true);
            SwitchToPanel(waitingRoomPanel);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("Failed to create lobby: " + e.Message);
        }
    }

    public void OnClickJoinButton()
    {
        if (joinInput != null && !string.IsNullOrEmpty(joinInput.text))
        {
            JoinLobbyWithCode(joinInput.text);
        }
    }

    private async void JoinLobbyWithCode(string lobbyCode)
    {
        try
        {
            currentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode);
            string relayJoinCode = currentLobby.Data["RelayCode"].Value;
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );

            NetworkManager.Singleton.StartClient();

            // เปลี่ยนหน้าจอไปที่ Waiting Room (Client ไม่เห็นปุ่มเริ่มเกม)
            lobbyCodeText.text = "Lobby Code: " + currentLobby.LobbyCode;
            hostStartGameButton.SetActive(false);
            SwitchToPanel(waitingRoomPanel);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("Failed to join lobby: " + e.Message);
        }
    }

    private async void KeepLobbyAlive(string lobbyId)
    {
        while (currentLobby != null)
        {
            await Task.Delay(15000);
            if (currentLobby != null)
            {
                await LobbyService.Instance.SendHeartbeatPingAsync(lobbyId);
            }
        }
    }
}