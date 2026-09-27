using System;
using Epic.OnlineServices;
using Epic.OnlineServices.Achievements;
using Epic.OnlineServices.Stats;
using EpicTransport;
using JustAGame;
using UnityEngine;

namespace JustAGame.Core.Network
{
    /// <summary>
    /// Tracks client authoritative walking distance, ingests distance metrics into EOS Stats Interface,
    /// and unlocks the 100m walking achievement through the EOS Achievements Interface.
    /// Follows strict repository DESIGN_PATTERNS.md zero-slop guidelines.
    /// Pure MonoBehaviour attached to the local player to guarantee zero Mirror netIdentity NREs.
    /// </summary>
    [DisallowMultipleComponent]
    public class EOSPlayerStatsTracker : MonoBehaviour
    {
        public const string STAT_NAME_DISTANCE = "DISTANCE_WALKED";
        public const string ACHIEVEMENT_ID_100M = "WALK_100M";
        public const float ACHIEVEMENT_TARGET_DISTANCE = 100.0f;
        public const float METRIC_INGEST_INTERVAL_METERS = 5.0f;

        [Header("Distance Tracking")]
        [SerializeField] private float totalDistanceWalked = 0f;
        [SerializeField] private bool achievementUnlocked = false;

        public float TotalDistanceWalked => totalDistanceWalked;
        public bool IsAchievementUnlocked => achievementUnlocked;

        public static EOSPlayerStatsTracker LocalInstance { get; private set; }

        public static event Action<float, float> OnDistanceUpdated;
        public static event Action<string> OnAchievementUnlockedEvent;

        private float _unreportedDistance = 0f;

        public void InitializeLocal()
        {
            try
            {
                LocalInstance = this;
                Debug.Log($"[EOSPlayerStatsTracker] Local tracking initialized for player: {gameObject.name}");
                OnDistanceUpdated?.Invoke(totalDistanceWalked, ACHIEVEMENT_TARGET_DISTANCE);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EOSPlayerStatsTracker] Exception in InitializeLocal: {ex.Message}");
            }
        }

        private void Awake()
        {
            try
            {
                if (LocalInstance.IsNull())
                {
                    LocalInstance = this;
                }
                else
                {
                    // Existing instance registered
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EOSPlayerStatsTracker] Exception in Awake: {ex.Message}");
            }
        }

        private void OnDestroy()
        {
            try
            {
                if (LocalInstance == this)
                {
                    LocalInstance = null;
                }
                else
                {
                    // Secondary instance
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EOSPlayerStatsTracker] Exception in OnDestroy: {ex.Message}");
            }
        }

