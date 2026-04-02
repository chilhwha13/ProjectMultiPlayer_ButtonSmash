using Unity.Netcode;
using UnityEngine;

public class PlayerScore : NetworkBehaviour
{
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

        currentScore.OnValueChanged += (oldValue, newValue) =>
        {
            ScoreboardManager.Instance?.RefreshScoreboard();
        };
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