using Mirror;
using Steamworks;
using UnityEngine;

/// <summary>
/// Host-only Steam logic. Enabled by GameModeManager when the player chooses to host.
/// </summary>
public class SteamLobbyHost : MonoBehaviour
{
    public static SteamLobbyHost Instance { get; private set; }

    [SerializeField, Tooltip("Lobby visibility set in Inspector.")]
    private ELobbyType _LobbyType = ELobbyType.k_ELobbyTypePublic;

    private const string HostAddressKey = "HostAddress";
    private CustomNetworkManager _NetworkManager;
    private ulong _CurrentLobbyID;

    public ulong CurrentLobbyID => _CurrentLobbyID;

    protected Callback<LobbyCreated_t> _LobbyCreated;
    protected Callback<GameLobbyJoinRequested_t> _JoinRequested;
    protected Callback<LobbyEnter_t> _LobbyEntered;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        _NetworkManager = GetComponent<CustomNetworkManager>();
        _LobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        _JoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnJoinRequested);
        _LobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
    }

    private void OnDisable()
    {
        _LobbyCreated?.Dispose();
        _JoinRequested?.Dispose();
        _LobbyEntered?.Dispose();
    }

    public void HostLobby()
    {
        SteamMatchmaking.CreateLobby(_LobbyType, _NetworkManager.maxConnections);
    }

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogError("Failed to create lobby: " + callback.m_eResult);
            return;
        }

        _CurrentLobbyID = callback.m_ulSteamIDLobby;
        CSteamID lobbyId = new CSteamID(callback.m_ulSteamIDLobby);

        SteamMatchmaking.SetLobbyJoinable(lobbyId, true);
        SteamMatchmaking.SetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), HostAddressKey, SteamUser.GetSteamID().ToString());
        SteamMatchmaking.SetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), "name", SteamFriends.GetPersonaName() + "'S LOBBY");

        // Lets Steam know how friends can join you (shows "Join Game" / enables invites)
        SteamFriends.SetRichPresence("connect", "+connect_lobby " + callback.m_ulSteamIDLobby);

        _NetworkManager.StartHost();
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        _CurrentLobbyID = callback.m_ulSteamIDLobby;
        // Host ignores this — already running
    }

    /// <summary>
    /// Handles Steam overlay invites received while hosting.
    /// </summary>
    private void OnJoinRequested(GameLobbyJoinRequested_t callback)
    {
        // Host ignores join requests — already in a session
        Debug.Log("Join request ignored — already hosting.");
    }
}