using Mirror;
using UnityEngine;

/// <summary>
/// Only the matching team can see and collect this.
/// NetworkTeam + TeamInterestManagement handle visibility automatically.
/// </summary>
[RequireComponent(typeof(Collider))]
public class TeamPickup : NetworkBehaviour
{
    [SerializeField] private Team _Team = Team.Red;
    [SerializeField] private int _Points = 1;
    [SerializeField] private float _RespawnTime = 5f;
    [SerializeField] private MeshRenderer _Renderer;

    [SyncVar(hook = nameof(OnActiveChanged))]
    private bool _Active = true;

    public override void OnStartServer()
    {
        NetworkTeam networkTeam = GetComponent<NetworkTeam>();
        if (networkTeam != null)
        {
            networkTeam.teamId = _Team.ToString();
        }
    }

    public override void OnStartClient()
    {
        UpdateVisual(_Active);
    }

    [ServerCallback]
    private void OnTriggerEnter(Collider other)
    {
        if (!_Active) return;

        PlayerGameController player = other.GetComponent<PlayerGameController>();
        if (player == null) return;
        if (player._Team != _Team) return;

        _Active = false;
        TeamManager.Instance.AddScore(_Team, _Points);
        Invoke(nameof(Respawn), _RespawnTime);
    }

    [Server]
    private void Respawn()
    {
        _Active = true;
    }

    private void OnActiveChanged(bool _, bool newVal)
    {
        UpdateVisual(newVal);
    }

    private void UpdateVisual(bool active)
    {
        if (_Renderer != null) _Renderer.enabled = active;
        GetComponent<Collider>().enabled = active;
    }
}