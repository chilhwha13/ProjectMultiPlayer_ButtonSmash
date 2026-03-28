using Unity.Netcode;
using UnityEngine;

public class PlayerScore : NetworkBehaviour
{
    public NetworkVariable<int> currentScore = new NetworkVariable<int>(
        0, // ค่าเริ่มต้น
        NetworkVariableReadPermission.Everyone, // ทุกคนอ่านค่าได้ (เอาไปโชว์ UI)
        NetworkVariableWritePermission.Server   // Server เท่านั้นที่แก้ไขค่าได้ (ป้องกัน Client แฮ็กคะแนน)
    );

    public override void OnNetworkSpawn()
    {
        if (ScoreboardManager.Instance != null)
        {
            ScoreboardManager.Instance.RefreshScoreboard();
        }
        currentScore.OnValueChanged += (int previousValue, int newValue) =>
        {
            if (ScoreboardManager.Instance != null)
            {
                ScoreboardManager.Instance.RefreshScoreboard();
            }
        };
    }

    
    public void OnClickSmashButton()
    {
        if (!IsOwner) return;

        if (GameManager.Instance != null && !GameManager.Instance.isGameActive.Value)
        {
            Debug.Log("Time is up! Cannot smash anymore.");
            return;
        }

        SubmitScoreServerRpc();
    }

    [ServerRpc]
    private void SubmitScoreServerRpc()
    {
        currentScore.Value += 1;
    }
}