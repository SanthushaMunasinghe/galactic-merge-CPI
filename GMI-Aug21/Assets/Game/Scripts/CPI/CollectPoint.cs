using Oxtail.Utils;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    public struct CollectPointCollectedEvent
    {
        public CollectPoint Point;
    }

    /// <summary>
    /// Spawned where a bullet destroys a comet; moves straight toward the planet every FixedUpdate via
    /// Rigidbody.MovePosition at a constant speed — no jump/slide phase. Fires CollectPointCollectedEvent
    /// and destroys itself only once it arrives within Planet Bounds Radius of the planet; being destroyed
    /// any other way never fires the event, so only an actual collection grants health.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class CollectPoint : MonoBehaviour
    {
        private Rigidbody m_Rigidbody;
        private float m_Speed;
        private Transform m_PlanetCenter;
        private float m_PlanetBoundsRadius;
        private bool m_IsCollected;

        private Vector3 PlanetPosition
        {
            get
            {
                if (m_PlanetCenter != null)
                    return m_PlanetCenter.position;

                return transform.parent != null ? transform.parent.position : Vector3.zero;
            }
        }

        private void Awake()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
        }

        public void Initialize(float speed, Transform planetCenter, float planetBoundsRadius)
        {
            m_Speed = speed;
            m_PlanetCenter = planetCenter;
            m_PlanetBoundsRadius = planetBoundsRadius;
        }

        private void FixedUpdate()
        {
            // Destroy is deferred to the end of the frame, so without this guard a collected point could
            // run one more step and raise the event twice.
            if (m_IsCollected)
                return;

            Vector3 planetPosition = PlanetPosition;
            Vector3 newPos = Vector3.MoveTowards(m_Rigidbody.position, planetPosition, m_Speed * Time.fixedDeltaTime);
            m_Rigidbody.MovePosition(newPos);

            if (Vector3.Distance(newPos, planetPosition) <= m_PlanetBoundsRadius)
                Collect();
        }

        private void Collect()
        {
            m_IsCollected = true;

            EventManager<CollectPointCollectedEvent>.TriggerEvent(new CollectPointCollectedEvent { Point = this });

            Destroy(gameObject);
        }
    }
}
