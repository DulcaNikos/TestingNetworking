using TMPro;
using Steamworks;
using UnityEngine;

public class JoinByCodeUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField _CodeInput;

    public void OnJoinPressed()
    {
        string code = _CodeInput.text.Trim().ToUpper();
        if (code.Length != 6)
        {
            Debug.LogWarning("Invalid code length");
            return;
        }

        ulong lobbyID = LobbyCodeHelper.FromCode(code);
        if (lobbyID == 0)
        {
            Debug.LogWarning("Invalid lobby code");
            return;
        }

        JoinViaCode(new CSteamID(lobbyID));
    }

    private void JoinViaCode(CSteamID lobbyID)
    {
        SteamMatchmaking.RequestLobbyData(lobbyID);
        SteamLobbyClient.Instance.JoinLobby(lobbyID);
    }
}