using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    /// <summary>
    /// Flies straight toward its target's current position at a constant speed via
    /// Rigidbody.MovePosition every FixedUpdate, so it still converges onto the target even if the
    /// target keeps moving. If the target is destroyed before being hit, the bullet keeps flying
    /// along its last heading instead of vanishing, self-destructing after Drift Lifetime seconds if
    /// it hits nothing else first. On a trigger hit matching the asteroid layer, destroys both itself
    /// and whatever asteroid it actually hit; if that wasn't its assigned target, the assigned
    /// target's IsTargeted flag is released so it can be targeted again. Requires a trigger Collider
    /// and a kinematic Rigidbody on this GameObject so Unity fires OnTriggerEnter for a script-moved
    /// object.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class BulletProjectile : MonoBehaviour
    {
        [SerializeField] private LayerMask m_AsteroidLayerMask;

        private Rigidbody m_Rigidbody;
        private AsteroidProjectile m_Target;
        private float m_Speed;
        private float m_DriftLifetime;
        private Vector3 m_LastTargetPosition;
        private bool m_IsDrifting;
        private Vector3 m_DriftDirection;
        private float m_DriftElapsed;

        private void Awake()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
        }

        public void Initialize(AsteroidProjectile target, float speed, float driftLifetime)
        {
            m_Target = target;
            m_Speed = speed;
            m_DriftLifetime = driftLifetime;
            m_LastTargetPosition = target.transform.position;
        }

        private void FixedUpdate()
        {
            if (m_Target == null)
            {
                Drift();
                return;
            }

            m_LastTargetPosition = m_Target.transform.position;
            Vector3 newPos = Vector3.MoveTowards(m_Rigidbody.position, m_Target.transform.position, m_Speed * Time.fixedDeltaTime);
            m_Rigidbody.MovePosition(newPos);
        }

        private void Drift()
        {
            if (!m_IsDrifting)
            {
                m_IsDrifting = true;

                Vector3 toLastKnown = m_LastTargetPosition - m_Rigidbody.position;
                m_DriftDirection = toLastKnown.sqrMagnitude > 0.0001f ? toLastKnown.normalized : transform.forward;
            }

            m_DriftElapsed += Time.fixedDeltaTime;
            if (m_DriftElapsed >= m_DriftLifetime)
            {
                Destroy(gameObject);
                return;
            }

            m_Rigidbody.MovePosition(m_Rigidbody.position + (m_DriftDirection * m_Speed * Time.fixedDeltaTime));
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
                Destroy(hitAsteroid.gameObject);
            }

            Destroy(gameObject);
        }
    }
}
