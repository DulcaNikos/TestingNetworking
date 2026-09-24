using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using System;

public enum Team { None = 0, Red = 1, Blue = 2 }

public class TeamManager : NetworkBehaviour
{
    public static TeamManager Instance { get; private set; }

    [SerializeField, Tooltip("Max players per team. 0 = unlimited.")]
    private int _MaxPerTeam = 4;

    [SyncVar(hook = nameof(OnRedScoreChanged))] public int RedScore = 0;
    [SyncVar(hook = nameof(OnBlueScoreChanged))] public int BlueScore = 0;

    public static event Action<int, int> OnScoreUpdated;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this); return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    [Server]
    public void AssignPlayerToTeam(PlayerObjectController player, Team team)
    {
        player._Team = team;

        NetworkTeam networkTeam = player.GetComponent<NetworkTeam>();
        if (networkTeam != null)
        {
            networkTeam.teamId = team.ToString();
        }
    }

    /// <summary>
    /// Call this from CustomNetworkManager before ServerChangeScene.
    /// </summary>
    [Server]
    public void AutoBalanceTeams()
    {
        CustomNetworkManager manager = CustomNetworkManager.singleton as CustomNetworkManager;
        List<PlayerObjectController> players = new List<PlayerObjectController>(manager._GamePlayers);

        foreach (PlayerObjectController player in players)
        {
            if (player._Team == Team.None)
            {
                AssignPlayerToTeam(player, GetSmallestTeam());
            }
        }
    }

    [Server]
    public bool CanJoinTeam(Team team)
    {
        if (team == Team.None) return false;
        return _MaxPerTeam == 0 || GetTeamCount(team) < _MaxPerTeam;
    }

    public int GetTeamCount(Team team)
    {
        CustomNetworkManager manager = CustomNetworkManager.singleton as CustomNetworkManager;
        return manager._GamePlayers.Count(p => p._Team == team);
    }

    public Team GetSmallestTeam()
    {
        int redCount = GetTeamCount(Team.Red);
        int blueCount = GetTeamCount(Team.Blue);

        if (redCount <= blueCount)
        {
            return Team.Red;
        }
        else
        {
            return Team.Blue;
        }
    }

    [Server]
    public void AddScore(Team team, int amount = 1)
    {
        if (team == Team.Red) RedScore += amount;
        if (team == Team.Blue) BlueScore += amount;
    }

    private void OnRedScoreChanged(int _, int newVal)
    {
        OnScoreUpdated?.Invoke(newVal, BlueScore);
    }


    private void OnBlueScoreChanged(int _, int newVal)
    {
        OnScoreUpdated?.Invoke(RedScore, newVal);
    }
}