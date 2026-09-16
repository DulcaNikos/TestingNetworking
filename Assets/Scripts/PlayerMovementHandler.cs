using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace SteamLobbyN
{
    public class PlayerMovementHandler : NetworkBehaviour
    {
        [SerializeField, Tooltip("")]
        private float _MoveSpeed = 5f;

        [SerializeField]
        private PlayerInput _playerInput;

        [SerializeField]
        private GameObject _PlayerModel;

        private Vector2 _MoveInput;

        // [SyncVar(hook = nameof(OnColorChanged))]
        // private Color _color;

        // public override void OnStartServer() => _color = Random.ColorHSV(0f, 1f, 0.6f, 1f, 0.7f, 1f);

        // public override void OnStartClient() => GetComponentInChildren<Renderer>().material.color = _color;

        // private void OnColorChanged(Color _old, Color _new) => GetComponentInChildren<Renderer>().material.color = _new;

        void Awake()
        {
            if (_playerInput == null) _playerInput = GetComponent<PlayerInput>();
            _playerInput.enabled = false;
            _PlayerModel.SetActive(false);
        }

        // public override void OnStartLocalPlayer()
        // {
        //     _playerInput.enabled = true;
        // }

        // private void Start()
        // {
        //     _PlayerModel.SetActive(false);
        // }

        void OnMove(InputValue value)
        {
            _MoveInput = value.Get<Vector2>();
        }

        void Update()
        {
            if (SceneManager.GetActiveScene().name == "Game")
            {
                if (_PlayerModel.activeSelf == false)
                {
                    _playerInput.enabled = true;
                    _PlayerModel.SetActive(true);
                }

                if (isOwned)
                {
                    Vector3 movement = new Vector3(_MoveInput.x, 0f, _MoveInput.y) * _MoveSpeed * Time.deltaTime;
                    transform.Translate(movement, Space.World);
                }
            }

        }
    }
}
