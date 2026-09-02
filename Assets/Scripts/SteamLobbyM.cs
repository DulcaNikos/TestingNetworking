using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Collections;
using Steamworks;
using System;

using TMPro;
namespace SteamLobbyN
{
    public class SteamLobbyM : MonoBehaviour
    {
        public static SteamLobbyM Instance;
        public ulong lobbyID;
        public NetworkManager networkManager;
        public PanelSwaper panelSwaper;
        public TMP_InputField lobbyIdInput;

        protected Callback<LobbyCreated_t> lobbyCreated;
        protected Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
        protected Callback<LobbyEnter_t> lobbyEntered;

        public event Action<ulong> OnLobbyReady;

        private const string HostAddressKey = "HostAddress";

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        void Start()
        {
            networkManager = GetComponent<NetworkManager>();
            if (!SteamManager.Initialized)
            {
                Debug.LogError("Steam is not initialized. Make sure to run this game in the steam environment");
                return;
            }
            panelSwaper.gameObject.SetActive(true);
            lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
            gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
            lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        }

        public void HostLobby()
        {
            SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, networkManager.maxConnections);
        }

        private void OnLobbyCreated(LobbyCreated_t _callback)
        {
            if (_callback.m_eResult != EResult.k_EResultOK)
            {
                Debug.LogError("Failed to create lobby: " + _callback.m_eResult);
            }

            Debug.Log("Lobby successfully created. Lobby ID: " + _callback.m_ulSteamIDLobby);
            networkManager.StartHost();
            SteamMatchmaking.SetLobbyData(new CSteamID(_callback.m_ulSteamIDLobby), HostAddressKey, SteamUser.GetSteamID().ToString());
            lobbyID = _callback.m_ulSteamIDLobby;
            OnLobbyReady?.Invoke(lobbyID);
        }

        private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t _callback)
        {
            Debug.Log("Join request received for lobby: " + _callback.m_steamIDLobby);
            if (NetworkClient.isConnected || NetworkClient.active)
            {
                Debug.Log("NetworkClient is active or connected. Disconnecting beforfe joining new lobby");
                NetworkManager.singleton.StopClient();
                NetworkClient.Shutdown();
            }

            SteamMatchmaking.JoinLobby(_callback.m_steamIDLobby);
        }

        private void OnLobbyEntered(LobbyEnter_t _callback)
        {
            if (_callback.m_EChatRoomEnterResponse != (uint)EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
            {
                Debug.LogError($"Failed to enter lobby: {(EChatRoomEnterResponse)_callback.m_EChatRoomEnterResponse}");
                lobbyID = 0;
                return;
            }

            if (NetworkServer.active)
            {
                Debug.Log("Already in a lobby as a host. Ignoring join request");
                return;
            }

            lobbyID = _callback.m_ulSteamIDLobby;
            string hostAddress = SteamMatchmaking.GetLobbyData(new CSteamID(_callback.m_ulSteamIDLobby), HostAddressKey);

            if (string.IsNullOrEmpty(hostAddress))
            {
                Debug.LogError("Lobby has no host address — is the host still running?");
                SteamMatchmaking.LeaveLobby(new CSteamID(lobbyID));
                lobbyID = 0;
                return;
            }

            networkManager.networkAddress = hostAddress;
            Debug.Log("Entered lobby: " + _callback.m_ulSteamIDLobby);
            networkManager.StartClient();
            panelSwaper.SwapPanel("LobbyPanel");
        }

        public void LeaveLobby()
        {
            CSteamID currentOwner = SteamMatchmaking.GetLobbyOwner(new CSteamID(lobbyID));
            CSteamID me = SteamUser.GetSteamID();
            CSteamID lobby = new CSteamID(lobbyID);
            List<CSteamID> members = new List<CSteamID>();

            int count = SteamMatchmaking.GetNumLobbyMembers(lobby);

            for (int i = 0; i < count; i++)
            {
                members.Add(SteamMatchmaking.GetLobbyMemberByIndex(lobby, i));
            }
            if (NetworkServer.active && currentOwner == me)
            {
                SteamMatchmaking.SetLobbyJoinable(lobby, false);
            }

            if (lobbyID != 0)
            {
                SteamMatchmaking.LeaveLobby(lobby);
                lobbyID = 0;
            }

            if (NetworkServer.active && currentOwner == me)
                NetworkManager.singleton.StopHost();
            else if (NetworkClient.isConnected)
                NetworkManager.singleton.StopClient();

            panelSwaper.gameObject.SetActive(true);
            this.gameObject.SetActive(true);
            panelSwaper.SwapPanel("MainPanel");
        }

        public void JoinLobbyFromInput()
        {
            if (!SteamManager.Initialized)
            {
                Debug.LogError("Steam is not initialized.");
                return;
            }

            string raw = lobbyIdInput.text.Trim();

            if (!ulong.TryParse(raw, out ulong id))
            {
                Debug.LogWarning($"'{raw}' is not a valid lobby ID.");
                // show a UI message here
                return;
            }

            CSteamID target = new CSteamID(id);

            if (!target.IsLobby())
            {
                Debug.LogWarning($"{id} is not a lobby ID.");
                return;
            }

            if (NetworkClient.isConnected || NetworkClient.active)
            {
                NetworkManager.singleton.StopClient();
                NetworkClient.Shutdown();
            }

            if (lobbyID != 0)
            {
                SteamMatchmaking.LeaveLobby(new CSteamID(lobbyID));
                lobbyID = 0;
            }

            Debug.Log($"Attempting to join lobby {id}");
            SteamMatchmaking.JoinLobby(target);
        }

        public void HandleForcedExit()
        {
            if (lobbyID != 0)
            {
                SteamMatchmaking.LeaveLobby(new CSteamID(lobbyID));
                lobbyID = 0;
            }
        }
    }
}

