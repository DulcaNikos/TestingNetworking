using UnityEngine;
using Steamworks;
using System.Collections.Generic;

public class LobbiesListManager : MonoBehaviour
{
    public static LobbiesListManager Instance;

    [SerializeField, Tooltip("")]
    private GameObject _LobbiesMenu;
    [SerializeField, Tooltip("")]
    private GameObject _LobbyDataItemPrefab;
    [SerializeField, Tooltip("")]
    private GameObject _LobbyListContent;

    [SerializeField, Tooltip("")]
    private GameObject _LobbiesButton;
    [SerializeField, Tooltip("")]
    private GameObject _HostButton;

    private List<GameObject> _ListOfLobbies = new List<GameObject>();
    public List<GameObject> ListOfLobbies => _ListOfLobbies;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    public void GetListOfLobbies()
    {
        _LobbiesButton.SetActive(false);
        _HostButton.SetActive(false);
        _LobbiesMenu.SetActive(true);

        SteamLobby.Instance.GetLobbiesList();
    }

    public void DisplayLobbies(List<CSteamID> _lobbyIDs, LobbyDataUpdate_t _result)
    {
        GameObject createdItem;
        LobbyDataEntry lobbyDataEntry;
        for (int i = 0; i < _lobbyIDs.Count; i++)
        {
            if (_lobbyIDs[i].m_SteamID == _result.m_ulSteamIDLobby)
            {
                createdItem = Instantiate(_LobbyDataItemPrefab);
                lobbyDataEntry = createdItem.GetComponent<LobbyDataEntry>();
                lobbyDataEntry._LobbyID = (CSteamID)_lobbyIDs[i].m_SteamID;
                lobbyDataEntry._LobbyName = SteamMatchmaking.GetLobbyData((CSteamID)_lobbyIDs[i].m_SteamID, "name");
                lobbyDataEntry.SetLobbydata();
                lobbyDataEntry.transform.SetParent(_LobbyListContent.transform);
                lobbyDataEntry.transform.localScale = Vector3.one;

                _ListOfLobbies.Add(createdItem);
            }
        }
    }

    public void DestroyLobbies()
    {
        foreach (GameObject lobbyItem in _ListOfLobbies)
        {
            Destroy(lobbyItem);
        }
        _ListOfLobbies.Clear();
    }


}
