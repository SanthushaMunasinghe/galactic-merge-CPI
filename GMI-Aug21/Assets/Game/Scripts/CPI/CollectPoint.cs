using Oxtail.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    public struct CollectPointCollectedEvent
    {
        public CollectPoint Point;

        /// <summary>The planet that absorbed this point.</summary>
        public PlanetEffect Planet;
    }

    /// <summary>
    /// Spawned where a bullet destroys a comet — one per kill, flying at whichever planet was closest to
    /// that kill. It moves straight toward that planet every FixedUpdate via Rigidbody.MovePosition at a
    /// constant speed, with no jump/slide phase. Arrival is detected by the planet itself (PlanetEffect),
    /// which calls CollectInto; being destroyed any other way never fires CollectPointCollectedEvent, so
    /// only an actual collection can grant health. Health is a single shared pool, so which planet takes
    /// the point in only decides where it flies, never how much is restored.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class CollectPoint : MonoBehaviour
    {
        /// <summary>Points currently in flight, so every planet can test them against its own bounds.</summary>
        public static readonly List<CollectPoint> ActivePoints = new List<CollectPoint>();

        private Rigidbody m_Rigidbody;
        private float m_Speed;
        private PlanetEffect m_TargetPlanet;
        private bool m_IsCollected;

        private Vector3 PlanetPosition
        {
            get
            {
                if (m_TargetPlanet != null)
                    return m_TargetPlanet.Center;

                return transform.parent != null ? transform.parent.position : Vector3.zero;
            }
        }

        private void Awake()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
        }

        public void Initialize(float speed, PlanetEffect targetPlanet)
        {
            m_Speed = speed;
            m_TargetPlanet = targetPlanet;

            ActivePoints.Add(this);
        }

        private void FixedUpdate()
        {
            // Destroy is deferred to the end of the frame, so without this guard a collected point could
            // run one more step after being absorbed.
            if (m_IsCollected)
                return;

            Vector3 newPos = Vector3.MoveTowards(m_Rigidbody.position, PlanetPosition, m_Speed * Time.fixedDeltaTime);
            m_Rigidbody.MovePosition(newPos);
        }

        private void OnDisable()
        {
            ActivePoints.Remove(this);
        }

        private void OnDestroy()
        {
            ActivePoints.Remove(this);
        }

        /// <summary>
        /// Called by the planet this point has arrived inside — not necessarily the one it was flying at,
        /// since every planet absorbs whatever enters its own bounds. Safe to call more than once.
        /// </summary>
        public void CollectInto(PlanetEffect planet)
        {
            if (m_IsCollected)
                return;

            m_IsCollected = true;

            // Removed immediately rather than waiting for the deferred OnDestroy, so no other planet can
            // absorb this same point later in the frame.
            ActivePoints.Remove(this);

            EventManager<CollectPointCollectedEvent>.TriggerEvent(new CollectPointCollectedEvent
            {
                Point = this,
                Planet = planet
            });

            Destroy(gameObject);
        }
    }
}
