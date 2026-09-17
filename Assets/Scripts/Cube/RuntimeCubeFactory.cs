using Mirror;
using UnityEngine;

/// <summary>
/// Builds the networked cube entirely in code. Used by the server to spawn
/// and by clients (via a spawn handler) to recreate it, guaranteeing identical component order.
/// </summary>
public static class RuntimeCubeFactory
{
    /// <summary>Custom asset id shared by server and clients. Must not collide with other types.</summary>
    public const uint AssetId = 0x43554245; // "CUBE"

    private static Material _Material;
    private static bool _HandlerRegistered;

    /// <summary>
    /// Optional material for the cube. Call on server and clients before spawning.
    /// </summary>
    public static void SetMaterial(Material material)
    {
        _Material = material;
    }

    /// <summary>
    /// Registers the client spawn handler. Call before the client connects.
    /// </summary>
    public static void RegisterClientHandler()
    {
        if (_HandlerRegistered) return;
        NetworkClient.RegisterSpawnHandler(AssetId, SpawnOnClient, UnspawnOnClient);
        _HandlerRegistered = true;
    }

    /// <summary>
    /// Removes the client spawn handler.
    /// </summary>
    public static void UnregisterClientHandler()
    {
        if (!_HandlerRegistered) return;
        NetworkClient.UnregisterSpawnHandler(AssetId);
        _HandlerRegistered = false;
    }

    /// <summary>
    /// Creates the cube INACTIVE with all components attached.
    /// The caller must activate it so NetworkIdentity sees every NetworkBehaviour in Awake.
    /// </summary>
    public static GameObject Build(Vector3 position, Quaternion rotation)
    {
        // CreatePrimitive gives MeshFilter, MeshRenderer and BoxCollider
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.SetActive(false);
        cube.name = "RuntimeCube";
        cube.transform.SetPositionAndRotation(position, rotation);

        if (_Material != null)
        {
            cube.GetComponent<MeshRenderer>().sharedMaterial = _Material;
        }

        Rigidbody rigidbody = cube.AddComponent<Rigidbody>();
        rigidbody.isKinematic = true;
        rigidbody.useGravity = false;

        cube.AddComponent<NetworkIdentity>();

        // Order of NetworkBehaviours below must never differ between server and client
        NetworkRigidbodyReliable networkRigidbody = cube.AddComponent<NetworkRigidbodyReliable>();
        networkRigidbody.target = cube.transform; // Reset()/OnValidate don't run for runtime components
        networkRigidbody.syncDirection = SyncDirection.ServerToClient;
        networkRigidbody.syncPosition = true;
        networkRigidbody.syncRotation = true;
        networkRigidbody.syncScale = false;
        networkRigidbody.coordinateSpace = CoordinateSpace.World;

        cube.AddComponent<CubeMover>();

        return cube;
    }

    /// <summary>
    /// Client-side spawn handler. Mirror applies netId and SyncVars after this returns.
    /// </summary>
    private static GameObject SpawnOnClient(SpawnMessage msg)
    {
        GameObject cube = Build(msg.position, msg.rotation);
        cube.SetActive(true);
        return cube;
    }

    /// <summary>
    /// Client-side unspawn handler.
    /// </summary>
    private static void UnspawnOnClient(GameObject spawned)
    {
        Object.Destroy(spawned);
    }
}