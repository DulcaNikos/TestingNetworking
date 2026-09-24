using Steamworks;
using TMPro;
using UnityEngine;

public class LobbyDataEntry : MonoBehaviour
{
    [SerializeField, Tooltip("")]
    private TextMeshProUGUI _LobbyNameText;

    public CSteamID _LobbyID;
    public string _LobbyName;

    public void SetLobbydata()
    {
        _LobbyNameText.text = !string.IsNullOrEmpty(_LobbyName) ? _LobbyName : "Empty";
    }

    public void JoinLobby()
    {
        SteamLobby.Instance.JoinLobby(_LobbyID);
    }
}