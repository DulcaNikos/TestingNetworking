using System.Collections.Generic;
using UnityEngine;
using Mirror;
using TMPro;
using UnityEngine.UI;

namespace SteamLobbyN
{
    public class LobbyUIManager : MonoBehaviour
    {
        public static LobbyUIManager Instance;
        public Transform _PlayerListParent;
        public List<PlayerLobbyHandler> _PlayerLobbyHandlers = new List<PlayerLobbyHandler>();
        public Button _PlayerGameButton;
        public TMP_InputField _InputFieldLobbyID;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        void Start()
        {
            _PlayerGameButton.interactable = false;
            if (SteamLobbyM.Instance != null)
                SteamLobbyM.Instance.OnLobbyReady += UpdateLobbyID;
            else
                Debug.LogError("SteamLobbyM.Instance is null — check script execution order.");
        }

        void OnDestroy()
        {
            if (SteamLobbyM.Instance != null)
                SteamLobbyM.Instance.OnLobbyReady -= UpdateLobbyID;
        }

        private void UpdateLobbyID(ulong lobbyID)
        {
            _InputFieldLobbyID.text = $"Lobby ID: {lobbyID}";
        }

        public void OnPlayButtonClicked()
        {
            if (NetworkServer.active)
            {
                CustomNetworkManager.singleton.ServerChangeScene("GameplayScene");
            }
        }

        public void RegisterPlayer(PlayerLobbyHandler _player)
        {
            _player.transform.SetParent(_PlayerListParent, false);
            if (!_PlayerLobbyHandlers.Contains(_player))
                _PlayerLobbyHandlers.Add(_player);
            RefreshPlayButton();
        }

        public void UnregisterPlayer(PlayerLobbyHandler _player)
        {
            _PlayerLobbyHandlers.Remove(_player);
            RefreshPlayButton();
        }

        public void RefreshPlayButton()
        {
            bool everyoneReady = _PlayerLobbyHandlers.Count > 0 && _PlayerLobbyHandlers.TrueForAll(p => p != null && p._IsReady);

            // Only the host can start the game
            _PlayerGameButton.interactable = everyoneReady && NetworkServer.active;
        }

    }
}