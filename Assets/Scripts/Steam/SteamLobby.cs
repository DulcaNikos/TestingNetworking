using UnityEngine;
using Mirror;
using Steamworks;
using System.Collections.Generic;

public class SteamLobby : MonoBehaviour
{
    public static SteamLobby Instance;

    protected Callback<LobbyCreated_t> _LobbyCreated;
    protected Callback<GameLobbyJoinRequested_t> _JoinRequested;
    protected Callback<LobbyEnter_t> _LobbyEntered;
    protected Callback<LobbyMatchList_t> _LobbyList;
    protected Callback<LobbyDataUpdate_t> _LobbyDataUpdated;

    private ulong _CurrentLobbyID;
    private const string HostAddressKey = "HostAddress";
    private CustomNetworkManager networkManager;
    private List<CSteamID> _LobbyIDs = new List<CSteamID>();
    private bool _IsHosting;

    public ulong CurrentLobbyID => _CurrentLobbyID;
    public List<CSteamID> LobbyIDs => _LobbyIDs;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    private void Start()
    {
        if (!SteamManager.Initialized) return;

        networkManager = GetComponent<CustomNetworkManager>();

        _LobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        _JoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnJoinRequested);
        _LobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        _LobbyList = Callback<LobbyMatchList_t>.Create(OnGetLobbyList);
        _LobbyDataUpdated = Callback<LobbyDataUpdate_t>.Create(OnGetLobbyData);
    }

    public void HostLobby()
    {
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, networkManager.maxConnections);
    }

    public void JoinLobby(CSteamID lobbyID)
    {
        SteamMatchmaking.JoinLobby(lobbyID);
    }

    public void GetLobbiesList()
    {
        _LobbyIDs.Clear();
        SteamMatchmaking.AddRequestLobbyListResultCountFilter(20);
        SteamMatchmaking.RequestLobbyList();
    }

    public void GetFriendsLobbies()
    {
        _LobbyIDs.Clear();

        int friendCount = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
        for (int i = 0; i < friendCount; i++)
        {
            CSteamID friendSteamID = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);

            if (!SteamFriends.GetFriendGamePlayed(friendSteamID, out FriendGameInfo_t gameInfo)) continue;
            if (gameInfo.m_gameID.AppID() != SteamUtils.GetAppID()) continue;
            if (!gameInfo.m_steamIDLobby.IsValid()) continue;
            if (_LobbyIDs.Contains(gameInfo.m_steamIDLobby)) continue;

            _LobbyIDs.Add(gameInfo.m_steamIDLobby);
            SteamMatchmaking.RequestLobbyData(gameInfo.m_steamIDLobby);
        }
    }

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogError("Failed to create lobby: " + callback.m_eResult);
            _IsHosting = false;
            return;
        }

        _IsHosting = true;
        networkManager.StartHost();
        SteamMatchmaking.SetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), HostAddressKey, SteamUser.GetSteamID().ToString());
        SteamMatchmaking.SetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), "name", SteamFriends.GetPersonaName() + "'S LOBBY");
    }

    private void OnJoinRequested(GameLobbyJoinRequested_t callback)
    {
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        _CurrentLobbyID = callback.m_ulSteamIDLobby;

        if (_IsHosting || NetworkServer.active) return;

        string hostAddress = SteamMatchmaking.GetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), HostAddressKey);
        if (string.IsNullOrEmpty(hostAddress))
        {
            Debug.LogError("Host address missing from lobby data.");
            return;
        }

        networkManager.networkAddress = hostAddress;
        networkManager.StartClient();
    }

    private void OnGetLobbyList(LobbyMatchList_t result)
    {
        if (LobbiesListManager.Instance.ListOfLobbies.Count > 0)
        {
            LobbiesListManager.Instance.DestroyLobbies();
        }

        for (int i = 0; i < result.m_nLobbiesMatching; i++)
        {
            CSteamID lobbyID = SteamMatchmaking.GetLobbyByIndex(i);
            _LobbyIDs.Add(lobbyID);
            SteamMatchmaking.RequestLobbyData(lobbyID);
        }
    }

    private void OnGetLobbyData(LobbyDataUpdate_t result)
    {
        LobbiesListManager.Instance.DisplayLobbies(_LobbyIDs, result);
    }
}