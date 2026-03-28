using Unity.Netcode;
using UnityEngine;

public class SmashUIManager : MonoBehaviour
{
    public void OnPressSmashButton()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
        {
            var myPlayerObject = NetworkManager.Singleton.LocalClient.PlayerObject;

            if (myPlayerObject != null)
            {
                PlayerScore myScoreScript = myPlayerObject.GetComponent<PlayerScore>();

                if (myScoreScript != null)
                {
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