using Oxtail.Utils;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    public struct AsteroidDestroyedByBulletEvent
    {
        public Asteroid Asteroid;
    }

    /// <summary>
    /// At spawn, predicts where the target will be by the time the bullet arrives (from the target's
    /// current heading/speed) and builds a single fixed, symmetrical quadratic-Bezier curve from the
    /// spawn point to that predicted point, bulging along the spawner's local -Z axis by Curve Height.
    /// It then flies that fixed curve at a constant Speed via Rigidbody.MovePosition every FixedUpdate
    /// — it never re-aims at the live target, so the curve stays smooth and symmetric even though the
    /// target keeps moving. The hit is applied on arrival rather than through a collider (the comet
    /// prefab has none, and production's CombatTrailDamageVFX resolves its damage the same way): either
    /// the moment the bullet comes within Hit Radius of its target, or when it reaches the end of the
    /// curve. Only its own assigned target is ever destroyed. If that target is already gone by then, the
    /// bullet continues straight along the curve's exit direction and self-destructs after Drift Lifetime
    /// seconds. Requires a kinematic Rigidbody on this GameObject.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class BulletProjectile : MonoBehaviour
    {
        [Header("Hit")]
        [SerializeField, Min(0f)] private float m_HitRadius = 0.35f;

        private Rigidbody m_Rigidbody;
        private Asteroid m_Target;
        private float m_Speed;
        private float m_DriftLifetime;

        private Vector3 m_CurveStart;
        private Vector3 m_CurveControl;
        private Vector3 m_CurveEnd;
        private float m_CurveLength;
        private float m_DistanceTraveled;

        private bool m_HasHit;
        private bool m_IsDrifting;
        private Vector3 m_DriftDirection;
        private float m_DriftElapsed;

        private void Awake()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
        }

        public void Initialize(Asteroid target, float speed, float driftLifetime, float curveHeight)
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
            // Destroy is deferred to the end of the frame, so without this guard a bullet that already
            // connected could step again and destroy a second comet.
            if (m_HasHit)
                return;

            if (m_IsDrifting)
            {
                Drift();
                return;
            }

            m_DistanceTraveled += m_Speed * Time.fixedDeltaTime;
            float t = m_CurveLength > 0f ? Mathf.Clamp01(m_DistanceTraveled / m_CurveLength) : 1f;

            Vector3 curvePos = QuadraticBezier(m_CurveStart, m_CurveControl, m_CurveEnd, t);
            m_Rigidbody.MovePosition(curvePos);

            if (CanHitTarget() && Vector3.Distance(curvePos, m_Target.transform.position) <= m_HitRadius)
            {
                HitTarget();
                return;
            }

            if (t < 1f)
                return;

            // Reached the end of the curve: the hit still lands, even if the comet has drifted a little past
            // the predicted meeting point, so prediction error never turns into a phantom miss.
            if (CanHitTarget())
                HitTarget();
            else
                BeginDrift();
        }

        private bool CanHitTarget()
        {
            return m_Target != null && !m_Target.IsDead;
        }

        private void HitTarget()
        {
            m_HasHit = true;

            m_Target.IsTargeted = false;
            Asteroid.ActiveAsteroids.Remove(m_Target);

            EventManager<AsteroidDestroyedByBulletEvent>.TriggerEvent(new AsteroidDestroyedByBulletEvent { Asteroid = m_Target });

            m_Target.DestroyWithEffect();

            Destroy(gameObject);
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
                // A genuine miss: this bullet is giving up without ever hitting its target, so the
                // lock must be released here too, not just on a hit — otherwise the target stays
                // IsTargeted forever and no future bullet can ever be aimed at it again.
                if (m_Target != null)
                    m_Target.IsTargeted = false;

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
    }
}
