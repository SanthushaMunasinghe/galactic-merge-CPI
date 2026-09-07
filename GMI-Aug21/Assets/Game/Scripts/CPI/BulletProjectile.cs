using Oxtail.Utils;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    public struct AsteroidDestroyedByBulletEvent
    {
        public AsteroidProjectile Asteroid;
    }

    /// <summary>
    /// At spawn, predicts where the target will be by the time the bullet arrives (from the target's
    /// current heading/speed) and builds a single fixed, symmetrical quadratic-Bezier curve from the
    /// spawn point to that predicted point, bulging along the spawner's local -Z axis by Curve Height.
    /// It then flies that fixed curve at a constant Speed via Rigidbody.MovePosition every FixedUpdate
    /// — it never re-aims at the live target, so the curve stays smooth and symmetric even though the
    /// target keeps moving. If it reaches the end of the curve without hitting anything (a miss, or
    /// the target was destroyed first), it continues straight along the curve's exit direction and
    /// self-destructs after Drift Lifetime seconds. On a trigger hit matching the asteroid layer,
    /// destroys both itself and whatever asteroid it actually hit; if that wasn't its assigned target,
    /// the assigned target's IsTargeted flag is released so it can be targeted again. Requires a
    /// trigger Collider and a kinematic Rigidbody on this GameObject so Unity fires OnTriggerEnter for
    /// a script-moved object.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class BulletProjectile : MonoBehaviour
    {
        [SerializeField] private LayerMask m_AsteroidLayerMask;

        private Rigidbody m_Rigidbody;
        private AsteroidProjectile m_Target;
        private float m_Speed;
        private float m_DriftLifetime;

        private Vector3 m_CurveStart;
        private Vector3 m_CurveControl;
        private Vector3 m_CurveEnd;
        private float m_CurveLength;
        private float m_DistanceTraveled;

        private bool m_IsDrifting;
        private Vector3 m_DriftDirection;
        private float m_DriftElapsed;

        private void Awake()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
        }

        public void Initialize(AsteroidProjectile target, float speed, float driftLifetime, float curveHeight)
        {
            m_Target = target;
            m_Speed = speed;
            m_DriftLifetime = driftLifetime;

            Vector3 startPos = transform.position;
            Vector3 predictedPoint = PredictMeetingPoint(startPos, target.transform.position, target.PredictedVelocity, speed);
            Vector3 curveAxisWorld = transform.parent != null ? -transform.parent.forward : Vector3.back;

            m_CurveStart = startPos;
            m_CurveEnd = predictedPoint;
            m_CurveControl = Vector3.Lerp(startPos, predictedPoint, 0.5f) + (curveAxisWorld * curveHeight);
            m_CurveLength = Vector3.Distance(startPos, predictedPoint);
            m_DistanceTraveled = 0f;
        }

        /// <summary>Iteratively refines where a constant-velocity target will be when a constant-speed shot fired now would reach it.</summary>
        private static Vector3 PredictMeetingPoint(Vector3 shooterPos, Vector3 targetPos, Vector3 targetVelocity, float bulletSpeed)
        {
            Vector3 predicted = targetPos;

            for (int i = 0; i < 3; i++)
            {
                float timeToReach = bulletSpeed > 0f ? Vector3.Distance(shooterPos, predicted) / bulletSpeed : 0f;
                predicted = targetPos + (targetVelocity * timeToReach);
            }

            return predicted;
        }

        private void FixedUpdate()
        {
            if (m_IsDrifting)
            {
                Drift();
                return;
            }

            m_DistanceTraveled += m_Speed * Time.fixedDeltaTime;
            float t = m_CurveLength > 0f ? Mathf.Clamp01(m_DistanceTraveled / m_CurveLength) : 1f;

            Vector3 curvePos = QuadraticBezier(m_CurveStart, m_CurveControl, m_CurveEnd, t);
            m_Rigidbody.MovePosition(curvePos);

            if (t >= 1f)
                BeginDrift();
        }

        private void BeginDrift()
        {
            m_IsDrifting = true;
            m_DriftElapsed = 0f;

            Vector3 tangent = m_CurveEnd - m_CurveControl;
            m_DriftDirection = tangent.sqrMagnitude > 0.0001f ? tangent.normalized : transform.forward;
        }

        private void Drift()
        {
            m_DriftElapsed += Time.fixedDeltaTime;
            if (m_DriftElapsed >= m_DriftLifetime)
            {
                Destroy(gameObject);
                return;
            }

            m_Rigidbody.MovePosition(m_Rigidbody.position + (m_DriftDirection * m_Speed * Time.fixedDeltaTime));
        }

        private static Vector3 QuadraticBezier(Vector3 a, Vector3 b, Vector3 c, float t)
        {
            float u = 1f - t;
            return (u * u * a) + (2f * u * t * b) + (t * t * c);
        }

        private void OnTriggerEnter(Collider other)
        {
            if ((m_AsteroidLayerMask.value & (1 << other.gameObject.layer)) == 0)
                return;

            AsteroidProjectile hitAsteroid = other.GetComponent<AsteroidProjectile>();
            if (hitAsteroid != null)
            {
                if (m_Target != null && hitAsteroid != m_Target)
                    m_Target.IsTargeted = false;

                hitAsteroid.IsTargeted = false;
                AsteroidProjectile.ActiveAsteroids.Remove(hitAsteroid);

                EventManager<AsteroidDestroyedByBulletEvent>.TriggerEvent(new AsteroidDestroyedByBulletEvent { Asteroid = hitAsteroid });

                hitAsteroid.DestroyWithEffect();
            }

            Destroy(gameObject);
        }
    }
}
