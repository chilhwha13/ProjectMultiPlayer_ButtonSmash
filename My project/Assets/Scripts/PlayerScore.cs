using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerScore : NetworkBehaviour
{
    // เพิ่มตัวแปรเก็บชื่อ (ใช้ FixedString เพื่อให้รองรับการส่งผ่าน Network)
    public NetworkVariable<FixedString32Bytes> playerName = new NetworkVariable<FixedString32Bytes>(
        "",
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> currentScore = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private float lastClickTime;
    private float clickCooldown = 0.1f;

    public override void OnNetworkSpawn()
    {
        ScoreboardManager.Instance?.RegisterPlayer(this);

        // เมื่อเกิดมา ถ้าเราเป็นเจ้าของตัวละคร ให้ส่งชื่อไปให้ Server บันทึก
        if (IsOwner)
        {
            SubmitNameServerRpc(LobbyAndRelayManager.PlayerName);
        }

        currentScore.OnValueChanged += (oldValue, newValue) =>
        {
            ScoreboardManager.Instance?.RefreshScoreboard();
        };

        playerName.OnValueChanged += (oldValue, newValue) =>
        {
            ScoreboardManager.Instance?.RefreshScoreboard();
        };
    }

    [ServerRpc]
    private void SubmitNameServerRpc(FixedString32Bytes name)
    {
        playerName.Value = name; // Server เป็นคนอัปเดตชื่อให้ทุกคนเห็นตรงกัน
    }

    public void OnClickSmashButton()
    {
        if (!IsOwner) return;
        if (!GameManager.Instance.isGameActive.Value) return;
        if (Time.time - lastClickTime < clickCooldown) return;

        lastClickTime = Time.time;
        SubmitScoreServerRpc();
    }

    [ServerRpc]
    private void SubmitScoreServerRpc()
    {
        currentScore.Value++;
    }
}