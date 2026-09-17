using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// In-world player object. Receives identity data from the lobby player on spawn.
/// </summary>
public class PlayerGameController : NetworkBehaviour
{
    [SyncVar] public int _ConnectionID;
    [SyncVar] public int _PlayerIdNumber;
    [SyncVar] public ulong _PlayerSteamID;
    [SyncVar] public string _PlayerName;

    [SerializeField, Tooltip("Disabled on the prefab, enabled only for the local player.")]
    private PlayerInput _PlayerInput;

    void Awake()
    {
        if (_PlayerInput == null) _PlayerInput = GetComponent<PlayerInput>();
        _PlayerInput.enabled = false;
    }

    /// <summary>
    /// Enables input only on the locally owned player.
    /// </summary>
    public override void OnStartLocalPlayer()
    {
        gameObject.name = "LocalGamePlayer";
        _PlayerInput.enabled = true;
    }
}