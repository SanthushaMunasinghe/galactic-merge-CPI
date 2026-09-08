using System;
using System.Collections;
using System.Collections.Generic;
using Oxtail.Utils;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    /// <summary>
    /// Spawns waves of AsteroidProjectile instances around a vertical oval in this transform's local
    /// X/Y plane (Spawn Radius for the X extent, scaled by Oval Height Multiplier for the Y extent),
    /// each sliding inward at the same angle to a point on a smaller, same-ratio oval (Jump Radius),
    /// offset along local Z by Jump Height Offset, before homing the rest of the way toward the shared
    /// local center. Asteroids within a wave always spawn one at a time. A wave is only ever started
    /// externally via TriggerWave (or the "Trigger Wave" context menu item while testing); a call
    /// while a wave is already in progress (including asteroids still mid-jump) is ignored. The
    /// spawn/jump oval gizmo only draws outside Play Mode.
    ///
    /// If Use Manual Wave Spawn Points is on, TriggerWave instead spawns one asteroid per not-yet-used
    /// child of the corresponding entry in Manual Wave Spawn Point Parents (same one-at-a-time timing/
    /// gating as the random path) rather than picking random positions; each point spawns at most once
    /// ever, even across repeated/overflowing wave triggers. Use the "Generate Manual Wave Spawn
    /// Points" button (or context menu item) to create a new hand-editable wave (its points start
    /// arranged on the spawn oval, then can be freely moved/added/deleted in the Scene view).
    /// </summary>
    public class AsteroidSpawnManager : MonoBehaviour
    {
        [Header("Prefab")]
        [SerializeField] private AsteroidProjectile m_AsteroidPrefab;

        [Header("Spawn Area")]
        [SerializeField, Min(0f)] private float m_SpawnRadius = 10f;
        [SerializeField, Min(0f)] private float m_JumpRadius = 4f;
        [SerializeField] private float m_JumpHeightOffset = -0.5f;
        [SerializeField, Min(0.01f)] private float m_OvalHeightMultiplier = 1.5f;

        [Header("Wave Settings")]
        [SerializeField] private List<int> m_WaveAsteroidCounts = new List<int> { 5 };
        [SerializeField] private Vector2 m_SpawnIntervalRange = new Vector2(0.2f, 1f);
        [SerializeField] private Vector2 m_JumpSpeedRange = new Vector2(2f, 6f);
        [SerializeField] private Vector2 m_CenteringSpeedRange = new Vector2(2f, 6f);

        [Header("Collect Point")]
        [SerializeField] private CollectPoint m_CollectPointPrefab;
        [SerializeField] private float m_CollectPointSpeed = 5f;

        [Header("Manual Wave Spawn Points")]
        [SerializeField] private bool m_UseManualWaveSpawnPoints;
        [SerializeField] private GameObject m_SpawnPointPrefab;
        [SerializeField, Min(1)] private int m_SpawnPointCount = 5;
        [SerializeField] private List<Transform> m_ManualWaveSpawnPointParents = new List<Transform>();

        private int m_CurrentWaveIndex;
        private bool m_IsWaveInProgress;
        private int m_AsteroidsPendingJump;
        private bool m_WaveSpawningComplete;
        private readonly HashSet<Transform> m_UsedManualSpawnPoints = new HashSet<Transform>();

        /// <summary>Fired once every asteroid from the current wave has spawned and been destroyed.</summary>
        public event Action OnWaveCleared;

        private void OnEnable()
        {
            EventManager<AsteroidDestroyedByBulletEvent>.AddListener(OnAsteroidDestroyedByBullet);
            EventManager<AsteroidDestroyedByPlanetEvent>.AddListener(OnAsteroidDestroyedByPlanet);
        }

        private void OnDisable()
        {
            EventManager<AsteroidDestroyedByBulletEvent>.RemoveListener(OnAsteroidDestroyedByBullet);
            EventManager<AsteroidDestroyedByPlanetEvent>.RemoveListener(OnAsteroidDestroyedByPlanet);
        }

        [ContextMenu("Trigger Wave")]
        public void TriggerWave()
        {
            if (m_IsWaveInProgress)
            {
                Debug.Log($"{nameof(AsteroidSpawnManager)}: a wave is already in progress, ignoring TriggerWave.", this);
                return;
            }

            if (m_UseManualWaveSpawnPoints)
            {
                TriggerManualWave();
                return;
            }

            if (m_WaveAsteroidCounts == null || m_WaveAsteroidCounts.Count == 0)
            {
                Debug.LogError($"{nameof(AsteroidSpawnManager)}: no waves configured in Wave Asteroid Counts.", this);
                return;
            }

            int waveCount = m_WaveAsteroidCounts[Mathf.Min(m_CurrentWaveIndex, m_WaveAsteroidCounts.Count - 1)];
            m_CurrentWaveIndex++;

            StartCoroutine(SpawnWaveCO(waveCount));
        }

        private void TriggerManualWave()
        {
            if (m_ManualWaveSpawnPointParents == null || m_ManualWaveSpawnPointParents.Count == 0)
            {
                Debug.LogError($"{nameof(AsteroidSpawnManager)}: no manual wave spawn points configured.", this);
                return;
            }

            Transform waveParent = m_ManualWaveSpawnPointParents[Mathf.Min(m_CurrentWaveIndex, m_ManualWaveSpawnPointParents.Count - 1)];
            m_CurrentWaveIndex++;

            if (waveParent == null)
            {
                // A parent entry can go null if it was deleted in the Hierarchy after being added to
                // the list. Logging and returning here (rather than letting the foreach below throw)
                // is what matters: an uncaught exception here would unwind back through
                // CPIManager.SetWaveState mid-way, leaving m_IsWaveActive stuck true and skipping the
                // CPIWaveStateChangedEvent that closes UI for the new wave.
                Debug.LogError($"{nameof(AsteroidSpawnManager)}: the selected manual wave spawn point parent is missing.", this);
                return;
            }

            List<Transform> spawnPoints = new List<Transform>();
            foreach (Transform point in waveParent)
            {
                if (point != null && !m_UsedManualSpawnPoints.Contains(point))
                    spawnPoints.Add(point);
            }

            ShuffleInPlace(spawnPoints);

            StartCoroutine(SpawnManualWaveCO(spawnPoints));
        }

        /// <summary>Fisher-Yates shuffle: randomizes spawn order while still spawning each point exactly once.</summary>
        private static void ShuffleInPlace(List<Transform> points)
        {
            for (int i = points.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                Transform temp = points[i];
                points[i] = points[j];
                points[j] = temp;
            }
        }

        private IEnumerator SpawnWaveCO(int count)
        {
            m_IsWaveInProgress = true;
            m_WaveSpawningComplete = false;

            for (int i = 0; i < count; i++)
            {
                SpawnAsteroid();

                float interval = UnityEngine.Random.Range(m_SpawnIntervalRange.x, m_SpawnIntervalRange.y);
                yield return new WaitForSeconds(interval);
            }

            m_WaveSpawningComplete = true;
            CheckWaveCleared();

            // Only allow the next wave once every asteroid from this one has finished its jump
            // (either by reaching the jump radius, or by being destroyed before it got there).
            yield return new WaitUntil(() => m_AsteroidsPendingJump <= 0);

            m_IsWaveInProgress = false;
        }

        private IEnumerator SpawnManualWaveCO(List<Transform> spawnPoints)
        {
            m_IsWaveInProgress = true;
            m_WaveSpawningComplete = false;

            for (int i = 0; i < spawnPoints.Count; i++)
            {
                Transform point = spawnPoints[i];
                if (point != null)
                {
                    // Marked used at the moment it actually spawns (not when queued), so it never
                    // spawns again on a later wave trigger, while still leaving the interval below.
                    m_UsedManualSpawnPoints.Add(point);
                    SpawnAsteroidAt(point.position);
                }

                float interval = UnityEngine.Random.Range(m_SpawnIntervalRange.x, m_SpawnIntervalRange.y);
                yield return new WaitForSeconds(interval);
            }

            m_WaveSpawningComplete = true;
            CheckWaveCleared();

            yield return new WaitUntil(() => m_AsteroidsPendingJump <= 0);

            m_IsWaveInProgress = false;
        }

        private void SpawnAsteroid()
        {
            if (m_AsteroidPrefab == null)
            {
                Debug.LogError($"{nameof(AsteroidSpawnManager)}: Asteroid Prefab must be assigned.", this);
                return;
            }

            float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            Vector3 spawnLocalPos = new Vector3(Mathf.Cos(angle) * m_SpawnRadius, Mathf.Sin(angle) * m_SpawnRadius * m_OvalHeightMultiplier, 0f);

            SpawnAsteroidAtLocal(spawnLocalPos, angle);
        }

        private void SpawnAsteroidAt(Vector3 worldPosition)
        {
            if (m_AsteroidPrefab == null)
            {
                Debug.LogError($"{nameof(AsteroidSpawnManager)}: Asteroid Prefab must be assigned.", this);
                return;
            }

            Vector3 localPos = transform.InverseTransformPoint(worldPosition);
            float angle = Mathf.Atan2(localPos.y / m_OvalHeightMultiplier, localPos.x);

            SpawnAsteroidAtLocal(localPos, angle);
        }

        private void SpawnAsteroidAtLocal(Vector3 spawnLocalPos, float angle)
        {
            Vector3 jumpLocalPos = new Vector3(Mathf.Cos(angle) * m_JumpRadius, Mathf.Sin(angle) * m_JumpRadius * m_OvalHeightMultiplier, m_JumpHeightOffset);

            AsteroidProjectile asteroid = Instantiate(m_AsteroidPrefab, transform.TransformPoint(spawnLocalPos), Quaternion.identity, transform);
            m_AsteroidsPendingJump++;

            float jumpSpeed = UnityEngine.Random.Range(m_JumpSpeedRange.x, m_JumpSpeedRange.y);
            float centeringSpeed = UnityEngine.Random.Range(m_CenteringSpeedRange.x, m_CenteringSpeedRange.y);
            asteroid.Initialize(jumpLocalPos, jumpSpeed, centeringSpeed, OnAsteroidJumpComplete);
        }

        [ContextMenu("Generate Manual Wave Spawn Points")]
        public void GenerateManualWaveSpawnPoints()
        {
            if (m_SpawnPointPrefab == null)
            {
                Debug.LogError($"{nameof(AsteroidSpawnManager)}: Spawn Point Prefab must be assigned.", this);
                return;
            }

            GameObject waveParentObject = new GameObject($"ManualWave{m_ManualWaveSpawnPointParents.Count}");
            waveParentObject.transform.SetParent(transform, false);

#if UNITY_EDITOR
            UnityEditor.Undo.RegisterCreatedObjectUndo(waveParentObject, "Generate Manual Wave Spawn Points");
#endif

            for (int i = 0; i < m_SpawnPointCount; i++)
            {
                float angle = i * (Mathf.PI * 2f / m_SpawnPointCount);
                Vector3 localPos = new Vector3(Mathf.Cos(angle) * m_SpawnRadius, Mathf.Sin(angle) * m_SpawnRadius * m_OvalHeightMultiplier, 0f);

#if UNITY_EDITOR
                GameObject point = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(m_SpawnPointPrefab, waveParentObject.transform);
                UnityEditor.Undo.RegisterCreatedObjectUndo(point, "Generate Manual Wave Spawn Points");
#else
                GameObject point = Instantiate(m_SpawnPointPrefab, waveParentObject.transform);
#endif
                point.transform.localPosition = localPos;
            }

            m_ManualWaveSpawnPointParents.Add(waveParentObject.transform);

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        private void OnAsteroidJumpComplete()
        {
            m_AsteroidsPendingJump--;
        }

        private void OnAsteroidDestroyedByBullet(AsteroidDestroyedByBulletEvent evt)
        {
            CheckWaveCleared();
        }

        private void OnAsteroidDestroyedByPlanet(AsteroidDestroyedByPlanetEvent evt)
        {
            CheckWaveCleared();
        }

        private void CheckWaveCleared()
        {
            if (m_WaveSpawningComplete && AsteroidProjectile.ActiveAsteroids.Count == 0)
                OnWaveCleared?.Invoke();
        }

        public void SpawnCollectPoint(Vector3 worldPosition)
        {
            if (m_CollectPointPrefab == null)
            {
                Debug.LogError($"{nameof(AsteroidSpawnManager)}: Collect Point Prefab must be assigned.", this);
                return;
            }

            CollectPoint collectPoint = Instantiate(m_CollectPointPrefab, worldPosition, Quaternion.identity, transform);
            collectPoint.Initialize(m_CollectPointSpeed);
        }

        private void OnValidate()
        {
            if (m_JumpRadius > m_SpawnRadius)
                Debug.LogWarning($"{nameof(AsteroidSpawnManager)}: Jump Radius is usually smaller than Spawn Radius.", this);
        }

        private void OnDrawGizmosSelected()
        {
            // Edit-time only: once Play Mode starts, asteroids visualize the real spawn/jump
            // positions themselves, so the ring gizmo would just be visual clutter.
            if (Application.isPlaying)
                return;

            Gizmos.matrix = transform.localToWorldMatrix;

            Gizmos.color = Color.cyan;
            DrawOvalGizmo(m_SpawnRadius, m_SpawnRadius * m_OvalHeightMultiplier);

            Gizmos.color = Color.yellow;
            DrawOvalGizmo(m_JumpRadius, m_JumpRadius * m_OvalHeightMultiplier);
        }

        private static void DrawOvalGizmo(float radiusX, float radiusY)
        {
            const int segments = 48;

            Vector3 previousPoint = new Vector3(radiusX, 0f, 0f);
            for (int i = 1; i <= segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2f;
                Vector3 point = new Vector3(Mathf.Cos(angle) * radiusX, Mathf.Sin(angle) * radiusY, 0f);
                Gizmos.DrawLine(previousPoint, point);
                previousPoint = point;
            }
        }
    }
}
