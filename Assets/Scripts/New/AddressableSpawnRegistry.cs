using System.Collections.Generic;
using System.Threading.Tasks;
using Mirror;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// Loads networked prefabs through Addressables and registers them with Mirror.
/// Must finish loading before hosting or joining.
/// </summary>
public class AddressableSpawnRegistry : MonoBehaviour
{
    public static AddressableSpawnRegistry Instance { get; private set; }

    [SerializeField, Tooltip("Networked prefabs to load. Do NOT also add them to Registered Spawnable Prefabs.")]
    private List<AssetReferenceGameObject> _NetworkPrefabs = new List<AssetReferenceGameObject>();

    private readonly List<AsyncOperationHandle<GameObject>> _Handles = new List<AsyncOperationHandle<GameObject>>();
    private readonly Dictionary<string, GameObject> _LoadedByKey = new Dictionary<string, GameObject>();
    private Task _LoadTask;

    public bool IsLoaded { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Loads all prefabs once. Safe to await multiple times.
    /// </summary>
    public Task LoadAllAsync()
    {
        if (_LoadTask == null)
        {
            _LoadTask = LoadInternalAsync();
        }
        return _LoadTask;
    }

    private async Task LoadInternalAsync()
    {
        foreach (AssetReferenceGameObject reference in _NetworkPrefabs)
        {
            AsyncOperationHandle<GameObject> handle = reference.LoadAssetAsync<GameObject>();
            _Handles.Add(handle);
            GameObject prefab = await handle.Task;

            if (handle.Status != AsyncOperationStatus.Succeeded || prefab == null)
            {
                Debug.LogError($"Failed to load networked prefab {reference.RuntimeKey}");
                continue;
            }

            _LoadedByKey[reference.RuntimeKey.ToString()] = prefab;
        }
        IsLoaded = true;
    }

    /// <summary>
    /// Server-side lookup for instantiating a loaded prefab.
    /// </summary>
    public GameObject GetPrefab(AssetReferenceGameObject reference)
    {
        _LoadedByKey.TryGetValue(reference.RuntimeKey.ToString(), out GameObject prefab);
        return prefab;
    }

    /// <summary>
    /// Registers all loaded prefabs with the Mirror client. Call from NetworkManager.OnStartClient.
    /// </summary>
    public void RegisterWithClient()
    {
        foreach (GameObject prefab in _LoadedByKey.Values)
        {
            NetworkClient.RegisterPrefab(prefab);
        }
    }

    /// <summary>
    /// Unregisters prefabs so a reconnect starts clean. Call from NetworkManager.OnStopClient.
    /// </summary>
    public void UnregisterFromClient()
    {
        foreach (GameObject prefab in _LoadedByKey.Values)
        {
            NetworkClient.UnregisterPrefab(prefab);
        }
    }

    private void OnDestroy()
    {
        foreach (AsyncOperationHandle<GameObject> handle in _Handles)
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }
        _Handles.Clear();
        _LoadedByKey.Clear();

        if (Instance == this)
        {
            Instance = null;
        }
    }
}