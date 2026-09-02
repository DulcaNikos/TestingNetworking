using UnityEngine;
using Mirror;
using UnityEngine.UI;
using TMPro;
using Steamworks;

namespace SteamLobbyN
{
    public class PlayerLobbyHandler : NetworkBehaviour
    {
        [SyncVar(hook = nameof(OnReadyStatusChanged))]
        public bool _IsReady = false;

        [SyncVar(hook = nameof(OnNameChanged))]
        public string _PlayerName = "";

        public Button _ReadyButton;
        public TextMeshProUGUI _NameText;

        public override void OnStartClient()
        {
            base.OnStartClient();
            LobbyUIManager.Instance.RegisterPlayer(this);
            _NameText.text = _PlayerName;
            SetSelectedButtonColor(_IsReady ? Color.green : Color.white);
            LobbyUIManager.Instance.RefreshPlayButton();
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            LobbyUIManager.Instance?.UnregisterPlayer(this);
        }

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            CmdSetName(SteamFriends.GetPersonaName());
        }

        void Start()
        {
            _ReadyButton.interactable = isLocalPlayer;
        }

        [Command]
        private void CmdSetName(string name)
        {
            _PlayerName = string.IsNullOrWhiteSpace(name) ? "Player" : name;
        }

        [Command]
        private void CmdSetReady()
        {
            _IsReady = !_IsReady;
        }

        public void OnReadyButtonClicked() => CmdSetReady();

        private void OnNameChanged(string _old, string _new)
        {
            _NameText.text = _new;
        }

        private void OnReadyStatusChanged(bool _old, bool _new)
        {
            SetSelectedButtonColor(_new ? Color.green : Color.white);
            LobbyUIManager.Instance.RefreshPlayButton();
        }

        private void SetSelectedButtonColor(Color color)
        {
            ColorBlock cb = _ReadyButton.colors;
            cb.normalColor = color;
            cb.selectedColor = color;
            cb.disabledColor = color;
            _ReadyButton.colors = cb;
        }
    }
}