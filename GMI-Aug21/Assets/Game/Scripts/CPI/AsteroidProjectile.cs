using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    /// <summary>
    /// Slides from its spawn point to a jump-radius point in a straight line (same local X/Y plane)
    /// using DOTween at jumpSpeed, then homes toward the shared local center (local origin of its
    /// parent) every FixedUpdate via Rigidbody.MovePosition at centeringSpeed. Destroyable throughout
    /// by a trigger hit on the planet layer. Requires a trigger Collider and a kinematic Rigidbody on
    /// this GameObject so Unity fires OnTriggerEnter for a script-moved object. Destruction VFX will
    /// be hooked into HandlePlanetHit later.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class AsteroidProjectile : MonoBehaviour
    {
        private enum AsteroidState
        {
            Sliding,
            Homing
        }

        public static readonly List<AsteroidProjectile> ActiveAsteroids = new List<AsteroidProjectile>();

        [SerializeField] private LayerMask m_PlanetLayerMask;

        private Rigidbody m_Rigidbody;
        private AsteroidState m_State = AsteroidState.Sliding;
        private float m_CenteringSpeed;
        private Action m_OnJumpComplete;
        private bool m_NotifiedJumpComplete;

        /// <summary>True once this asteroid has arrived at the shared local center.</summary>
        public bool HasReachedCenter { get; private set; }

        /// <summary>True while a bullet is currently assigned to hit this asteroid.</summary>
        public bool IsTargeted { get; set; }

        private void Awake()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
        }

        public void Initialize(Vector3 localJumpPosition, float jumpSpeed, float centeringSpeed, Action onJumpComplete = null)
        {
            m_CenteringSpeed = centeringSpeed;
            m_OnJumpComplete = onJumpComplete;
            m_State = AsteroidState.Sliding;

            float distance = Vector3.Distance(transform.localPosition, localJumpPosition);
            float duration = jumpSpeed > 0f ? distance / jumpSpeed : 0f;

            transform.DOLocalMove(localJumpPosition, duration)
                .SetEase(Ease.OutQuad)
                .OnComplete(BeginHoming);
        }

        private void BeginHoming()
        {
            m_State = AsteroidState.Homing;
            NotifyJumpComplete();
        }

        private void OnEnable()
        {
            ActiveAsteroids.Add(this);
        }

        private void OnDisable()
        {
            ActiveAsteroids.Remove(this);
        }

        private void FixedUpdate()
        {
            if (m_State != AsteroidState.Homing)
                return;

            Vector3 centerWorldPos = transform.parent != null ? transform.parent.position : Vector3.zero;
            Vector3 newPos = Vector3.MoveTowards(m_Rigidbody.position, centerWorldPos, m_CenteringSpeed * Time.fixedDeltaTime);
            m_Rigidbody.MovePosition(newPos);

            if (newPos == centerWorldPos)
                HasReachedCenter = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if ((m_PlanetLayerMask.value & (1 << other.gameObject.layer)) == 0)
                return;

            HandlePlanetHit();
        }

        private void HandlePlanetHit()
        {
            // Removed immediately (rather than waiting for the deferred OnDisable) so a bullet
            // resolving its target later this same frame never targets an asteroid already destroyed.
            ActiveAsteroids.Remove(this);

            // TODO: hook in destruction VFX here.
            Destroy(gameObject);
        }

        private void NotifyJumpComplete()
        {
            if (m_NotifiedJumpComplete)
                return;

            m_NotifiedJumpComplete = true;
            m_OnJumpComplete?.Invoke();
        }

        private void OnDestroy()
        {
            // Guarantees the spawner's wave gate is never stuck waiting on an asteroid that was
            // destroyed mid-slide before it ever reached BeginHoming.
            NotifyJumpComplete();
        }
    }
}
