using System;
using DG.Tweening;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    /// <summary>
    /// Slides from its spawn point to a jump-radius point in a straight line (same local X/Y plane)
    /// using DOTween at jumpSpeed, then homes toward the shared local center (local origin of its
    /// parent) every frame at centeringSpeed. Destroyable throughout by a trigger hit on the planet
    /// layer. Requires a trigger Collider and a kinematic Rigidbody on this GameObject so Unity fires
    /// OnTriggerEnter for a script-moved object. Destruction VFX will be hooked into HandlePlanetHit
    /// later.
    /// </summary>
    public class AsteroidProjectile : MonoBehaviour
    {
        private enum AsteroidState
        {
            Sliding,
            Homing
        }

        [SerializeField] private LayerMask m_PlanetLayerMask;

        private AsteroidState m_State = AsteroidState.Sliding;
        private float m_CenteringSpeed;
        private Action m_OnJumpComplete;
        private bool m_NotifiedJumpComplete;

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

        private void Update()
        {
            if (m_State != AsteroidState.Homing)
                return;

            transform.localPosition = Vector3.MoveTowards(transform.localPosition, Vector3.zero, m_CenteringSpeed * Time.deltaTime);
        }

        private void OnTriggerEnter(Collider other)
        {
            if ((m_PlanetLayerMask.value & (1 << other.gameObject.layer)) == 0)
                return;

            HandlePlanetHit();
        }

        private void HandlePlanetHit()
        {
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
