using System.Collections;
using Mirror;
using UnityEngine;

/// <summary>
/// Server-authoritative movement pattern: moves forward in steps, then turns, repeated.
/// Position and rotation reach clients through a NetworkTransform on the same object.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class CubeMover : NetworkBehaviour
{
    [Header("Pattern")]
    [SerializeField, Tooltip("Steps forward before each turn.")]
    private int _StepsPerLeg = 5;

    [SerializeField, Tooltip("How many times the cube turns.")]
    private int _Turns = 5;

    [SerializeField, Tooltip("Distance of one step in world units.")]
    private float _StepSize = 1f;

    [SerializeField, Tooltip("Seconds to complete one step.")]
    private float _StepDuration = 0.25f;

    [SerializeField, Tooltip("Degrees per turn. Positive = right, negative = left.")]
    private float _TurnAngle = 90f;

    [SerializeField, Tooltip("Seconds to complete one turn.")]
    private float _TurnDuration = 0.3f;

    [Header("Cleanup")]
    [SerializeField, Tooltip("Destroy the cube for everyone when the pattern ends.")]
    private bool _DestroyWhenFinished = true;

    [SerializeField, Tooltip("Seconds to wait after finishing before destroying.")]
    private float _DestroyDelay = 2f;

    private Rigidbody _Rigidbody;

    /// <summary>
    /// Kinematic so MovePosition/MoveRotation fully control the cube
    /// while it still pushes other non-kinematic rigidbodies.
    /// </summary>
    private void Awake()
    {
        _Rigidbody = GetComponent<Rigidbody>();
        _Rigidbody.isKinematic = true;
        _Rigidbody.useGravity = false;
    }

    /// <summary>
    /// Smooth physics on the server; pure clients let NetworkTransform handle smoothing.
    /// </summary>
    public override void OnStartClient()
    {
        if (!isServer)
        {
            _Rigidbody.interpolation = RigidbodyInterpolation.None;
        }
    }

    /// <summary>
    /// Starts the movement pattern. Runs only on the server (and host).
    /// </summary>
    public override void OnStartServer()
    {
        _Rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        StartCoroutine(RunPattern());
    }

    [Server]
    private IEnumerator RunPattern()
    {
        for (int turn = 0; turn < _Turns; turn++)
        {
            for (int step = 0; step < _StepsPerLeg; step++)
            {
                yield return MoveStep();
            }
            yield return Turn();
        }

        if (_DestroyWhenFinished)
        {
            yield return new WaitForSeconds(_DestroyDelay);
            NetworkServer.Destroy(gameObject);
        }
    }

    /// <summary>
    /// Moves one step along the cube's current forward direction.
    /// </summary>
    [Server]
    private IEnumerator MoveStep()
    {
        Vector3 start = _Rigidbody.position;
        Vector3 direction = _Rigidbody.rotation * Vector3.forward;
        Vector3 target = start + direction * _StepSize;
        float elapsed = 0f;

        while (elapsed < _StepDuration)
        {
            yield return new WaitForFixedUpdate();
            elapsed += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(elapsed / _StepDuration);
            _Rigidbody.MovePosition(Vector3.Lerp(start, target, t));
        }
    }

    /// <summary>
    /// Rotates around the Y axis by the turn angle.
    /// </summary>
    [Server]
    private IEnumerator Turn()
    {
        Quaternion start = _Rigidbody.rotation;
        Quaternion target = start * Quaternion.Euler(0f, _TurnAngle, 0f);
        float elapsed = 0f;

        while (elapsed < _TurnDuration)
        {
            yield return new WaitForFixedUpdate();
            elapsed += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(elapsed / _TurnDuration);
            _Rigidbody.MoveRotation(Quaternion.Slerp(start, target, t));
        }
    }
}