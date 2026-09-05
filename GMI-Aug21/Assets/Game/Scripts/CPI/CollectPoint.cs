using Oxtail.Utils;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    public struct CollectPointCollectedEvent
    {
        public CollectPoint Point;
    }

    /// <summary>
    /// Spawned where a bullet destroys an asteroid; moves straight toward the shared local center
    /// (local origin of its parent) every FixedUpdate via Rigidbody.MovePosition at a constant speed
    /// — no jump/slide phase. Fires CollectPointCollectedEvent and destroys itself only on a trigger
    /// hit matching the planet layer; being destroyed any other way never fires the event, so only an
    /// actual collection grants health. Requires a trigger Collider and a kinematic Rigidbody on this
    /// GameObject so Unity fires OnTriggerEnter for a script-moved object.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class CollectPoint : MonoBehaviour
    {
        [SerializeField] private LayerMask m_PlanetLayerMask;

        private Rigidbody m_Rigidbody;
        private float m_Speed;

        private void Awake()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
        }

        public void Initialize(float speed)
        {
            m_Speed = speed;
        }

        private void FixedUpdate()
        {
            Vector3 centerWorldPos = transform.parent != null ? transform.parent.position : Vector3.zero;
            Vector3 newPos = Vector3.MoveTowards(m_Rigidbody.position, centerWorldPos, m_Speed * Time.fixedDeltaTime);
            m_Rigidbody.MovePosition(newPos);
        }

        private void OnTriggerEnter(Collider other)
        {
            if ((m_PlanetLayerMask.value & (1 << other.gameObject.layer)) == 0)
                return;

            EventManager<CollectPointCollectedEvent>.TriggerEvent(new CollectPointCollectedEvent { Point = this });
            Destroy(gameObject);
        }
    }
}
