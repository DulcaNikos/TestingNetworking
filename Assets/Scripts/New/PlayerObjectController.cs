using UnityEngine;
using Mirror;
using Steamworks;

public class PlayerObjectController : NetworkBehaviour
{
    //PlayerData
    [SyncVar] public int _ConnectionID;
    [SyncVar] public int _PlayerIdNumber;
    [SyncVar] public ulong _PlayerSteamID;
    [SyncVar(hook = nameof(PlayerNameUpdate))] public string _PlayerName;
    [SyncVar(hook = nameof(PlayerReadyUpdate))] public bool _Ready;

    private CustomNetworkManager _Manager;

    private CustomNetworkManager Manager
    {
        get
        {
            if (_Manager != null)
            {
                return _Manager;
            }
            return _Manager = CustomNetworkManager.singleton as CustomNetworkManager;
        }
    }

    private void Start()
    {
        DontDestroyOnLoad(this.gameObject);
    }

    public override void OnStartAuthority()
    {
        CmdSetPlayerName(SteamFriends.GetPersonaName().ToString());
        gameObject.name = "LocalGamePlayer";
        if (LobbyController.Instance == null) return;
        LobbyController.Instance.FindLocalPlayer();
        LobbyController.Instance.UpdateLobbyName();

    }

    public override void OnStartClient()
    {
        Manager._GamePlayers.Add(this);
        if (LobbyController.Instance == null) return;
        LobbyController.Instance.UpdateLobbyName();
        LobbyController.Instance.UpdatePlayerList();
    }

    public override void OnStopClient()
    {
        Manager._GamePlayers.Remove(this);
        if (LobbyController.Instance == null) return;
        LobbyController.Instance.UpdatePlayerList();
    }

    [Command]
    private void CmdSetPlayerName(string _playerName)
    {
        this.PlayerNameUpdate(this._PlayerName, _playerName);
    }

    private void PlayerNameUpdate(string _oldValue, string _newValue)
    {
        if (isServer)
        {
            this._PlayerName = _newValue;
        }
        if (isClient)
        {
            LobbyController.Instance.UpdatePlayerList();
        }
    }

    public void ChangeReady()
    {
        if (isOwned)
        {
            CmdSetPlayerReady();
        }
    }

    [Command]
    private void CmdSetPlayerReady()
    {
        this.PlayerReadyUpdate(this._Ready, !this._Ready);
    }

    private void PlayerReadyUpdate(bool _oldValue, bool _newValue)
    {
        if (isServer)
        {
            this._Ready = _newValue;
        }
        if (isClient)
        {
            LobbyController.Instance.UpdatePlayerList();
        }
    }


    public void CanStartGame(string _sceneName)
    {
        if (isOwned)
        {
            CmdCanStartGame(_sceneName);
        }
    }

    [Command]
    public void CmdCanStartGame(string _sceneName)
    {
        Manager.StartGame(_sceneName);
    }
}
