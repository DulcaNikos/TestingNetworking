using UnityEngine;
using Mirror;
using Steamworks;
using System.Collections.Generic;
using TMPro;

public class SteamLobby : MonoBehaviour
{
    public static SteamLobby Instance;

    protected Callback<LobbyCreated_t> _LobbyCreated;
    protected Callback<GameLobbyJoinRequested_t> _JoinRequested;
    protected Callback<LobbyEnter_t> _LobbyEntered;

    //Lobbies Callbacks
    protected Callback<LobbyMatchList_t> _LobbyList;
    protected Callback<LobbyDataUpdate_t> _LobbyDataUpdated;

    private ulong _CurrentLobbyID;
    private const string HostAddressKey = "HostAddress";
    private CustomNetworkManager networkManager;
    private List<CSteamID> _LobbyIDs = new List<CSteamID>();

    public ulong CurrentLobbyID => _CurrentLobbyID;
    public List<CSteamID> LobbyIDs => _LobbyIDs;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    private void Start()
    {
        if (!SteamManager.Initialized)
        {
            return;
        }

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

    public void JoinLobby(CSteamID _lobbyID)
    {
        SteamMatchmaking.JoinLobby(_lobbyID);
    }

    public void GetLobbiesList()
    {
        if (_LobbyIDs.Count > 0)
        {
            _LobbyIDs.Clear();
        }
        SteamMatchmaking.AddRequestLobbyListResultCountFilter(20);
        SteamMatchmaking.RequestLobbyList();
    }

    private void OnLobbyCreated(LobbyCreated_t _callback)
    {
        if (_callback.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogError("Failed to create lobby: " + _callback.m_eResult);
            return;
        }

        Debug.Log("Lobby successfully created. Lobby ID: " + _callback.m_ulSteamIDLobby);
        networkManager.StartHost();
        SteamMatchmaking.SetLobbyData(new CSteamID(_callback.m_ulSteamIDLobby), HostAddressKey, SteamUser.GetSteamID().ToString());
        SteamMatchmaking.SetLobbyData(new CSteamID(_callback.m_ulSteamIDLobby), "name", SteamFriends.GetPersonaName().ToString() + " 'S LOBBY");
    }

    private void OnJoinRequested(GameLobbyJoinRequested_t _callback)
    {
        Debug.Log("Join request received for lobby: " + _callback.m_steamIDLobby);
        SteamMatchmaking.JoinLobby(_callback.m_steamIDLobby);
        // if (NetworkClient.isConnected || NetworkClient.active)
        // {
        //     Debug.Log("NetworkClient is active or connected. Disconnecting beforfe joining new lobby");
        //     NetworkManager.singleton.StopClient();
        //     NetworkClient.Shutdown();
        // }

    }

    private void OnLobbyEntered(LobbyEnter_t _callback)
    {
        _CurrentLobbyID = _callback.m_ulSteamIDLobby;

        //Clients
        if (NetworkServer.active)
        {
            Debug.Log("Already in a lobby as a host. Ignoring join request");
            return;
        }

        string hostAddress = SteamMatchmaking.GetLobbyData(new CSteamID(_callback.m_ulSteamIDLobby), HostAddressKey);
        networkManager.networkAddress = hostAddress;
        Debug.Log("Entered lobby: " + _callback.m_ulSteamIDLobby);
        networkManager.StartClient();

        // if (_callback.m_EChatRoomEnterResponse != (uint)EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
        // {
        //     Debug.LogError($"Failed to enter lobby: {(EChatRoomEnterResponse)_callback.m_EChatRoomEnterResponse}");
        //     _CurrentLobbyID = 0;
        //     return;
        // }


        // if (string.IsNullOrEmpty(hostAddress))
        // {
        //     Debug.LogError("Lobby has no host address — is the host still running?");
        //     SteamMatchmaking.LeaveLobby(new CSteamID(_CurrentLobbyID));
        //     _CurrentLobbyID = 0;
        //     return;
        // }
    }

    private void OnGetLobbyList(LobbyMatchList_t _result)
    {
        if (LobbiesListManager.Instance.ListOfLobbies.Count > 0)
        {
            LobbiesListManager.Instance.DestroyLobbies();
        }

        CSteamID lobbyID;
        for (int i = 0; i < _result.m_nLobbiesMatching; i++)
        {
            lobbyID = SteamMatchmaking.GetLobbyByIndex(i);
            _LobbyIDs.Add(lobbyID);
            SteamMatchmaking.RequestLobbyData(lobbyID);
        }
    }

    private void OnGetLobbyData(LobbyDataUpdate_t _result)
    {
        LobbiesListManager.Instance.DisplayLobbies(_LobbyIDs, _result);
    }

}