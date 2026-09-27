using System;
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
        private JustAGame.Core.Network.EOSPlayerStatsTracker _statsTracker;

        private void Awake()
        {
            try
            {
                _characterController = this.GetComponentOrNull<CharacterController>();
                if (_characterController.IsNull())
                {
                    Debug.LogError($"[ClientAuthoritativeMovement] CharacterController not found on {name}.");
                }
                else
                {
                    // CharacterController successfully acquired
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ClientAuthoritativeMovement] Exception in Awake: {ex.Message}");
            }
        }

        public override void OnStartAuthority()
        {
            try
            {
                base.OnStartAuthority();
                _mainCamera = Camera.main;

                // Ensure NetworkTransform syncs client inputs to server
                var networkTransform = this.GetComponentOrNull<NetworkTransformBase>();
                if (networkTransform.IsNotNull())
                {
                    networkTransform.syncDirection = SyncDirection.ClientToServer;
                }
                else
                {
                    Debug.LogWarning($"[ClientAuthoritativeMovement] NetworkTransformBase not found on {name}.");
                }

                // Initialize local player stats and distance tracking
                _statsTracker = this.GetComponentOrNull<JustAGame.Core.Network.EOSPlayerStatsTracker>();
                if (_statsTracker.IsNull())
                {
                    _statsTracker = gameObject.AddComponent<JustAGame.Core.Network.EOSPlayerStatsTracker>();
                }
                else
                {
                    // Already attached
                }

                _statsTracker.InitializeLocal();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ClientAuthoritativeMovement] Exception in OnStartAuthority: {ex.Message}");
            }
        }

        private void Update()
        {
            try
            {
                // Only the owning client processes local input once client is ready
                if (!isOwned)
                {
                    return;
                }
                else
                {
                    if (!NetworkClient.ready)
                    {
                        return;
                    }
                    else
                    {
                        HandleMovement();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ClientAuthoritativeMovement] Exception in Update: {ex.Message}");
            }
        }

        private void HandleMovement()
        {
            try
            {
                if (_characterController.IsNull())
                {
                    return;
                }
                else
                {
                    Vector2 input = GetMovementInput();
                    Vector3 direction = new Vector3(input.x, 0f, input.y);

                    // Align movement with camera orientation
                    if (_mainCamera.IsNotNull() && direction.sqrMagnitude > 0.001f)
                    {
                        Vector3 camForward = _mainCamera.transform.forward;
                        Vector3 camRight = _mainCamera.transform.right;
                        camForward.y = 0f;
                        camRight.y = 0f;
                        camForward.Normalize();
                        camRight.Normalize();

                        direction = (camForward * direction.z + camRight * direction.x).normalized;
                    }
                    else
                    {
                        // Camera unavailable or character idle
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

                    // Track horizontal walking distance on local authoritative client
                    Vector3 horizontalMotion = new Vector3(motion.x, 0f, motion.z);
                    float stepDistance = horizontalMotion.magnitude;
                    if (stepDistance > 0.0001f && _statsTracker.IsNotNull())
                    {
                        _statsTracker.AddWalkedDistance(stepDistance);
                    }
                    else
                    {
                        // Idle or stationary
                    }

                    // Smoothly rotate towards movement direction
                    if (direction.sqrMagnitude > 0.001f)
                    {
                        Quaternion targetRotation = Quaternion.LookRotation(new Vector3(direction.x, 0f, direction.z));
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                    }
                    else
                    {
                        // Maintain current rotation
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ClientAuthoritativeMovement] Exception in HandleMovement: {ex.Message}");
            }
        }

        // Cross-compatible input reading for New and Legacy Input System
        private Vector2 GetMovementInput()
        {
            try
            {
                Vector2 input = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
                if (Keyboard.current.IsNotNull())
                {
                    float x = 0f;
                    float y = 0f;

                    if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
                    {
                        y += 1f;
                    }
                    else
                    {
                        // No forward input
                    }

                    if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
                    {
                        y -= 1f;
                    }
                    else
                    {
                        // No backward input
                    }

                    if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                    {
                        x -= 1f;
                    }
                    else
                    {
                        // No leftward input
                    }

                    if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                    {
                        x += 1f;
                    }
                    else
                    {
                        // No rightward input
                    }

                    input = new Vector2(x, y);
                }
                else
                {
                    // Keyboard not available
                }

                if (Gamepad.current.IsNotNull() && input.sqrMagnitude < 0.001f)
                {
                    input = Gamepad.current.leftStick.ReadValue();
                }
                else
                {
                    // Gamepad not connected or input already captured
                }
#else
                input.x = Input.GetAxisRaw("Horizontal");
                input.y = Input.GetAxisRaw("Vertical");
#endif

                return Vector2.ClampMagnitude(input, 1.0f);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ClientAuthoritativeMovement] Exception in GetMovementInput: {ex.Message}");
                return Vector2.zero;
            }
        }

        protected override void OnValidate()
        {
            try
            {
                base.OnValidate();
                var nt = this.GetComponentOrNull<NetworkTransformBase>();
                if (nt.IsNotNull() && nt.syncDirection != SyncDirection.ClientToServer)
                {
                    nt.syncDirection = SyncDirection.ClientToServer;
                }
                else
                {
                    // Already configured or component not present
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ClientAuthoritativeMovement] Exception in OnValidate: {ex.Message}");
            }
        }
    }
}
