using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;
using System.Collections.Generic;
using Steamworks;

public class CustomNetworkManager : NetworkManager
{
    [SerializeField, Tooltip("")]
    private PlayerObjectController _GamePlayerPrefab;

    [SerializeField, Tooltip("Spawned in the gameplay scene. Must be in Registered Spawnable Prefabs.")]
    private PlayerGameController _GameplayPlayerPrefab;

    [SerializeField, Tooltip("")]
    private string _LobbySceneName = "Lobby";

    [SerializeField, Tooltip("")]
    private string _GameplaySceneName = "Game";

    public List<PlayerObjectController> _GamePlayers { get; } = new List<PlayerObjectController>();

    public void StartGame(string _sceneName)
    {
        ServerChangeScene(_sceneName);
    }

    //Spawn lobby player
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        if (SceneManager.GetActiveScene().name == _LobbySceneName)
        {
            PlayerObjectController GamePlayerInstance = Instantiate(_GamePlayerPrefab);

            GamePlayerInstance._ConnectionID = conn.connectionId;
            GamePlayerInstance._PlayerIdNumber = _GamePlayers.Count + 1;
            GamePlayerInstance._PlayerSteamID = (ulong)SteamMatchmaking.GetLobbyMemberByIndex((CSteamID)SteamLobby.Instance.CurrentLobbyID, _GamePlayers.Count);

            NetworkServer.AddPlayerForConnection(conn, GamePlayerInstance.gameObject);
        }
    }

    // Spawn gameplay player

    /// <summary>
    /// Called on the server when a client has finished loading a scene.
    /// In the gameplay scene, replaces the lobby player with the gameplay player.
    /// </summary>
    public override void OnServerReady(NetworkConnectionToClient conn)
    {
        base.OnServerReady(conn);

        if (SceneManager.GetActiveScene().name != _GameplaySceneName) return;
        if (conn.identity == null) return;

        PlayerObjectController lobbyPlayer = conn.identity.GetComponent<PlayerObjectController>();
        if (lobbyPlayer == null) return; // already replaced

        SpawnGameplayPlayer(conn, lobbyPlayer);
    }

    /// <summary>
    /// Spawns the gameplay player at a start position and copies lobby data onto it.
    /// </summary>
    private void SpawnGameplayPlayer(NetworkConnectionToClient conn, PlayerObjectController lobbyPlayer)
    {
        Transform startPos = GetStartPosition();
        Vector3 position = startPos != null ? startPos.position : Vector3.zero;
        Quaternion rotation = startPos != null ? startPos.rotation : Quaternion.identity;

        PlayerGameController gamePlayer = Instantiate(_GameplayPlayerPrefab, position, rotation);

        // Set before replacing so the SyncVars are included in the spawn message
        gamePlayer._ConnectionID = lobbyPlayer._ConnectionID;
        gamePlayer._PlayerIdNumber = lobbyPlayer._PlayerIdNumber;
        gamePlayer._PlayerSteamID = lobbyPlayer._PlayerSteamID;
        gamePlayer._PlayerName = lobbyPlayer._PlayerName;

        NetworkServer.ReplacePlayerForConnection(conn, gamePlayer.gameObject, ReplacePlayerOptions.Destroy);
    }
}
