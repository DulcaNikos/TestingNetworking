using Steamworks;
using System.Text;
using UnityEngine;

/// <summary>
/// Steam lobby chat. Works for all members regardless of team.
/// Add to your lobby scene or a persistent object.
/// </summary>
public class LobbyChatManager : MonoBehaviour
{
    public static LobbyChatManager Instance { get; private set; }

    // string senderName, string message
    public static event System.Action<string, string> OnMessageReceived;

    private Callback<LobbyChatMsg_t> _ChatCallback;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        _ChatCallback = Callback<LobbyChatMsg_t>.Create(OnLobbyChatMsg);
    }

    private void OnDisable()
    {
        _ChatCallback?.Dispose();
        _ChatCallback = null;
    }

    /// <summary>
    /// Send a message to everyone in the lobby including yourself.
    /// </summary>
    public void SendMessage(string message)
    {
        ulong lobbyID = GetCurrentLobbyID();
        if (lobbyID == 0) return;

        byte[] bytes = Encoding.UTF8.GetBytes(message);
        SteamMatchmaking.SendLobbyChatMsg(new CSteamID(lobbyID), bytes, bytes.Length);
    }

    private void OnLobbyChatMsg(LobbyChatMsg_t callback)
    {
        byte[] buffer = new byte[4096];

        int length = SteamMatchmaking.GetLobbyChatEntry(
            new CSteamID(callback.m_ulSteamIDLobby),
            (int)callback.m_iChatID,
            out CSteamID senderID,
            buffer,
            buffer.Length,
            out EChatEntryType chatType);

        if (chatType != EChatEntryType.k_EChatEntryTypeChatMsg) return;

        string message = Encoding.UTF8.GetString(buffer, 0, length);
        string senderName = SteamFriends.GetFriendPersonaName(senderID);

        OnMessageReceived?.Invoke(senderName, message);
    }

    private ulong GetCurrentLobbyID()
    {
        if (SteamLobbyHost.Instance != null && SteamLobbyHost.Instance.CurrentLobbyID != 0)
        {
            return SteamLobbyHost.Instance.CurrentLobbyID;
        }
        if (SteamLobbyClient.Instance != null)
        {
            return SteamLobbyClient.Instance.CurrentLobbyID;
        }
        return 0;
    }
}