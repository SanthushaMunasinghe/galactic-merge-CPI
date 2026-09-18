using Oxtail.Utils;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    public struct AsteroidDestroyedByBulletEvent
    {
        public Asteroid Asteroid;
    }

    /// <summary>
    /// A bullet's direction is fixed once, at spawn (Initialize), to the firing Shot Point's world-space
    /// up direction — there is no targeting or homing, so an angled shot point fires outward simply
    /// because its own up vector points outward. The bullet also rotates its own transform once at spawn
    /// so its sprite's nose (local +Y) faces that same direction, using the same
    /// Atan2(dir.y, dir.x) * Rad2Deg - 90f convention as Asteroid.FaceTravelDirection. From there it flies
    /// in a straight line at a constant Speed via Rigidbody.MovePosition every FixedUpdate — no
    /// prediction, no curve. After moving, it checks every entry in Asteroid.ActiveAsteroids (skipping any
    /// that has already reached the planet) for one within Hit Radius of its new position; the first one
    /// found is destroyed exactly as before (AsteroidDestroyedByBulletEvent, DestroyWithEffect, removed
    /// from ActiveAsteroids) and this bullet is destroyed with it — any bullet can hit any asteroid it
    /// happens to pass near, there is no longer a locked one-bullet-one-target relationship. A bullet that
    /// never gets that close to anything self-destructs after Max Lifetime seconds instead of flying
    /// forever off-screen. Requires a kinematic Rigidbody on this GameObject.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class BulletProjectile : MonoBehaviour
    {
        [Header("Hit")]
        [SerializeField, Min(0f)] private float m_HitRadius = 0.35f;

        [Header("Flight")]
        [Tooltip("A bullet that never gets within Hit Radius of anything self-destructs after this long, " +
            "instead of flying forever off-screen.")]
        [SerializeField, Min(0f)] private float m_MaxLifetime = 4f;

        private Rigidbody m_Rigidbody;
        private Vector3 m_Direction;
        private float m_Speed;
        private float m_Elapsed;
        private bool m_HasHit;

        private void Awake()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
        }

        public void Initialize(Vector3 direction, float speed)
        {
            m_Direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.up;
            m_Speed = speed;
            m_Elapsed = 0f;

            // Nose is local +Y, same convention as Asteroid.FaceTravelDirection.
            float angle = (Mathf.Atan2(m_Direction.y, m_Direction.x) * Mathf.Rad2Deg) - 90f;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void FixedUpdate()
        {
            // Destroy is deferred to the end of the frame, so without this guard a bullet that already
            // connected could step again and destroy a second comet.
            if (m_HasHit)
                return;

            m_Elapsed += Time.fixedDeltaTime;
            if (m_Elapsed >= m_MaxLifetime)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 newPosition = m_Rigidbody.position + (m_Direction * m_Speed * Time.fixedDeltaTime);
            m_Rigidbody.MovePosition(newPosition);

            CheckForHit(newPosition);
        }

        private void CheckForHit(Vector3 position)
        {
            for (int i = 0; i < Asteroid.ActiveAsteroids.Count; i++)
            {
                Asteroid asteroid = Asteroid.ActiveAsteroids[i];
                if (asteroid == null || asteroid.HasReachedCenter)
                    continue;

                if (Vector3.Distance(position, asteroid.transform.position) <= m_HitRadius)
                {
                    HitTarget(asteroid);
                    return;
                }
            }
        }

        private void HitTarget(Asteroid target)
        {
            m_HasHit = true;

            Asteroid.ActiveAsteroids.Remove(target);

            EventManager<AsteroidDestroyedByBulletEvent>.TriggerEvent(new AsteroidDestroyedByBulletEvent { Asteroid = target });

            target.DestroyWithEffect();

            Destroy(gameObject);
        }
    }
}
