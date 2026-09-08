using Oxtail.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    /// <summary>
    /// Listens for SpaceshipHitRewardLineEvent and, if any untargeted asteroid is currently active,
    /// hasn't already reached the center, and is outside the Dead Zone Radius, spawns a bullet at the
    /// reward line's position that flies a curved path (bulging along this transform's local -Z axis)
    /// toward the closest one at a constant Speed and locks it (IsTargeted) so no other bullet aims at
    /// it too. The curve's height scales with spawn-to-target distance (Curve Height Per Distance),
    /// capped at Max Curve Height — there is no minimum, so a close enough target gets a straight line
    /// (curve height 0). If no valid target exists at the moment a reward line is hit (every active
    /// asteroid already targeted, already centered, or inside the dead zone), the shot is queued and
    /// retried every frame until a target frees up, rather than being dropped — a shot is only ever
    /// abandoned via ClearPendingShots (called by CPIManager once a wave has fully cleared).
    /// </summary>
    public class BulletSpawnManager : MonoBehaviour
    {
        [Header("Prefab")]
        [SerializeField] private BulletProjectile m_BulletPrefab;

        [Header("Flight Settings")]
        [SerializeField] private float m_Speed = 8f;
        [SerializeField, Min(0f)] private float m_DriftLifetime = 10f;
        [SerializeField, Min(0f)] private float m_MaxCurveHeight = 4f;
        [SerializeField, Min(0f)] private float m_CurveHeightPerDistance = 0.2f;

        [Header("Targeting")]
        [SerializeField, Min(0f)] private float m_DeadZoneRadius = 2f;

        private readonly List<Vector3> m_PendingShotOrigins = new List<Vector3>();

        private void OnEnable()
        {
            EventManager<SpaceshipHitRewardLineEvent>.AddListener(OnSpaceshipHitRewardLine);
        }

        private void OnDisable()
        {
            EventManager<SpaceshipHitRewardLineEvent>.RemoveListener(OnSpaceshipHitRewardLine);
        }

        private void Update()
        {
            // Retries every queued shot every frame so a reward-line hit that found no free target the
            // instant it happened still fires as soon as one opens up (another bullet resolving, or a
            // later asteroid in the same wave spawning in), instead of being lost for good.
            for (int i = m_PendingShotOrigins.Count - 1; i >= 0; i--)
            {
                if (TryFireBullet(m_PendingShotOrigins[i]))
                    m_PendingShotOrigins.RemoveAt(i);
            }
        }

        private void OnSpaceshipHitRewardLine(SpaceshipHitRewardLineEvent evt)
        {
            if (m_BulletPrefab == null)
            {
                Debug.LogError($"{nameof(BulletSpawnManager)}: Bullet Prefab must be assigned.", this);
                return;
            }

            Vector3 origin = evt.Line.transform.position;
            if (!TryFireBullet(origin))
                m_PendingShotOrigins.Add(origin);
        }

        private bool TryFireBullet(Vector3 fromPosition)
        {
            AsteroidProjectile target = FindClosestAsteroid(fromPosition);
            if (target == null)
                return false;

            target.IsTargeted = true;

            float distanceToTarget = Vector3.Distance(fromPosition, target.transform.position);
            float curveHeight = Mathf.Min(distanceToTarget * m_CurveHeightPerDistance, m_MaxCurveHeight);

            BulletProjectile bullet = Instantiate(m_BulletPrefab, fromPosition, Quaternion.identity, transform);
            bullet.Initialize(target, m_Speed, m_DriftLifetime, curveHeight);
            return true;
        }

        /// <summary>Abandons every shot still waiting for a target. Call once a wave has fully cleared
        /// (no more asteroids will ever appear to fulfill it), so a stray shot from the previous wave
        /// doesn't unexpectedly fire the moment the next wave's first asteroid spawns.</summary>
        public void ClearPendingShots()
        {
            m_PendingShotOrigins.Clear();
        }

        private AsteroidProjectile FindClosestAsteroid(Vector3 fromPosition)
        {
            float minDistance = float.MaxValue;
            AsteroidProjectile closest = null;

            for (int i = 0; i < AsteroidProjectile.ActiveAsteroids.Count; i++)
            {
                AsteroidProjectile asteroid = AsteroidProjectile.ActiveAsteroids[i];
                if (asteroid.HasReachedCenter || asteroid.IsTargeted)
                    continue;

                if (Vector3.Distance(transform.position, asteroid.transform.position) < m_DeadZoneRadius)
                    continue;

                float distance = Vector3.Distance(fromPosition, asteroid.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closest = asteroid;
                }
            }

            return closest;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.matrix = transform.localToWorldMatrix;
            DrawCircleGizmo(m_DeadZoneRadius);
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