        /// <summary>
        /// Accumulates walked distance in meters. Called by ClientAuthoritativeMovement on movement.
        /// </summary>
        public void AddWalkedDistance(float distanceInMeters)
        {
            try
            {
                if (distanceInMeters <= 0.0001f || distanceInMeters > 50.0f)
                {
                    // Discard negligible or extreme snap/teleport distances
                    return;
                }
                else
                {
                    totalDistanceWalked += distanceInMeters;
                    _unreportedDistance += distanceInMeters;

                    // Notify UI listeners
                    OnDistanceUpdated?.Invoke(totalDistanceWalked, ACHIEVEMENT_TARGET_DISTANCE);

                    // Milestone check for 100m achievement
                    if (totalDistanceWalked >= ACHIEVEMENT_TARGET_DISTANCE && !achievementUnlocked)
                    {
                        achievementUnlocked = true;
                        TriggerAchievementUnlock();
                    }
                    else
                    {
                        // Milestone not reached or already unlocked
                    }

                    // Ingest stat in batches to prevent network spam
                    if (_unreportedDistance >= METRIC_INGEST_INTERVAL_METERS)
                    {
                        IngestDistanceMetric((int)_unreportedDistance);
                        _unreportedDistance = 0f;
                    }
                    else
                    {
                        // Accumulating distance for next batch
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EOSPlayerStatsTracker] Exception in AddWalkedDistance: {ex.Message}");
            }
        }

        private void TriggerAchievementUnlock()
        {
            try
            {
                Debug.Log("[EOSPlayerStatsTracker] 🏆 ACHIEVEMENT UNLOCKED: Century Walker (100m Walked)!");
                OnAchievementUnlockedEvent?.Invoke(ACHIEVEMENT_ID_100M);

                if (!EOSSDKComponent.Initialized)
                {
                    Debug.LogWarning("[EOSPlayerStatsTracker] EOS SDK not initialized. Local achievement registered.");
                    return;
                }
                else
                {
                    ProductUserId localPuid = EOSSDKComponent.LocalUserProductId;
                    if (localPuid.IsNull())
                    {
                        Debug.LogWarning("[EOSPlayerStatsTracker] Local ProductUserId is null. Local achievement registered.");
                        return;
                    }
                    else
                    {
                        var achievementsInterface = EOSSDKComponent.GetAchievementsInterface();
                        if (achievementsInterface.IsNull())
                        {
                            Debug.LogWarning("[EOSPlayerStatsTracker] AchievementsInterface is null.");
                            return;
                        }
                        else
                        {
                            var unlockOptions = new UnlockAchievementsOptions
                            {
                                UserId = localPuid,
                                AchievementIds = new string[] { ACHIEVEMENT_ID_100M }
                            };

                            achievementsInterface.UnlockAchievements(unlockOptions, null, (OnUnlockAchievementsCompleteCallbackInfo callbackInfo) =>
                            {
                                try
                                {
                                    if (callbackInfo.ResultCode == Result.Success)
                                    {
                                        Debug.Log($"[EOSPlayerStatsTracker] Successfully pushed unlock for '{ACHIEVEMENT_ID_100M}' to EOS Backend!");
                                    }
                                    else
                                    {
                                        Debug.Log($"[EOSPlayerStatsTracker] EOS Achievements Unlock returned: {callbackInfo.ResultCode} (Ensure '{ACHIEVEMENT_ID_100M}' definition is configured in Epic Developer Portal).");
                                    }
                                }
                                catch (Exception cbEx)
                                {
                                    Debug.LogError($"[EOSPlayerStatsTracker] Exception in UnlockAchievements callback: {cbEx.Message}");
                                }
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EOSPlayerStatsTracker] Exception in TriggerAchievementUnlock: {ex.Message}");
            }
        }

        private void IngestDistanceMetric(int metersWalked)
        {
            try
            {
                if (metersWalked <= 0)
                {
                    return;
                }
                else
                {
                    Debug.Log($"[EOSPlayerStatsTracker] Ingesting distance metric: +{metersWalked}m (Total: {totalDistanceWalked:F1}m)");

                    if (!EOSSDKComponent.Initialized)
                    {
                        return;
                    }
                    else
                    {
                        ProductUserId localPuid = EOSSDKComponent.LocalUserProductId;
                        if (localPuid.IsNull())
                        {
                            return;
                        }
                        else
                        {
                            var statsInterface = EOSSDKComponent.GetStatsInterface();
                            if (statsInterface.IsNull())
                            {
                                return;
                            }
                            else
                            {
                                var ingestOptions = new IngestStatOptions
                                {
                                    LocalUserId = localPuid,
                                    TargetUserId = localPuid,
                                    Stats = new IngestData[]
                                    {
                                        new IngestData
                                        {
                                            StatName = STAT_NAME_DISTANCE,
                                            IngestAmount = metersWalked
                                        }
                                    }
                                };

                                statsInterface.IngestStat(ingestOptions, null, (IngestStatCompleteCallbackInfo callbackInfo) =>
                                {
                                    try
                                    {
                                        if (callbackInfo.ResultCode == Result.Success)
                                        {
                                            Debug.Log($"[EOSPlayerStatsTracker] Successfully ingested {metersWalked}m for stat '{STAT_NAME_DISTANCE}' to EOS Backend!");
                                        }
                                        else
                                        {
                                            Debug.Log($"[EOSPlayerStatsTracker] EOS Stats Ingest returned: {callbackInfo.ResultCode} (Stat '{STAT_NAME_DISTANCE}' can be registered in Developer Portal > Stats).");
                                        }
                                    }
                                    catch (Exception cbEx)
                                    {
                                        Debug.LogError($"[EOSPlayerStatsTracker] Exception in IngestStat callback: {cbEx.Message}");
                                    }
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EOSPlayerStatsTracker] Exception in IngestDistanceMetric: {ex.Message}");
            }
        }
    }
}
