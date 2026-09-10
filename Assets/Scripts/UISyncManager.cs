using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class UISyncManager : NetworkBehaviour
{
    #region Singleton

    public static UISyncManager Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        // Guard against clearing a newer instance if one already replaced this object.
        if (Instance == this) Instance = null;
    }

    #endregion

    #region Payload types

    // Plain structs of serializable primitives, so Mirror's codegen can generate readers and
    // writers for them automatically. Used only by the late-join handshake, which has to send
    // whole dictionaries in one message.

    public struct FloatEntry { public string id; public float value; }
    public struct BoolEntry { public string id; public bool value; }
    public struct StringEntry { public string id; public string value; }
    public struct IntEntry { public string id; public int value; }

    #endregion

    #region State and listener tables

    // ---------------------------------------------------------------------------------------
    // WHY THESE ARE STATIC
    //
    // Widgets register in OnEnable, which can run before this manager's NetworkIdentity has
    // spawned (scene load order is not guaranteed, and a client's spawn message arrives some
    // frames after the scene is up). Instance members would require every widget to poll or
    // wait for Instance != null. Static tables sidestep that entirely: registration always
    // succeeds, and the value is delivered later when it arrives.
    //
    // The cost is that statics survive scene loads and survive a host stopping and restarting
    // inside the same editor session, which is why OnStopServer clears them. If you ever run
    // a client and a host in one process, these tables would collide.
    // ---------------------------------------------------------------------------------------

    /// <summary>Authoritative values on the server; a mirror-image cache on each client.</summary>
    static readonly Dictionary<string, float> floats = new Dictionary<string, float>();
    static readonly Dictionary<string, bool> bools = new Dictionary<string, bool>();
    static readonly Dictionary<string, string> strings = new Dictionary<string, string>();
    static readonly Dictionary<string, int> ints = new Dictionary<string, int>();

    /// <summary>
    /// Client-side handlers keyed by the same ids. One handler per id -- registering a second
    /// widget under an existing id silently replaces the first, so ids must be unique.
    /// </summary>
    static readonly Dictionary<string, Action<float>> floatListeners = new Dictionary<string, Action<float>>();
    static readonly Dictionary<string, Action<bool>> boolListeners = new Dictionary<string, Action<bool>>();
    static readonly Dictionary<string, Action<string>> stringListeners = new Dictionary<string, Action<string>>();
    static readonly Dictionary<string, Action<int>> intListeners = new Dictionary<string, Action<int>>();

    /// <summary>
    /// Server-side methods that any client may trigger by name via <see cref="Invoke"/>.
    /// These hold no value -- they are remote triggers, not state.
    /// </summary>
    static readonly Dictionary<string, Action> serverActions = new Dictionary<string, Action>();

    /// <summary>
    /// Wipes the authoritative store when the server shuts down, so a second host session in
    /// the same process does not start with stale values from the first.
    /// </summary>
    public override void OnStopServer()
    {
        base.OnStopServer();
        floats.Clear();
        bools.Clear();
        strings.Clear();
        ints.Clear();
    }

    #endregion

    #region Registration (client side)

    // All four RegisterX methods follow the same shape:
    //   1. store the handler under the id (overwrites any previous handler for that id)
    //   2. if a value for that id has already arrived, invoke the handler immediately
    //
    // Step 2 is what makes registration order-independent. A widget that enables after the
    // state has been received still paints correctly, and a widget that enables before will
    // be painted when ApplyX runs.
    //
    // Static because widgets call these without needing a live Instance.

    /// <summary>Subscribes a handler to float changes for <paramref name="id"/>.</summary>
    public static void RegisterFloat(string id, Action<float> handler)
    {
        floatListeners[id] = handler;
        if (floats.TryGetValue(id, out float v)) handler(v);
    }

    /// <summary>Subscribes a handler to bool changes for <paramref name="id"/>.</summary>
    public static void RegisterBool(string id, Action<bool> handler)
    {
        boolListeners[id] = handler;
        if (bools.TryGetValue(id, out bool v)) handler(v);
    }

    /// <summary>Subscribes a handler to string changes for <paramref name="id"/>.</summary>
    public static void RegisterString(string id, Action<string> handler)
    {
        stringListeners[id] = handler;
        if (strings.TryGetValue(id, out string v)) handler(v);
    }

    /// <summary>Subscribes a handler to int changes for <paramref name="id"/>.</summary>
    public static void RegisterInt(string id, Action<int> handler)
    {
        intListeners[id] = handler;
        if (ints.TryGetValue(id, out int v)) handler(v);
    }

    /// <summary>
    /// Removes any handler registered under <paramref name="id"/>. Clears all four tables
    /// because an id belongs to exactly one type in practice, so only one Remove does work.
    /// Call from OnDisable to avoid handlers pointing at destroyed objects.
    /// </summary>
    public static void Unregister(string id)
    {
        floatListeners.Remove(id);
        boolListeners.Remove(id);
        stringListeners.Remove(id);
        intListeners.Remove(id);
    }

    /// <summary>
    /// Registers a server-side method that clients can trigger by name. Only meaningful on the
    /// server; registering on a pure client is harmless but the entry will never be invoked.
    /// </summary>
    public static void RegisterServerAction(string id, Action action)
    {
        serverActions[id] = action;
    }

    /// <summary>Removes a server action. Call from OnDisable.</summary>
    public static void UnregisterServerAction(string id)
    {
        serverActions.Remove(id);
    }

    #endregion

    #region Write path -- client requests

    // These are the entry points widgets call. They are static wrappers whose only job is to
    // forward to the Command on the live Instance; the null-conditional means a call before
    // the manager spawns is silently dropped rather than throwing.
    //
    // Nothing changes locally here. The value only reaches the UI once the server has applied
    // it and the Rpc has come back down -- on the host that round trip is free, on a remote
    // client it costs roughly one RTT.

    /// <summary>Asks the server to set a float value.</summary>
    public static void SetFloat(string id, float value)
    {
        Instance?.CmdSetFloat(id, value);
    }

    /// <summary>Asks the server to set a bool value.</summary>
    public static void SetBool(string id, bool value)
    {
        Instance?.CmdSetBool(id, value);
    }

    /// <summary>Asks the server to set a string value.</summary>
    public static void SetString(string id, string value)
    {
        Instance?.CmdSetString(id, value);
    }

    /// <summary>Asks the server to set an int value.</summary>
    public static void SetInt(string id, int value)
    {
        Instance?.CmdSetInt(id, value);
    }

    /// <summary>Asks the server to run the action registered under <paramref name="id"/>.</summary>
    public static void Invoke(string id)
    {
        Instance?.CmdInvoke(id);
    }

    #endregion

    #region Commands -- run on the server

    // requiresAuthority = false because this manager is a scene object owned by the server:
    // no client has authority over it, but every client needs to be able to send to it.
    //
    // NOTE: there is no validation here. Any client can set any id to any value. Add
    // per-id checks in these methods if a value ever affects gameplay rather than cosmetics.
    //
    // Each Cmd writes the authoritative value, then broadcasts. The server's own copy is
    // updated by the write, not by the Rpc.

    /// <summary>Server-side handler for <see cref="SetFloat"/>.</summary>
    [Command(requiresAuthority = false)]
    void CmdSetFloat(string id, float value)
    {
        floats[id] = value;
        RpcSetFloat(id, value);
    }

    /// <summary>Server-side handler for <see cref="SetBool"/>.</summary>
    [Command(requiresAuthority = false)]
    void CmdSetBool(string id, bool value)
    {
        bools[id] = value;
        RpcSetBool(id, value);
    }

    /// <summary>Server-side handler for <see cref="SetString"/>.</summary>
    [Command(requiresAuthority = false)]
    void CmdSetString(string id, string value)
    {
        strings[id] = value;
        RpcSetString(id, value);
    }

    /// <summary>Server-side handler for <see cref="SetInt"/>.</summary>
    [Command(requiresAuthority = false)]
    void CmdSetInt(string id, int value)
    {
        ints[id] = value;
        RpcSetInt(id, value);
    }

    /// <summary>
    /// Server-side handler for <see cref="Invoke"/>. Unknown ids are ignored -- a client
    /// asking for an action this server has not registered is a no-op, not an error.
    /// </summary>
    [Command(requiresAuthority = false)]
    void CmdInvoke(string id)
    {
        if (serverActions.TryGetValue(id, out Action a)) a();
    }

    #endregion

    #region Write path -- server pushes directly

    // Identical bodies to the Cmd methods above, minus the network hop. Called by server-side
    // game logic that already holds authority, such as AutoTyping's coroutine. [Server] logs a
    // warning and returns if these are somehow reached on a client.

    /// <summary>Sets a float from server-side logic and broadcasts it.</summary>
    [Server]
    public void ServerSetFloat(string id, float value)
    {
        floats[id] = value;
        RpcSetFloat(id, value);
    }

    /// <summary>Sets a bool from server-side logic and broadcasts it.</summary>
    [Server]
    public void ServerSetBool(string id, bool value)
    {
        bools[id] = value;
        RpcSetBool(id, value);
    }

    /// <summary>Sets a string from server-side logic and broadcasts it.</summary>
    [Server]
    public void ServerSetString(string id, string value)
    {
        strings[id] = value;
        RpcSetString(id, value);
    }

    /// <summary>Sets an int from server-side logic and broadcasts it.</summary>
    [Server]
    public void ServerSetInt(string id, int value)
    {
        ints[id] = value;
        RpcSetInt(id, value);
    }

    #endregion

    #region Broadcast -- run on every client

    // Thin wrappers so the Apply logic stays in one place and can also be reused by the
    // late-join handshake, which delivers the same values through a TargetRpc instead.
    //
    // Unlike SyncVars these are not batched at syncInterval: every call is its own message.
    // High-frequency sources (a slider drag) should throttle on the sending side.

    /// <summary>Delivers a float change to all clients.</summary>
    [ClientRpc]
    void RpcSetFloat(string id, float value)
    {
        ApplyFloat(id, value);
    }

    /// <summary>Delivers a bool change to all clients.</summary>
    [ClientRpc]
    void RpcSetBool(string id, bool value)
    {
        ApplyBool(id, value);
    }

    /// <summary>Delivers a string change to all clients.</summary>
    [ClientRpc]
    void RpcSetString(string id, string value)
    {
        ApplyString(id, value);
    }

    /// <summary>Delivers an int change to all clients.</summary>
    [ClientRpc]
    void RpcSetInt(string id, int value)
    {
        ApplyInt(id, value);
    }

    #endregion

    #region Apply -- update the client cache and notify the widget

    // Same two steps in all four: write the cache first, then fire the handler if one exists.
    //
    // Cache-first matters. A widget that registers later reads the cache in RegisterX and
    // paints itself immediately, so it does not have to wait for the next change to arrive.
    //
    // No listener for an id is normal, not an error: the value may belong to a widget that is
    // currently disabled, or on a scene this client has not loaded yet.
    //
    // Static so TargetFullState and the Rpcs can share them without an instance reference.

    static void ApplyFloat(string id, float value)
    {
        floats[id] = value;
        if (floatListeners.TryGetValue(id, out Action<float> h)) h(value);
    }

    static void ApplyBool(string id, bool value)
    {
        bools[id] = value;
        if (boolListeners.TryGetValue(id, out Action<bool> h)) h(value);
    }

    static void ApplyString(string id, string value)
    {
        strings[id] = value;
        if (stringListeners.TryGetValue(id, out Action<string> h)) h(value);
    }

    static void ApplyInt(string id, int value)
    {
        ints[id] = value;
        if (intListeners.TryGetValue(id, out Action<int> h)) h(value);
    }

    #endregion

    #region Late-join handshake

    // This block is what SyncVars give you for free. An Rpc only reaches clients connected at
    // the moment it fires, so without this a client joining after a slider moved would see
    // that slider at zero forever. On spawn, each client asks the server for a full snapshot
    // and receives it privately.

    /// <summary>
    /// Requests the current state as soon as this object spawns on a client. Also runs on the
    /// host, where the round trip is local and the values are already correct -- harmless, but
    /// you can guard with <c>if (!isServer)</c> if you want to skip the redundant message.
    /// </summary>
    public override void OnStartClient()
    {
        base.OnStartClient();
        CmdRequestFullState();
    }

    /// <summary>
    /// Server side: flattens all four dictionaries into arrays and sends them back to the one
    /// caller. <paramref name="sender"/> is filled in by Mirror -- it is not passed by the
    /// client, which is why it has a default value.
    /// </summary>
    [Command(requiresAuthority = false)]
    void CmdRequestFullState(NetworkConnectionToClient sender = null)
    {
        List<FloatEntry> f = new List<FloatEntry>();
        foreach (KeyValuePair<string, float> kv in floats)
        {
            f.Add(new FloatEntry { id = kv.Key, value = kv.Value });
        }

        List<BoolEntry> b = new List<BoolEntry>();
        foreach (KeyValuePair<string, bool> kv in bools)
        {
            b.Add(new BoolEntry { id = kv.Key, value = kv.Value });
        }

        List<StringEntry> s = new List<StringEntry>();
        foreach (KeyValuePair<string, string> kv in strings)
        {
            s.Add(new StringEntry { id = kv.Key, value = kv.Value });
        }

        List<IntEntry> i = new List<IntEntry>();
        foreach (KeyValuePair<string, int> kv in ints)
        {
            i.Add(new IntEntry { id = kv.Key, value = kv.Value });
        }

        TargetFullState(sender, f.ToArray(), b.ToArray(), s.ToArray(), i.ToArray());
    }

    /// <summary>
    /// Client side: applies a full snapshot. Sent as a TargetRpc so only the joining client
    /// pays for it rather than broadcasting the whole store to everyone.
    /// </summary>
    [TargetRpc]
    void TargetFullState(NetworkConnectionToClient target, FloatEntry[] f, BoolEntry[] b, StringEntry[] s, IntEntry[] i)
    {
        foreach (FloatEntry e in f) ApplyFloat(e.id, e.value);
        foreach (BoolEntry e in b) ApplyBool(e.id, e.value);
        foreach (StringEntry e in s) ApplyString(e.id, e.value);
        foreach (IntEntry e in i) ApplyInt(e.id, e.value);
    }

    #endregion
}
