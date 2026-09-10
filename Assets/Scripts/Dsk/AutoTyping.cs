using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AutoTyping : MonoBehaviour
{
    #region Inspector

    // progressId and activeId address replicated values.
    // startId is different in kind: it holds no value at all, it names a server-side method that any client may trigger.
    [Header("Sync IDs")]
    [SerializeField] string progressId = "typing1.progress";
    [SerializeField] string activeId = "typing1.active";
    [SerializeField] string startId = "typing1.start";

    [Header("Typing Settings")]
    [SerializeField] string fullText = "Lorem ipsum dolor sit amet...";
    [SerializeField] float typingSpeed = 0.05f;

    [Header("UI References")]
    [SerializeField] TMP_Text displayText;
    [SerializeField] Button toggleButton;
    [SerializeField] Image buttonImage;

    [Header("Colors")]
    [SerializeField] Color activeColor = Color.green;
    [SerializeField] Color inactiveColor = Color.red;

    #endregion

    #region Local state

    /// <summary>
    /// Handle to the running typing coroutine, server-side only. Non-null means typing is in progress.
    /// Used to cancel a run before starting a new one.
    /// </summary>
    Coroutine typingCoroutine;

    #endregion

    #region Lifecycle

    /// <summary>
    /// Registers both display handlers and the server action. RegisterServerAction is called
    /// on every peer, but only the server's copy is ever invoked -- on a client the entry sits
    /// unused, which keeps this script free of isServer branching at setup time.
    /// </summary>
    void OnEnable()
    {
        UISyncManager.RegisterInt(progressId, ApplyProgress);
        UISyncManager.RegisterBool(activeId, ApplyActive);
        UISyncManager.RegisterServerAction(startId, ServerStartTyping);

        if (toggleButton != null) toggleButton.onClick.AddListener(OnToggleButtonPressed);
    }

    /// <summary>
    /// Unregisters everything and stops the coroutine. Stopping matters on the server: a
    /// disabled object's coroutine would otherwise be halted by Unity mid-run, leaving the
    /// active flag stuck true on every client.
    /// </summary>
    void OnDisable()
    {
        UISyncManager.Unregister(progressId);
        UISyncManager.Unregister(activeId);
        UISyncManager.UnregisterServerAction(startId);

        if (toggleButton != null) toggleButton.onClick.RemoveListener(OnToggleButtonPressed);

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
    }

    #endregion

    #region Local input -> server

    /// <summary>
    /// Button press on any client. Sends a trigger rather than a value -- the client is asking
    /// the server to run something, not setting state. On a pure client this does nothing
    /// locally; the animation only appears once the server's first progress update arrives.
    /// </summary>
    void OnToggleButtonPressed()
    {
        UISyncManager.Invoke(startId);
    }

    #endregion

    #region Server logic

    /// <summary>
    /// Invoked by the manager when a client triggers <see cref="startId"/>. Restarts from the
    /// beginning if a run is already going, so repeated presses do not stack coroutines.
    /// </summary>
    void ServerStartTyping()
    {
        if (!NetworkServer.active) return;

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeTextRoutine());
    }

    /// <summary>
    /// Server-only. Pushes a rising character count to all clients, one message per tick, then
    /// clears the active flag when finished.
    /// </summary>
    IEnumerator TypeTextRoutine()
    {
        UISyncManager mgr = UISyncManager.Instance;

        mgr.ServerSetBool(activeId, true);
        mgr.ServerSetInt(progressId, 0);

        for (int i = 1; i <= fullText.Length; i++)
        {
            mgr.ServerSetInt(progressId, i);
            yield return new WaitForSeconds(typingSpeed);
        }

        mgr.ServerSetBool(activeId, false);
        typingCoroutine = null;
    }

    #endregion

    #region Server -> UI

    /// <summary>
    /// Rebuilds the visible text from the replicated character count. Clamped because a stale
    /// or mismatched count must not throw -- if fullText ever differs between peers, this
    /// degrades to wrong text rather than an exception.
    /// </summary>
    void ApplyProgress(int count)
    {
        if (displayText == null) return;
        count = Mathf.Clamp(count, 0, fullText.Length);
        displayText.text = fullText.Substring(0, count);
    }

    /// <summary>Tints the button to show whether typing is currently running.</summary>
    void ApplyActive(bool v)
    {
        if (buttonImage != null) buttonImage.color = v ? activeColor : inactiveColor;
    }

    #endregion
}
