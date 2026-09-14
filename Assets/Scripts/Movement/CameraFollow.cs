using UnityEngine;

namespace JustAGame.Movement
{
    /// <summary>
    /// Smoothly follows the local player character.
    /// Attach this to your Main Camera in the scene.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [Tooltip("Target transform to follow. Automatically assigned when the local player spawns.")]
        [SerializeField] private Transform target;

        [Tooltip("Offset relative to the target player.")]
        [SerializeField] private Vector3 offset = new Vector3(0f, 6f, -8f);

        [Tooltip("Camera position follow smoothing.")]
        [SerializeField] private float smoothSpeed = 8f;

        public static CameraFollow Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 desiredPosition = target.position + offset;
            transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
            transform.LookAt(target.position + Vector3.up * 1.2f);
        }
    }
}
