using Mirror;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PublicVarSynchronization : MonoBehaviour
{
    #region Inspector

    // Addresses for this widget's three values on the manager. Duplicating this GameObject
    // copies these strings too -- rename them (panel2.slider, ...) or both copies will share
    // state and one of them will stop receiving updates.
    [Header("Sync IDs (must be unique per widget in the scene)")]
    [SerializeField] string sliderId = "panel1.slider";
    [SerializeField] string inputId = "panel1.input";
    [SerializeField] string activeId = "panel1.active";

    [Header("UI References")]
    [SerializeField] Slider slider;
    [SerializeField] TMP_InputField inputField;
    [SerializeField] Button actionButton;
    [SerializeField] Image actionButtonImage;

    [Header("Colors")]
    [SerializeField] Color activeColor = Color.green;
    [SerializeField] Color inactiveColor = Color.red;

    #endregion

    #region Local state

    /// <summary>
    /// Set while an Apply method is writing to a control, so the resulting onValueChanged
    /// event is not mistaken for local user input and echoed back to the server.
    /// SetValueWithoutNotify already suppresses the callback;
    /// </summary>
    bool isUpdatingFromNetwork;

    /// <summary>
    /// Last known value of the active flag.
    /// </summary>
    bool localActive;

    #endregion

    #region Lifecycle

    /// <summary>
    /// Subscribes to the manager (network -> UI) and to the controls (UI -> network).
    /// OnEnable rather than Start so the registrations survive the object being toggled off
    /// and on again. Registration is safe before the manager spawns -- the tables are static.
    /// </summary>
    void OnEnable()
    {
        UISyncManager.RegisterFloat(sliderId, ApplySlider);
        UISyncManager.RegisterString(inputId, ApplyInput);
        UISyncManager.RegisterBool(activeId, ApplyActive);

        if (slider != null) slider.onValueChanged.AddListener(OnLocalSliderChanged);
        if (inputField != null) inputField.onValueChanged.AddListener(OnLocalInputFieldChanged);
        if (actionButton != null) actionButton.onClick.AddListener(OnLocalButtonPressed);
    }

    /// <summary>
    /// Mirror image of OnEnable. Unregistering matters: the manager's listener tables are
    /// static, so a stale handler would keep a destroyed object alive and throw on the next
    /// update for that id.
    /// </summary>
    void OnDisable()
    {
        UISyncManager.Unregister(sliderId);
        UISyncManager.Unregister(inputId);
        UISyncManager.Unregister(activeId);

        if (slider != null) slider.onValueChanged.RemoveListener(OnLocalSliderChanged);
        if (inputField != null) inputField.onValueChanged.RemoveListener(OnLocalInputFieldChanged);
        if (actionButton != null) actionButton.onClick.RemoveListener(OnLocalButtonPressed);
    }

    #endregion

    #region Local input -> server
    void OnLocalSliderChanged(float v)
    {
        if (isUpdatingFromNetwork) return;
        UISyncManager.SetFloat(sliderId, v);
    }

    /// <summary>
    /// Input field edit. Sends on every keystroke; switching to onEndEdit would send once per
    /// commit instead, at the cost of others not seeing the text as it is typed.
    /// </summary>
    void OnLocalInputFieldChanged(string t)
    {
        if (isUpdatingFromNetwork) return;
        UISyncManager.SetString(inputId, t);
    }

    /// <summary>
    /// Button press. Inverts the cached value rather than a networked one, so two clients
    /// clicking within the same round trip both send the same target value -- the second click
    /// is absorbed instead of toggling twice. Send a trigger and let the server invert if that
    /// matters.
    /// </summary>
    void OnLocalButtonPressed()
    {
        UISyncManager.SetBool(activeId, !localActive);
    }

    #endregion

    #region Server -> UI

    /// <summary>Applies an incoming slider value.</summary>
    void ApplySlider(float v)
    {
        if (slider == null) return;
        isUpdatingFromNetwork = true;
        slider.SetValueWithoutNotify(v);
        isUpdatingFromNetwork = false;
    }

    /// <summary>
    /// Applies incoming text. Note this overwrites whatever the local user is typing if two
    /// people edit the same field at once -- last write wins, and the caret can jump.
    /// </summary>
    void ApplyInput(string t)
    {
        if (inputField == null) return;
        isUpdatingFromNetwork = true;
        inputField.SetTextWithoutNotify(t);
        isUpdatingFromNetwork = false;
    }

    /// <summary>
    /// Applies the active flag: updates the cached copy used by the toggle, then tints the
    /// button. No guard flag needed -- setting a colour raises no UI event.
    /// </summary>
    void ApplyActive(bool v)
    {
        localActive = v;
        if (actionButtonImage != null)
            actionButtonImage.color = v ? activeColor : inactiveColor;
    }

    #endregion
}
