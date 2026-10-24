using Steamworks;
using TMPro;
using UnityEngine;

/// <summary>
/// Password popup shown when joining a private lobby.
/// </summary>
public class PasswordPopupUI : MonoBehaviour
{
    public static PasswordPopupUI Instance { get; private set; }

    [SerializeField] private GameObject _Panel;
    [SerializeField] private TMP_InputField _PasswordInput;
    [SerializeField] private TextMeshProUGUI _ErrorText;

    private CSteamID _PendingLobbyID;
    private string _RequiredPassword;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        _Panel.SetActive(false);
    }

    /// <summary>
    /// Called by SteamLobbyClient when the lobby has a password.
    /// </summary>
    public void Show(CSteamID lobbyID, string requiredPassword)
    {
        _PendingLobbyID = lobbyID;
        _RequiredPassword = requiredPassword;
        _PasswordInput.text = "";
        _ErrorText.text = "";
        _Panel.SetActive(true);
        _PasswordInput.ActivateInputField();
    }

    /// <summary>
    /// Wire to the Confirm button.
    /// </summary>
    public void OnConfirmPressed()
    {
        if (_PasswordInput.text.Trim() == _RequiredPassword)
        {
            _Panel.SetActive(false);
            SteamLobbyClient.Instance.JoinLobby(_PendingLobbyID);
        }
        else
        {
            _ErrorText.text = "Wrong password. Try again.";
            _PasswordInput.text = "";
            _PasswordInput.ActivateInputField();
        }
    }

    /// <summary>
    /// Wire to the Cancel button.
    /// </summary>
    public void OnCancelPressed()
    {
        _Panel.SetActive(false);
        _PendingLobbyID = default;
        _RequiredPassword = "";
    }
}