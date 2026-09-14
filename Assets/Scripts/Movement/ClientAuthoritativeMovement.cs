using Mirror;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace JustAGame.Movement
{
    /// <summary>
    /// Client-authoritative movement controller.
    /// Uses Mirror's built-in NetworkTransform (Unreliable or Reliable) to synchronize 
    /// positions from the local owning client to the server and remote observer clients.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class ClientAuthoritativeMovement : NetworkBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Horizontal movement speed in units/second.")]
        [SerializeField] private float moveSpeed = 6.0f;

        [Tooltip("Rotation smoothing speed.")]
        [SerializeField] private float rotationSpeed = 12.0f;

        [Tooltip("Gravity acceleration.")]
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
            if (CameraFollow.Instance != null)
            {
                CameraFollow.Instance.SetTarget(transform);
            }

            // Ensure NetworkTransform is configured for Client-to-Server authority
            var networkTransform = GetComponent<NetworkTransformBase>();
            if (networkTransform != null)
            {
                networkTransform.syncDirection = SyncDirection.ClientToServer;
            }
        }

        private void Update()
        {
            // Client-authoritative rule: Only the owning client processes local input & movement
            if (!isOwned) return;

            HandleMovement();
        }

        private void HandleMovement()
        {
            Vector2 input = GetMovementInput();
            Vector3 direction = new Vector3(input.x, 0f, input.y);

            // Translate direction relative to camera viewpoint if camera exists
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

            // Apply horizontal velocity
            Vector3 motion = direction * (moveSpeed * Time.deltaTime);

            // Apply gravity
            if (_characterController.isGrounded && _velocity.y < 0)
            {
                _velocity.y = -2f; // Slight downward grounding force
            }
            else
            {
                _velocity.y += gravity * Time.deltaTime;
            }

            motion.y = _velocity.y * Time.deltaTime;

            // Move CharacterController
            _characterController.Move(motion);

            // Smooth rotation towards movement direction
            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(new Vector3(direction.x, 0f, direction.z));
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }

        /// <summary>
        /// Reads input with cross-compatibility for New Input System and Legacy Input.
        /// </summary>
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
            // Automatically ensure that any NetworkTransform on this object is set to ClientToServer
            var nt = GetComponent<NetworkTransformBase>();
            if (nt != null && nt.syncDirection != SyncDirection.ClientToServer)
            {
                nt.syncDirection = SyncDirection.ClientToServer;
            }
        }
    }
}
