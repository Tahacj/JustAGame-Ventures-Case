using Mirror;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace JustAGame.Movement
{
    [RequireComponent(typeof(CharacterController))]
    public class ClientAuthoritativeMovement : NetworkBehaviour
    {
        [SerializeField] private float moveSpeed = 6.0f;
        [SerializeField] private float rotationSpeed = 12.0f;
        [SerializeField] private float gravity = -9.81f;

        private CharacterController _characterController;
        private Camera _mainCamera;
        private Vector3 _velocity;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
        }

        public override void OnStartAuthority()
        {
            base.OnStartAuthority();
            _mainCamera = Camera.main;

            // Ensure NetworkTransform syncs client inputs to server
            var networkTransform = GetComponent<NetworkTransformBase>();
            if (networkTransform != null)
            {
                networkTransform.syncDirection = SyncDirection.ClientToServer;
            }
        }

        private void Update()
        {
            // Only the owning client processes local input
            if (!isOwned) return;

            HandleMovement();
        }

        private void HandleMovement()
        {
            Vector2 input = GetMovementInput();
            Vector3 direction = new Vector3(input.x, 0f, input.y);

            // Align movement with camera orientation
            if (_mainCamera != null && direction.sqrMagnitude > 0.001f)
            {
                Vector3 camForward = _mainCamera.transform.forward;
                Vector3 camRight = _mainCamera.transform.right;
                camForward.y = 0f;
                camRight.y = 0f;
                camForward.Normalize();
                camRight.Normalize();

                direction = (camForward * direction.z + camRight * direction.x).normalized;
            }

            Vector3 motion = direction * (moveSpeed * Time.deltaTime);

            // Apply gravity and ground check
            if (_characterController.isGrounded && _velocity.y < 0)
            {
                _velocity.y = -2f;
            }
            else
            {
                _velocity.y += gravity * Time.deltaTime;
            }

            motion.y = _velocity.y * Time.deltaTime;
            _characterController.Move(motion);

            // Smoothly rotate towards movement direction
            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(new Vector3(direction.x, 0f, direction.z));
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }

        // Cross-compatible input reading for New and Legacy Input System
        private Vector2 GetMovementInput()
        {
            Vector2 input = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                float x = 0f;
                float y = 0f;
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) y += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) y -= 1f;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x += 1f;
                input = new Vector2(x, y);
            }

            if (Gamepad.current != null && input.sqrMagnitude < 0.001f)
            {
                input = Gamepad.current.leftStick.ReadValue();
            }
#else
            input.x = Input.GetAxisRaw("Horizontal");
            input.y = Input.GetAxisRaw("Vertical");
#endif

            return Vector2.ClampMagnitude(input, 1.0f);
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            var nt = GetComponent<NetworkTransformBase>();
            if (nt != null && nt.syncDirection != SyncDirection.ClientToServer)
            {
                nt.syncDirection = SyncDirection.ClientToServer;
            }
        }
    }
}
