using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Scene-placed spawner. Any client (or only the host) can press Space to spawn a cube
/// in front of their own player.
/// </summary>
public class CubeSpawner : NetworkBehaviour
{
    [SerializeField, Tooltip("Must be in Registered Spawnable Prefabs.")]
    private GameObject _CubePrefab;

    [SerializeField, Tooltip("If true, only the host can spawn cubes.")]
    private bool _HostOnly = false;

    [SerializeField, Tooltip("")]
    private float _SpawnDistance = 2f;

    [SerializeField, Tooltip("")]
    private float _SpawnHeight = 0.5f;

    [SerializeField, Tooltip("Per-player cooldown, enforced on the server.")]
    private float _SpawnCooldown = 0.5f;

    private readonly Dictionary<int, float> _NextSpawnTimes = new Dictionary<int, float>();

    /// <summary>
    /// Runs on every client; each reads its own keyboard.
    /// </summary>
    private void Update()
    {
        if (!isClient) return;                 // not spawned yet on this client
        if (_HostOnly && !isServer) return;    // UI-side filter, server re-checks
        if (Keyboard.current == null) return;
        if (!Keyboard.current.spaceKey.wasPressedThisFrame) return;

        CmdSpawnCube();
    }

    /// <summary>
    /// Any client may call this. Mirror fills in the sender automatically.
    /// </summary>
    [Command(requiresAuthority = false)]
    private void CmdSpawnCube(NetworkConnectionToClient sender = null)
    {
        if (sender == null || _CubePrefab == null) return;

        // Never trust the client-side check alone
        if (_HostOnly && sender != NetworkServer.localConnection) return;

        float now = Time.time;
        if (_NextSpawnTimes.TryGetValue(sender.connectionId, out float nextTime) && now < nextTime) return;
        _NextSpawnTimes[sender.connectionId] = now + _SpawnCooldown;

        // Spawn in front of the sender's player, or at the spawner if they have none
        Transform origin = sender.identity != null ? sender.identity.transform : transform;

        Vector3 forward = origin.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f)
        {
            forward = Vector3.forward;
        }
        forward.Normalize();

        Vector3 position = origin.position + forward * _SpawnDistance;
        position.y = _SpawnHeight;

        GameObject cube = Instantiate(_CubePrefab, position, Quaternion.LookRotation(forward, Vector3.up));
        NetworkServer.Spawn(cube);
    }
}