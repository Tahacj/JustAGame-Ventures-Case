using System;
using JustAGame.Core.Network;
using TMPro;
using UnityEngine;

namespace JustAGame.UI
{
    /// <summary>
    /// Displays the player's total walking distance and achievement progress on screen.
    /// Exclusively uses TextMeshProUGUI (TMP) for high-fidelity text rendering.
    /// Adheres strictly to repository DESIGN_PATTERNS.md zero-slop guidelines.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerDistanceUI : MonoBehaviour
    {
        [Header("TextMeshPro UI Reference")]
        [Tooltip("Assign your TextMeshProUGUI component here. If left unassigned, it will auto-detect in Awake.")]
        [SerializeField] private TextMeshProUGUI distanceTMP;

        [Header("Format Settings")]
        [SerializeField] private string normalFormat = "Distance: {0:F1}m / {1:F0}m ({2:F0}%)";
        [SerializeField] private string unlockedFormat = "Distance: {0:F1}m <color=#80FF80>🏆 100m Unlocked!</color>";

        private float _cachedDistance = 0f;
        private float _cachedTarget = 100f;
        private bool _cachedUnlocked = false;

        private void Awake()
        {
            try
            {
                // Auto-detect TextMeshProUGUI component if not explicitly assigned
                if (distanceTMP.IsNull())
                {
                    distanceTMP = this.GetComponentOrNull<TextMeshProUGUI>();
                    if (distanceTMP.IsNull())
                    {
                        distanceTMP = this.GetInChildrenOrNull<TextMeshProUGUI>();
                    }
                    else
                    {
                        // Found on current GameObject
                    }
                }
                else
                {
                    // TextMeshProUGUI already assigned
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerDistanceUI] Exception in Awake: {ex.Message}");
            }
        }

        private void Start()
        {
            try
            {
                // Secondary check in Start in case canvas hierarchy initialized after Awake
                if (distanceTMP.IsNull())
                {
                    distanceTMP = this.GetComponentOrNull<TextMeshProUGUI>() ?? this.GetInChildrenOrNull<TextMeshProUGUI>();
                }
                else
                {
                    // TMP already assigned
                }

                RefreshDisplay();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerDistanceUI] Exception in Start: {ex.Message}");
            }
        }

        private void OnEnable()
        {
            try
            {
                EOSPlayerStatsTracker.OnDistanceUpdated += HandleDistanceUpdated;
                EOSPlayerStatsTracker.OnAchievementUnlockedEvent += HandleAchievementUnlocked;

                // Sync current state from LocalInstance if active
                if (EOSPlayerStatsTracker.LocalInstance.IsNotNull())
                {
                    _cachedDistance = EOSPlayerStatsTracker.LocalInstance.TotalDistanceWalked;
                    _cachedUnlocked = EOSPlayerStatsTracker.LocalInstance.IsAchievementUnlocked;
                }
                else
                {
                    // Player instance not yet spawned
                }

                RefreshDisplay();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerDistanceUI] Exception in OnEnable: {ex.Message}");
            }
        }

        private void OnDisable()
        {
            try
            {
                EOSPlayerStatsTracker.OnDistanceUpdated -= HandleDistanceUpdated;
                EOSPlayerStatsTracker.OnAchievementUnlockedEvent -= HandleAchievementUnlocked;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerDistanceUI] Exception in OnDisable: {ex.Message}");
            }
        }

        private void Update()
        {
            try
            {
                // Continually poll LocalInstance to ensure real-time UI updates
                if (EOSPlayerStatsTracker.LocalInstance.IsNotNull())
                {
                    float currentDist = EOSPlayerStatsTracker.LocalInstance.TotalDistanceWalked;
                    bool isUnlocked = EOSPlayerStatsTracker.LocalInstance.IsAchievementUnlocked;

                    if (Math.Abs(currentDist - _cachedDistance) > 0.05f || isUnlocked != _cachedUnlocked)
                    {
                        _cachedDistance = currentDist;
                        _cachedUnlocked = isUnlocked;
                        RefreshDisplay();
                    }
                    else
                    {
                        // Distance unchanged
                    }
                }
                else
                {
                    // Local player tracker not yet instantiated
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerDistanceUI] Exception in Update: {ex.Message}");
            }
        }

        private void HandleDistanceUpdated(float currentMeters, float targetMeters)
        {
            try
            {
                _cachedDistance = currentMeters;
                _cachedTarget = targetMeters;
                RefreshDisplay();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerDistanceUI] Exception in HandleDistanceUpdated: {ex.Message}");
            }
        }

        private void HandleAchievementUnlocked(string achievementId)
        {
            try
            {
                _cachedUnlocked = true;
                RefreshDisplay();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerDistanceUI] Exception in HandleAchievementUnlocked: {ex.Message}");
            }
        }

        private void RefreshDisplay()
        {
            try
            {
                if (distanceTMP.IsNull())
                {
                    return;
                }
                else
                {
                    float progressPercent = Mathf.Clamp01(_cachedDistance / Mathf.Max(1f, _cachedTarget)) * 100f;
                    string displayText;

                    if (_cachedUnlocked || _cachedDistance >= _cachedTarget)
                    {
                        displayText = string.Format(unlockedFormat, _cachedDistance);
                    }
                    else
                    {
                        displayText = string.Format(normalFormat, _cachedDistance, _cachedTarget, progressPercent);
                    }

                    distanceTMP.text = displayText;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerDistanceUI] Exception in RefreshDisplay: {ex.Message}");
            }
        }
    }
}
