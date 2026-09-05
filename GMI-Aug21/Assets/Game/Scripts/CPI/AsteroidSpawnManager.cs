using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    /// <summary>
    /// Spawns waves of AsteroidProjectile instances around a circle in this transform's local X/Y
    /// plane (Spawn Radius), each sliding inward at the same angle to a point on a smaller circle
    /// (Jump Radius), offset along local Z by Jump Height Offset, before homing the rest of the way
    /// toward the shared local center. Asteroids within a wave always spawn one at a time. A wave is
    /// only ever started externally via TriggerWave (or the "Trigger Wave" context menu item while
    /// testing); a call while a wave is already in progress (including asteroids still mid-jump) is
    /// ignored.
    /// </summary>
    public class AsteroidSpawnManager : MonoBehaviour
    {
        [Header("Prefab")]
        [SerializeField] private AsteroidProjectile m_AsteroidPrefab;

        [Header("Spawn Area")]
        [SerializeField, Min(0f)] private float m_SpawnRadius = 10f;
        [SerializeField, Min(0f)] private float m_JumpRadius = 4f;
        [SerializeField] private float m_JumpHeightOffset = -0.5f;

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

            for (int i = 0; i < count; i++)
            {
                SpawnAsteroid();

                float interval = Random.Range(m_SpawnIntervalRange.x, m_SpawnIntervalRange.y);
                yield return new WaitForSeconds(interval);
            }

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

            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector3 spawnLocalPos = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * m_SpawnRadius;
            Vector3 jumpLocalPos = new Vector3(Mathf.Cos(angle) * m_JumpRadius, Mathf.Sin(angle) * m_JumpRadius, m_JumpHeightOffset);

            AsteroidProjectile asteroid = Instantiate(m_AsteroidPrefab, transform.TransformPoint(spawnLocalPos), Quaternion.identity, transform);
            m_AsteroidsPendingJump++;

            float jumpSpeed = Random.Range(m_JumpSpeedRange.x, m_JumpSpeedRange.y);
            float centeringSpeed = Random.Range(m_CenteringSpeedRange.x, m_CenteringSpeedRange.y);
            asteroid.Initialize(jumpLocalPos, jumpSpeed, centeringSpeed, OnAsteroidJumpComplete);
        }

        private void OnAsteroidJumpComplete()
        {
            m_AsteroidsPendingJump--;
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
            Gizmos.matrix = transform.localToWorldMatrix;

            Gizmos.color = Color.cyan;
            DrawCircleGizmo(m_SpawnRadius);

            Gizmos.color = Color.yellow;
            DrawCircleGizmo(m_JumpRadius);
        }

        private static void DrawCircleGizmo(float radius)
        {
            const int segments = 48;

            Vector3 previousPoint = new Vector3(radius, 0f, 0f);
            for (int i = 1; i <= segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2f;
                Vector3 point = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
                Gizmos.DrawLine(previousPoint, point);
                previousPoint = point;
            }
        }
    }
}
