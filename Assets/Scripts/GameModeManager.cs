using UnityEngine;
using Mirror;
using Mirror.FizzySteam;

/// <summary>
/// Enables only the components needed for the chosen role.
/// Use the debug toggles to disable individual managers and observe errors.
/// </summary>
public class GameModeManager : MonoBehaviour
{
    public static GameModeManager Instance { get; private set; }

    [Header("Managers")]
    [SerializeField] private SteamManager _SteamManager;
    [SerializeField] private FizzySteamworks _FizzySteamworks;
    [SerializeField] private CustomNetworkManager _CustomNetworkManager;
    [SerializeField] private TeamInterestManagement _TeamInterestManagement;
    [SerializeField] private SteamLobbyHost _SteamLobbyHost;
    [SerializeField] private SteamLobbyClient _SteamLobbyClient;
    [SerializeField] private LobbiesListManager _LobbiesListManager;

    [Header("UI")]
    [SerializeField] private GameObject _MainMenuUI;
    [SerializeField] private GameObject _LobbyBrowserUI;

    [Header("Debug — SteamManager")]
    [Tooltip("SteamManager is always required. Disabling will break everything on both sides.")]
    [SerializeField] private bool _SteamManagerForHost = true;
    [SerializeField] private bool _SteamManagerForClient = true;

    [Header("Debug — Fizzy Steamworks (transport)")]
    [Tooltip("Without this the host can't accept connections. " +
             "Expected: clients time out immediately.")]
    [SerializeField] private bool _FizzyForHost = true;
    [Tooltip("Without this the client can't send or receive data. " +
             "Expected: connection attempt hangs or times out.")]
    [SerializeField] private bool _FizzyForClient = true;

    [Header("Debug — CustomNetworkManager")]
    [Tooltip("Without this on host: StartHost never called, no players spawn, " +
             "no scene change possible.")]
    [SerializeField] private bool _NetworkManagerForHost = true;
    [Tooltip("Without this on client: StartClient never called, " +
             "no objects received, scene never loads.")]
    [SerializeField] private bool _NetworkManagerForClient = true;

    [Header("Debug — TeamInterestManagement")]
    [Tooltip("Without this on host: all spawned objects sent to all clients. " +
             "Wrong team sees enemy pickups.")]
    [SerializeField] private bool _TeamInterestForHost = true;
    [Tooltip("Client-side this does nothing — safe to leave off. " +
             "No expected errors.")]
    [SerializeField] private bool _TeamInterestForClient = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Everything starts disabled until a role is chosen
        SetEnabled(_SteamManager, false);
        SetEnabled(_FizzySteamworks, false);
        SetEnabled(_CustomNetworkManager, false);
        SetEnabled(_TeamInterestManagement, false);
        SetEnabled(_SteamLobbyHost, false);
        SetEnabled(_SteamLobbyClient, false);
        SetEnabled(_LobbiesListManager, false);
    }

    #region Button handlers
    /// <summary>
    /// Called by the Host button.
    /// </summary>
    public void OnHostPressed()
    {
        SetEnabled(_SteamLobbyClient, false);
        SetEnabled(_LobbiesListManager, false);

        SetEnabled(_SteamManager, _SteamManagerForHost);
        SetEnabled(_FizzySteamworks, _FizzyForHost);
        SetEnabled(_CustomNetworkManager, _NetworkManagerForHost);
        SetEnabled(_TeamInterestManagement, _TeamInterestForHost);

        SetEnabled(_SteamLobbyHost, true); // always needs to be on to host

        LogDisabledWarnings(isHost: true);

        _MainMenuUI.SetActive(false);

        if (_SteamLobbyHost != null && _SteamLobbyHost.enabled)
        {
            _SteamLobbyHost.HostLobby();
        }
    }

    /// <summary>
    /// Called by the Browse Lobbies button.
    /// </summary>
    public void OnBrowsePressed()
    {
        SetEnabled(_SteamLobbyHost, false);

        SetEnabled(_SteamManager, _SteamManagerForClient);
        SetEnabled(_FizzySteamworks, _FizzyForClient);
        SetEnabled(_CustomNetworkManager, _NetworkManagerForClient);
        SetEnabled(_TeamInterestManagement, _TeamInterestForClient);

        SetEnabled(_SteamLobbyClient, true); // always needs to be on to browse
        SetEnabled(_LobbiesListManager, true);

        LogDisabledWarnings(isHost: false);

        _MainMenuUI.SetActive(false);
        _LobbyBrowserUI.SetActive(true);

        if (_SteamLobbyClient != null && _SteamLobbyClient.enabled)
        {
            _SteamLobbyClient.OpenLobbyList();
        }
    }
    #endregion

    #region Helpers
    private void SetEnabled(Behaviour component, bool active)
    {
        if (component != null) component.enabled = active;
    }

    private void LogDisabledWarnings(bool isHost)
    {
        if (isHost)
        {
            if (!_NetworkManagerForHost)
                Debug.LogWarning("[GameModeManager] CustomNetworkManager disabled for host. " +
                                 "StartHost will not be called. No players will spawn.");

            if (!_TeamInterestForHost)
                Debug.LogWarning("[GameModeManager] TeamInterestManagement disabled for host. " +
                                 "All objects will be sent to all clients regardless of team.");

            if (!_FizzyForHost)
                Debug.LogWarning("[GameModeManager] Fizzy Steamworks disabled for host. " +
                                 "Clients will fail to connect.");

            if (!_SteamManagerForHost)
                Debug.LogWarning("[GameModeManager] SteamManager disabled for host. " +
                                 "All Steam API calls will fail.");
        }
        else
        {
            if (!_NetworkManagerForClient)
                Debug.LogWarning("[GameModeManager] CustomNetworkManager disabled for client. " +
                                 "StartClient will not be called. No objects will be received.");

            if (!_FizzyForClient)
                Debug.LogWarning("[GameModeManager] Fizzy Steamworks disabled for client. " +
                                 "Connection will time out.");

            if (!_SteamManagerForClient)
                Debug.LogWarning("[GameModeManager] SteamManager disabled for client. " +
                                 "All Steam API calls will fail.");
        }
    }
    #endregion
}