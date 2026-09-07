using System;
using System.Collections.Generic;
using DG.Tweening;
using Oxtail.Utils;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    public struct AsteroidDestroyedByPlanetEvent
    {
        public AsteroidProjectile Asteroid;
    }

    /// <summary>
    /// Slides from its spawn point to a jump-radius point in a straight line (same local X/Y plane)
    /// using DOTween at jumpSpeed, then homes toward the shared local center (local origin of its
    /// parent) every FixedUpdate via Rigidbody.MovePosition at centeringSpeed. Destroyable throughout
    /// by a trigger hit on the planet layer. Requires a trigger Collider and a kinematic Rigidbody on
    /// this GameObject so Unity fires OnTriggerEnter for a script-moved object. On destruction (via
    /// DestroyWithEffect), it immediately stops moving/colliding, hides Active Visual, and scatters
    /// Debris Parent's children outward while shrinking them before the GameObject is destroyed.
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

        [Header("Destruction Debris")]
        [SerializeField] private GameObject m_ActiveVisual;
        [SerializeField] private Transform m_DebrisParent;
        [SerializeField, Min(0f)] private float m_DebrisMaxDistance = 2f;
        [SerializeField, Min(0f)] private float m_DebrisDuration = 1f;

        private Rigidbody m_Rigidbody;
        private Collider m_Collider;
        private AsteroidState m_State = AsteroidState.Sliding;
        private float m_CenteringSpeed;
        private Action m_OnJumpComplete;
        private bool m_NotifiedJumpComplete;
        private bool m_IsDestroyed;

        /// <summary>True once this asteroid has arrived at the shared local center.</summary>
        public bool HasReachedCenter { get; private set; }

        /// <summary>True while a bullet is currently assigned to hit this asteroid.</summary>
        public bool IsTargeted { get; set; }

        /// <summary>
        /// Approximate current heading and speed (toward the shared local center, at centering
        /// speed), used by bullets to predict where this asteroid will be when they arrive.
        /// </summary>
        public Vector3 PredictedVelocity
        {
            get
            {
                Vector3 centerWorldPos = transform.parent != null ? transform.parent.position : Vector3.zero;
                Vector3 toCenter = centerWorldPos - transform.position;
                return toCenter.sqrMagnitude > 0.0001f ? toCenter.normalized * m_CenteringSpeed : Vector3.zero;
            }
        }

        private void Awake()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
            m_Collider = GetComponent<Collider>();
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
            if (m_IsDestroyed)
                return;

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
            if (m_IsDestroyed)
                return;

            if ((m_PlanetLayerMask.value & (1 << other.gameObject.layer)) == 0)
                return;

            HandlePlanetHit();
        }

        private void HandlePlanetHit()
        {
            // Removed immediately (rather than waiting for the deferred OnDisable) so a bullet
            // resolving its target later this same frame never targets an asteroid already destroyed.
            ActiveAsteroids.Remove(this);

            EventManager<AsteroidDestroyedByPlanetEvent>.TriggerEvent(new AsteroidDestroyedByPlanetEvent { Asteroid = this });

            DestroyWithEffect();
        }

        /// <summary>
        /// Immediately makes this asteroid stop moving and colliding (logically destroyed), then plays
        /// the debris scatter/shrink effect before destroying the GameObject after Debris Duration.
        /// Safe to call more than once.
        /// </summary>
        public void DestroyWithEffect()
        {
            if (m_IsDestroyed)
                return;

            m_IsDestroyed = true;

            transform.DOKill();

            if (m_Collider != null)
                m_Collider.enabled = false;

            // Not deferred to OnDestroy: the spawner's wave gate must not wait through the debris
            // animation of an asteroid that died mid-slide.
            NotifyJumpComplete();

            if (m_ActiveVisual != null)
                m_ActiveVisual.SetActive(false);

            PlayDebrisEffect();

            Destroy(gameObject, m_DebrisDuration);
        }

        private void PlayDebrisEffect()
        {
            if (m_DebrisParent == null)
                return;

            m_DebrisParent.gameObject.SetActive(true);

            foreach (Transform debris in m_DebrisParent)
            {
                Vector3 direction = debris.localPosition.sqrMagnitude > 0.0001f
                    ? debris.localPosition.normalized
                    : UnityEngine.Random.onUnitSphere;

                debris.DOLocalMove(direction * m_DebrisMaxDistance, m_DebrisDuration).SetEase(Ease.OutQuad);
                debris.DOScale(0f, m_DebrisDuration).SetEase(Ease.InQuad);
            }
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
