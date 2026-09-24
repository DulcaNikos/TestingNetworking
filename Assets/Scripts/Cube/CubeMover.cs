using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Host-controlled cube. Only the server reads input and moves the object.
/// NetworkRigidbodyReliable syncs position and rotation to all clients.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class CubeMover : NetworkBehaviour
{
    [SerializeField, Tooltip("Movement speed in units per second.")]
    private float _MoveSpeed = 5f;

    private Rigidbody _Rigidbody;

    private void Awake()
    {
        _Rigidbody = GetComponent<Rigidbody>();
        _Rigidbody.isKinematic = true;
        _Rigidbody.useGravity = false;
    }

    public override void OnStartClient()
    {
        if (!isServer)
        {
            _Rigidbody.interpolation = RigidbodyInterpolation.None;
        }
    }

    public override void OnStartServer()
    {
        _Rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
    }

    /// <summary>
    /// Only runs on the server/host. Clients never enter this.
    /// </summary>
    [ServerCallback]
    private void FixedUpdate()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        // Only active while Right Shift is held
        if (!kb.rightShiftKey.isPressed) return;

        Vector2 input = Vector2.zero;
        if (kb.wKey.isPressed) input.y = 1f;
        if (kb.sKey.isPressed) input.y = -1f;
        if (kb.aKey.isPressed) input.x = -1f;
        if (kb.dKey.isPressed) input.x = 1f;

        if (input.sqrMagnitude > 0f)
        {
            Vector3 move = new Vector3(input.x, 0f, input.y).normalized * _MoveSpeed * Time.fixedDeltaTime;
            _Rigidbody.MovePosition(_Rigidbody.position + move);
        }
    }
}