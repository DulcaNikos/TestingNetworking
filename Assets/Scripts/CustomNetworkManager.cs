using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;
using System.Collections.Generic;
using Steamworks;

public class CustomNetworkManager : NetworkManager
{
    [SerializeField, Tooltip("")]
    private PlayerObjectController _GamePlayerPrefab;

    public List<PlayerObjectController> _GamePlayers { get; } = new List<PlayerObjectController>();

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        if (SceneManager.GetActiveScene().name == "Lobby")
        {
            PlayerObjectController GamePlayerInstance = Instantiate(_GamePlayerPrefab);

            GamePlayerInstance._ConnectionID = conn.connectionId;
            GamePlayerInstance._PlayerIdNumber = _GamePlayers.Count + 1;
            GamePlayerInstance._PlayerSteamID = (ulong)SteamMatchmaking.GetLobbyMemberByIndex((CSteamID)SteamLobby.Instance.CurrentLobbyID, _GamePlayers.Count);

            NetworkServer.AddPlayerForConnection(conn, GamePlayerInstance.gameObject);
        }
    }

    public void StartGame(string _sceneName)
    {
        ServerChangeScene(_sceneName);
    }
}
