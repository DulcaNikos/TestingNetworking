using Steamworks;
using UnityEngine;

/// <summary>
/// Add to your lobby UI. Shows Steam's built-in invite overlay.
/// </summary>
public class SteamInviteButton : MonoBehaviour
{
    /// <summary>
    /// Wire this to your Invite Friends button.
    /// </summary>
    public void OpenInviteOverlay()
    {
        ulong lobbyID = SteamLobbyHost.Instance != null
            ? SteamLobbyHost.Instance.CurrentLobbyID : 0;

        if (lobbyID == 0)
        {
            Debug.LogError("No active lobby to invite to.");
            return;
        }

        // Opens Steam's own friend picker overlay
        SteamFriends.ActivateGameOverlayInviteDialog(new CSteamID(lobbyID));
    }
}