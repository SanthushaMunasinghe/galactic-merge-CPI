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

        private int m_CurrentWaveIndex;
        private bool m_IsWaveInProgress;
        private int m_AsteroidsPendingJump;
        private bool m_WaveSpawningComplete;

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

            if (m_WaveAsteroidCounts == null || m_WaveAsteroidCounts.Count == 0)
            {
                Debug.LogError($"{nameof(AsteroidSpawnManager)}: no waves configured in Wave Asteroid Counts.", this);
                return;
            }

            int waveCount = m_WaveAsteroidCounts[Mathf.Min(m_CurrentWaveIndex, m_WaveAsteroidCounts.Count - 1)];
            m_CurrentWaveIndex++;

            StartCoroutine(SpawnWaveCO(waveCount));
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

        private void SpawnAsteroid()
        {
            if (m_AsteroidPrefab == null)
            {
                Debug.LogError($"{nameof(AsteroidSpawnManager)}: Asteroid Prefab must be assigned.", this);
                return;
            }

            float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            Vector3 spawnLocalPos = new Vector3(Mathf.Cos(angle) * m_SpawnRadius, Mathf.Sin(angle) * m_SpawnRadius * m_OvalHeightMultiplier, 0f);
            Vector3 jumpLocalPos = new Vector3(Mathf.Cos(angle) * m_JumpRadius, Mathf.Sin(angle) * m_JumpRadius * m_OvalHeightMultiplier, m_JumpHeightOffset);

            AsteroidProjectile asteroid = Instantiate(m_AsteroidPrefab, transform.TransformPoint(spawnLocalPos), Quaternion.identity, transform);
            m_AsteroidsPendingJump++;

            float jumpSpeed = UnityEngine.Random.Range(m_JumpSpeedRange.x, m_JumpSpeedRange.y);
            float centeringSpeed = UnityEngine.Random.Range(m_CenteringSpeedRange.x, m_CenteringSpeedRange.y);
            asteroid.Initialize(jumpLocalPos, jumpSpeed, centeringSpeed, OnAsteroidJumpComplete);
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
