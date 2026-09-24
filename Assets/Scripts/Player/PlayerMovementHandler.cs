using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SteamLobbyN
{
    public class PlayerMovementHandler : NetworkBehaviour
    {
        [SerializeField, Tooltip("")]
        private float _MoveSpeed = 5f;

        private Vector2 _MoveInput;

        [SyncVar(hook = nameof(OnColorChanged))]
        private Color _color;

        void OnMove(InputValue value)
        {
            _MoveInput = value.Get<Vector2>();
        }

        void Update()
        {
            if (isLocalPlayer)
            {
                Vector3 movement = new Vector3(_MoveInput.x, 0f, _MoveInput.y) * _MoveSpeed * Time.deltaTime;
                transform.Translate(movement, Space.World);
            }
        }

        //Color
        public override void OnStartServer()
        {
            _color = Random.ColorHSV(0f, 1f, 0.6f, 1f, 0.7f, 1f);
        }

        public override void OnStartClient()
        {
            GetComponent<Renderer>().material.color = _color;
        }

        private void OnColorChanged(Color _old, Color _new)
        {
            GetComponent<Renderer>().material.color = _new;
        }
    }
}
