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
    [Header("UI References")]
    public TMP_InputField joinInput; // ช่องกรอกรหัสสำหรับฝั่งคนจอย

    private Lobby currentLobby;
    private const int MaxPlayers = 4; // จำนวนผู้เล่นสูงสุดในห้อง

    async void Start()
    {
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

    public async void CreateLobbyAndStartHost()
    {
        try
        {
            Debug.Log("กำลังสร้างห้อง...");

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

            Debug.Log("Created Lobby! Name: " + currentLobby.Name);
            Debug.Log("===== LOBBY CODE (เอาไปให้เพื่อนกรอก): " + currentLobby.LobbyCode + " =====");

            KeepLobbyAlive(currentLobby.Id);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            NetworkManager.Singleton.StartHost();
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
            currentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode);
            Debug.Log("Joined Lobby: " + currentLobby.Name);

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
            Debug.Log("เชื่อมต่อสำเร็จ กำลังเข้าสู่เกม...");
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
            await Task.Delay(15000); // รอ 15 วินาที
            if (currentLobby != null)
            {
                await LobbyService.Instance.SendHeartbeatPingAsync(lobbyId);
               
            }
        }
    }
}