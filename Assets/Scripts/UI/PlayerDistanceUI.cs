using System;
using JustAGame.Core.Network;
using TMPro;
using UnityEngine;

namespace JustAGame.UI
{
    /// <summary>
    /// Displays the player's total walking distance and achievement progress on screen.
    /// Exclusively uses TextMeshProUGUI (TMP) for high-fidelity text rendering.
    /// Implements dynamic 3-color status:
    /// - RED (Default): Milestone not yet reached (< 100m).
    /// - YELLOW: Milestone met locally (>= 100m), but awaiting EOS backend verification.
    /// - GREEN: Achievement verified and synced on the EOS backend (updates immediately on login).
    /// Adheres strictly to repository DESIGN_PATTERNS.md zero-slop guidelines.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerDistanceUI : MonoBehaviour
    {
        [Header("TextMeshPro UI Reference")]
        [Tooltip("Assign your TextMeshProUGUI component here. If left unassigned, it will auto-detect in Awake.")]
        [SerializeField] private TextMeshProUGUI distanceTMP;

        [Header("Color Settings (3-State Sync)")]
        [Tooltip("Default color when the achievement is NOT yet achieved (< 100m).")]
        [SerializeField] private Color notAchievedColor = new Color(1.0f, 0.33f, 0.33f, 1.0f); // Red

        [Tooltip("Color when the distance milestone (100m) is met locally, but NOT yet synced with EOS backend.")]
        [SerializeField] private Color milestoneMetUnsyncedColor = new Color(1.0f, 0.85f, 0.0f, 1.0f); // Yellow

        [Tooltip("Color when the achievement IS verified and synced on the EOS backend.")]
        [SerializeField] private Color syncedColor = new Color(0.33f, 1.0f, 0.33f, 1.0f); // Green

        [Header("Format Settings")]
        [SerializeField] private string normalFormat = "Distance: {0:F1}m / {1:F0}m ({2:F0}%)";
        [SerializeField] private string unsyncedFormat = "Distance: {0:F1}m [100m Met - Syncing with EOS...]";
        [SerializeField] private string syncedFormat = "Distance: {0:F1}m [100m Unlocked & Synced]";

        private float _cachedDistance = 0f;
        private float _cachedTarget = 100f;
        private bool _cachedUnlocked = false;
        private bool _cachedSynced = false;

        private void Awake()
        {
            try
            {
                // Sanitize any legacy emoji from serialized inspector fields to prevent TMP font missing glyph warnings
                if (syncedFormat.IsNotNull() && (syncedFormat.Contains("\U0001F3C6") || syncedFormat.Contains("🏆")))
                {
                    syncedFormat = syncedFormat.Replace("\U0001F3C6", "[UNLOCKED]").Replace("🏆", "[UNLOCKED]");
                }
                else
                {
                    // No emoji present in format
                }

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

                // Initialize default visual appearance to RED
                if (distanceTMP.IsNotNull())
                {
                    distanceTMP.color = notAchievedColor;
                }
                else
                {
                    // TMP not yet resolved
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
                EOSPlayerStatsTracker.OnAchievementSyncStatusChanged += HandleSyncStatusChanged;

                // Sync current state from LocalInstance if active
                if (EOSPlayerStatsTracker.LocalInstance.IsNotNull())
                {
                    _cachedDistance = EOSPlayerStatsTracker.LocalInstance.TotalDistanceWalked;
                    _cachedUnlocked = EOSPlayerStatsTracker.LocalInstance.IsAchievementUnlocked;
                    _cachedSynced = EOSPlayerStatsTracker.LocalInstance.IsAchievementBackendSynced;
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
                EOSPlayerStatsTracker.OnAchievementSyncStatusChanged -= HandleSyncStatusChanged;
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
                    bool isSynced = EOSPlayerStatsTracker.LocalInstance.IsAchievementBackendSynced;

                    if (Math.Abs(currentDist - _cachedDistance) > 0.05f || isUnlocked != _cachedUnlocked || isSynced != _cachedSynced)
                    {
                        _cachedDistance = currentDist;
                        _cachedUnlocked = isUnlocked;
                        _cachedSynced = isSynced;
                        RefreshDisplay();
                    }
                    else
                    {
                        // Distance and sync state unchanged
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

        private void HandleSyncStatusChanged(bool isSynced)
        {
            try
            {
                _cachedSynced = isSynced;
                RefreshDisplay();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerDistanceUI] Exception in HandleSyncStatusChanged: {ex.Message}");
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
                    // State 1: Synced with EOS Backend -> GREEN
                    if (_cachedSynced)
                    {
                        distanceTMP.color = syncedColor;
                        distanceTMP.text = string.Format(syncedFormat, _cachedDistance);
                    }
                    // State 2: Milestone Met Locally (>= 100m) but Unsynced -> YELLOW
                    else if (_cachedDistance >= _cachedTarget || _cachedUnlocked)
                    {
                        distanceTMP.color = milestoneMetUnsyncedColor;
                        distanceTMP.text = string.Format(unsyncedFormat, _cachedDistance);
                    }
                    // State 3: Not Yet Achieved (< 100m) -> RED (Default)
                    else
                    {
                        float progressPercent = Mathf.Clamp01(_cachedDistance / Mathf.Max(1f, _cachedTarget)) * 100f;
                        distanceTMP.color = notAchievedColor;
                        distanceTMP.text = string.Format(normalFormat, _cachedDistance, _cachedTarget, progressPercent);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerDistanceUI] Exception in RefreshDisplay: {ex.Message}");
            }
        }
    }
}
