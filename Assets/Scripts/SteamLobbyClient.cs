using Mirror;
using Steamworks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Client-only Steam logic. Enabled by GameModeManager when the player browses or joins.
/// </summary>
public class SteamLobbyClient : MonoBehaviour
{
    public static SteamLobbyClient Instance { get; private set; }

    private const string HostAddressKey = "HostAddress";
    private CustomNetworkManager _NetworkManager;
    private List<CSteamID> _LobbyIDs = new List<CSteamID>();
    private bool _JoiningLobby;

    [SerializeField, Tooltip("Seconds between auto-refreshes while the lobby list is open.")]
    private float _RefreshInterval = 10f;

    private bool _ListOpen;
    private Coroutine _RefreshCoroutine;

    public List<CSteamID> LobbyIDs => _LobbyIDs;

    protected Callback<LobbyEnter_t> _LobbyEntered;
    protected Callback<LobbyMatchList_t> _LobbyList;
    protected Callback<LobbyDataUpdate_t> _LobbyDataUpdated;
    protected Callback<GameLobbyJoinRequested_t> _JoinRequested;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        _NetworkManager = GetComponent<CustomNetworkManager>();
        _LobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        _LobbyList = Callback<LobbyMatchList_t>.Create(OnGetLobbyList);
        _LobbyDataUpdated = Callback<LobbyDataUpdate_t>.Create(OnGetLobbyData);
        _JoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnJoinRequested);
    }

    private void OnDisable()
    {
        _LobbyEntered?.Dispose();
        _LobbyList?.Dispose();
        _LobbyDataUpdated?.Dispose();
        _JoinRequested?.Dispose();
        StopRefresh();
    }

    #region  Lobby list
    public void OpenLobbyList()
    {
        _ListOpen = true;
        RefreshLobbies();

        if (_RefreshCoroutine == null)
        {
            _RefreshCoroutine = StartCoroutine(AutoRefresh());
        }
    }

    public void CloseLobbyList()
    {
        _ListOpen = false;
        StopRefresh();
    }

    private void StopRefresh()
    {
        if (_RefreshCoroutine != null)
        {
            StopCoroutine(_RefreshCoroutine);
            _RefreshCoroutine = null;
        }
    }

    private IEnumerator AutoRefresh()
    {
        while (_ListOpen)
        {
            yield return new WaitForSeconds(_RefreshInterval);
            if (_ListOpen && !_JoiningLobby)
            {
                RefreshLobbies();
            }
        }
    }

    private void RefreshLobbies()
    {
        _LobbyIDs.Clear();
        SteamMatchmaking.AddRequestLobbyListResultCountFilter(20);
        SteamMatchmaking.RequestLobbyList();
    }

    private void GetFriendsLobbies()
    {
        _LobbyIDs.Clear();

        int friendCount = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
        for (int i = 0; i < friendCount; i++)
        {
            CSteamID friendID = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);
            if (!SteamFriends.GetFriendGamePlayed(friendID, out FriendGameInfo_t gameInfo)) continue;
            if (gameInfo.m_gameID.AppID() != SteamUtils.GetAppID()) continue;
            if (!gameInfo.m_steamIDLobby.IsValid()) continue;
            if (_LobbyIDs.Contains(gameInfo.m_steamIDLobby)) continue;
            _LobbyIDs.Add(gameInfo.m_steamIDLobby);
            SteamMatchmaking.RequestLobbyData(gameInfo.m_steamIDLobby);
        }
    }

    private void OnJoinRequested(GameLobbyJoinRequested_t callback)
    {
        JoinLobby(callback.m_steamIDLobby);
    }

    public void JoinLobby(CSteamID lobbyID)
    {
        _JoiningLobby = true;
        StopRefresh();
        LobbiesListManager.Instance.DestroyLobbies();
        LobbiesListManager.Instance.HideMenu();
        SteamMatchmaking.JoinLobby(lobbyID);
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        _JoiningLobby = false;

        if (NetworkServer.active) return;

        string hostAddress = SteamMatchmaking.GetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), HostAddressKey);
        if (string.IsNullOrEmpty(hostAddress))
        {
            Debug.LogError("Host address missing from lobby data.");
            return;
        }

        _NetworkManager.networkAddress = hostAddress;
        _NetworkManager.StartClient();
    }

    private void OnGetLobbyList(LobbyMatchList_t result)
    {
        if (_JoiningLobby) return;

        LobbiesListManager.Instance.DestroyLobbies();

        for (int i = 0; i < result.m_nLobbiesMatching; i++)
        {
            CSteamID lobbyID = SteamMatchmaking.GetLobbyByIndex(i);
            _LobbyIDs.Add(lobbyID);
            SteamMatchmaking.RequestLobbyData(lobbyID);
        }
    }

    private void OnGetLobbyData(LobbyDataUpdate_t result)
    {
        if (_JoiningLobby) return;
        LobbiesListManager.Instance.DisplayLobbies(_LobbyIDs, result);
    }
    #endregion
}