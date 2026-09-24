using UnityEngine;
using Mirror;
using Steamworks;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public class LobbyController : MonoBehaviour
{
    public static LobbyController Instance;

    //UI Elements
    [SerializeField, Tooltip("")]
    private TextMeshProUGUI _LobbyNameText;
    //Player Data
    [SerializeField, Tooltip("")]
    private GameObject _PlayerListViewContent;
    [SerializeField, Tooltip("")]
    private GameObject _PlayerListItemPrefab;
    //Ready
    [SerializeField, Tooltip("")]
    private Button _StartGameButton;
    [SerializeField, Tooltip("")]
    private TextMeshProUGUI _ReadyButtonText;

    //Other Data
    private ulong _CurrentLobbyID;
    private bool _PlayerItemCreated = false;
    private List<PlayerListItem> _PlayerListItems = new List<PlayerListItem>();
    private GameObject _LocalPlayerObject;
    private PlayerObjectController _LocalPlayerController;

    //Manager
    private CustomNetworkManager _Manager;

    private CustomNetworkManager Manager
    {
        get
        {
            if (_Manager != null)
            {
                return _Manager;
            }
            return _Manager = CustomNetworkManager.singleton as CustomNetworkManager;
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void UpdateLobbyName()
    {
        _CurrentLobbyID = Manager.GetComponent<SteamLobby>().CurrentLobbyID;
        _LobbyNameText.text = SteamMatchmaking.GetLobbyData(new CSteamID(_CurrentLobbyID), "name");
    }

    public void UpdatePlayerList()
    {
        if (!_PlayerItemCreated)
        {
            CreateHostPlayerItem();
        }

        if (_PlayerListItems.Count < Manager._GamePlayers.Count)
        {
            CreateClientPlayerItem();
        }
        else if (_PlayerListItems.Count > Manager._GamePlayers.Count)
        {
            RemovePlayerItem();
        }
        else if (_PlayerListItems.Count == Manager._GamePlayers.Count)
        {
            UpdatePlayerItem();
        }
    }

    public void FindLocalPlayer()
    {
        _LocalPlayerObject = GameObject.Find("LocalGamePlayer");
        _LocalPlayerController = _LocalPlayerObject.GetComponent<PlayerObjectController>();
    }

    private void CreateHostPlayerItem()
    {
        foreach (PlayerObjectController player in Manager._GamePlayers)
        {
            GameObject newPlayerItem = Instantiate(_PlayerListItemPrefab) as GameObject;
            PlayerListItem newPlayerItemScript = newPlayerItem.GetComponent<PlayerListItem>();

            newPlayerItemScript._PlayerName = player._PlayerName;
            newPlayerItemScript._ConnectionID = player._ConnectionID;
            newPlayerItemScript._PlayerSteamID = player._PlayerSteamID;
            newPlayerItemScript._Ready = player._Ready;
            newPlayerItemScript.SetPlayerValues();

            newPlayerItem.transform.SetParent(_PlayerListViewContent.transform);
            newPlayerItem.transform.localScale = Vector3.one;

            _PlayerListItems.Add(newPlayerItemScript);
        }
        _PlayerItemCreated = true;
    }

    private void CreateClientPlayerItem()
    {
        foreach (PlayerObjectController player in Manager._GamePlayers)
        {
            if (!_PlayerListItems.Any(b => b._ConnectionID == player._ConnectionID))
            {
                GameObject newPlayerItem = Instantiate(_PlayerListItemPrefab) as GameObject;
                PlayerListItem newPlayerItemScript = newPlayerItem.GetComponent<PlayerListItem>();

                newPlayerItemScript._PlayerName = player._PlayerName;
                newPlayerItemScript._ConnectionID = player._ConnectionID;
                newPlayerItemScript._PlayerSteamID = player._PlayerSteamID;
                newPlayerItemScript._Ready = player._Ready;
                newPlayerItemScript.SetPlayerValues();

                newPlayerItem.transform.SetParent(_PlayerListViewContent.transform);
                newPlayerItem.transform.localScale = Vector3.one;

                _PlayerListItems.Add(newPlayerItemScript);
            }
        }
    }

    private void UpdatePlayerItem()
    {
        foreach (PlayerObjectController player in Manager._GamePlayers)
        {
            foreach (PlayerListItem playerListItemScript in _PlayerListItems)
            {
                if (playerListItemScript._ConnectionID == player._ConnectionID)
                {
                    playerListItemScript._PlayerName = player._PlayerName;
                    playerListItemScript._Ready = player._Ready;
                    playerListItemScript.SetPlayerValues();
                    if (player == _LocalPlayerController)
                    {
                        UpdateButton();
                    }
                }
            }
        }
        CheckIfAllReady();
    }

    private void RemovePlayerItem()
    {
        List<PlayerListItem> playerListItemToRemove = new List<PlayerListItem>();

        foreach (PlayerListItem playerListItem in _PlayerListItems)
        {
            if (!Manager._GamePlayers.Any(b => b._ConnectionID == playerListItem._ConnectionID))
            {
                playerListItemToRemove.Add(playerListItem);
            }
        }
        if (playerListItemToRemove.Count > 0)
        {
            foreach (PlayerListItem playerlistItemToRemove in playerListItemToRemove)
            {
                GameObject ObjectToRemove = playerlistItemToRemove.gameObject;
                _PlayerListItems.Remove(playerlistItemToRemove);
                Destroy(ObjectToRemove);
                ObjectToRemove = null;
            }

        }
    }

    private void UpdateButton()
    {
        if (_LocalPlayerController._Ready)
        {
            _ReadyButtonText.text = "Unready";
        }
        else
        {
            _ReadyButtonText.text = "Ready";

        }
    }

    private void CheckIfAllReady()
    {
        bool allReady = false;

        foreach (PlayerObjectController player in Manager._GamePlayers)
        {
            if (player._Ready)
            {
                allReady = true;
            }
            else
            {
                allReady = false;
                break;
            }
        }

        if (allReady)
        {
            //Is the host
            if (_LocalPlayerController._PlayerIdNumber == 1)
            {
                _StartGameButton.interactable = true;
            }
            else
            {
                _StartGameButton.interactable = false;
            }
        }
        else
        {
            _StartGameButton.interactable = false;
        }
    }

    #region Called from buttons

    public void ReadyPlayer()
    {
        _LocalPlayerController.ChangeReady();
    }


    public void StartGame(string _sceneName)
    {
        _LocalPlayerController.CanStartGame(_sceneName);
    }
    #endregion
}
