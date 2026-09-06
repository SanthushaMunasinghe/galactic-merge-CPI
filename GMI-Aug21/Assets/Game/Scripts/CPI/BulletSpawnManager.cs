using Oxtail.Utils;
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
    /// (curve height 0). If no valid target exists, nothing is spawned.
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

        private void OnEnable()
        {
            EventManager<SpaceshipHitRewardLineEvent>.AddListener(OnSpaceshipHitRewardLine);
        }

        private void OnDisable()
        {
            EventManager<SpaceshipHitRewardLineEvent>.RemoveListener(OnSpaceshipHitRewardLine);
        }

        private void OnSpaceshipHitRewardLine(SpaceshipHitRewardLineEvent evt)
        {
            if (m_BulletPrefab == null)
            {
                Debug.LogError($"{nameof(BulletSpawnManager)}: Bullet Prefab must be assigned.", this);
                return;
            }

            AsteroidProjectile target = FindClosestAsteroid(evt.Line.transform.position);
            if (target == null)
            {
                Debug.Log($"{nameof(BulletSpawnManager)}: no valid asteroid target, skipping bullet spawn.", this);
                return;
            }

            target.IsTargeted = true;

            float distanceToTarget = Vector3.Distance(evt.Line.transform.position, target.transform.position);
            float curveHeight = Mathf.Min(distanceToTarget * m_CurveHeightPerDistance, m_MaxCurveHeight);

            BulletProjectile bullet = Instantiate(m_BulletPrefab, evt.Line.transform.position, Quaternion.identity, transform);
            bullet.Initialize(target, m_Speed, m_DriftLifetime, curveHeight);
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
