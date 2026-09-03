using Mirror;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PublicVarSynchronization : NetworkBehaviour
{
    [Header("UI References (local, not networked)")]
    [SerializeField] public Slider slider;
    [SerializeField] public TMP_InputField inputField;
    [SerializeField] public Button actionButton;
    [SerializeField] public Image actionButtonImage;

    [Header("Colors")]
    [SerializeField] public Color activeColor = Color.green;
    [SerializeField] public Color inactiveColor = Color.red;

    [SyncVar(hook = nameof(OnSliderValueChanged))]
    public float sliderValue;

    [SyncVar(hook = nameof(OnActiveChanged))]
    public bool active;

    [SyncVar(hook = nameof(OnInputFieldTextChanged))]
    public string inputFieldText = "";

    private bool isUpdatingFromNetwork;

    public override void OnStartClient()
    {
        base.OnStartClient();

        isUpdatingFromNetwork = true;

        if (slider != null)
        {
            slider.SetValueWithoutNotify(sliderValue);
        }

        if (inputField != null)
        {
            inputField.SetTextWithoutNotify(inputFieldText);
        }

        if (actionButtonImage != null)
        {
            actionButtonImage.color = active ? activeColor : inactiveColor;
        }

        isUpdatingFromNetwork = false;

        if (slider != null)
        {
            slider.onValueChanged.AddListener(OnLocalSliderChanged);
        }

        if (inputField != null)
        {
            inputField.onValueChanged.AddListener(OnLocalInputFieldChanged);
        }

        if (actionButton != null)
        {
            actionButton.onClick.AddListener(OnLocalButtonPressed);
        }
    }

    private void OnDestroy()
    {
        if (slider != null)
        {
            slider.onValueChanged.RemoveListener(OnLocalSliderChanged);
        }

        if (inputField != null)
        {
            inputField.onValueChanged.RemoveListener(OnLocalInputFieldChanged);
        }

        if (actionButton != null)
        {
            actionButton.onClick.RemoveListener(OnLocalButtonPressed);
        }
    }

    // ---------------- SLIDER ----------------

    private void OnLocalSliderChanged(float newValue)
    {
        if (isUpdatingFromNetwork)
        {
            return;
        }

        CmdUpdateSliderValue(newValue);
    }

    [Command(requiresAuthority = false)]
    private void CmdUpdateSliderValue(float newValue)
    {
        sliderValue = newValue;
    }

    private void OnSliderValueChanged(float oldValue, float newValue)
    {
        if (slider == null)
        {
            return;
        }

        isUpdatingFromNetwork = true;
        slider.SetValueWithoutNotify(newValue);
        isUpdatingFromNetwork = false;
    }

    // ---------------- INPUT FIELD ----------------

    private void OnLocalInputFieldChanged(string newText)
    {
        if (isUpdatingFromNetwork)
        {
            return;
        }

        CmdUpdateInputFieldText(newText);
    }

    [Command(requiresAuthority = false)]
    private void CmdUpdateInputFieldText(string newText)
    {
        inputFieldText = newText;
    }

    private void OnInputFieldTextChanged(string oldText, string newText)
    {
        if (inputField == null)
        {
            return;
        }

        isUpdatingFromNetwork = true;
        inputField.SetTextWithoutNotify(newText);
        isUpdatingFromNetwork = false;
    }

    // ---------------- BUTTON / ACTIVE STATE ----------------

    private void OnLocalButtonPressed()
    {
        CmdSetActive(!active);
    }

    [Command(requiresAuthority = false)]
    private void CmdSetActive(bool newValue)
    {
        active = newValue;
    }

    private void OnActiveChanged(bool oldValue, bool newValue)
    {
        if (actionButtonImage != null)
        {
            actionButtonImage.color = newValue ? activeColor : inactiveColor;
        }
    }
}