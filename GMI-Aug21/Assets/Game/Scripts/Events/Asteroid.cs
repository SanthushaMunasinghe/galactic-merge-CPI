using DG.Tweening;
using Oxtail.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    public struct AsteroidDestroyedByPlanetEvent
    {
        public Asteroid Asteroid;
    }

    /// <summary>
    /// The 2D comet. By default it is a hand placed level target: it drops in from above on Start, then sits
    /// still taking damage through CombatTarget until SetDead scatters its parts.
    ///
    /// AsteroidSpawnManager can instead drive it as a projectile through InitializeAsProjectile: the drop in
    /// is skipped, the comet slides to a jump point and then homes toward the planet, and arriving inside
    /// Planet Bounds Radius raises AsteroidDestroyedByPlanetEvent and shatters it. Projectile mode never
    /// touches CombatTarget's health or SaveLoadManager, so a spawned comet can never overwrite the saved
    /// health of the level asteroid that shares its Target Index.
    /// </summary>
    public class Asteroid : CombatTarget
    {
        private enum ProjectileState
        {
            Sliding,
            Homing
        }

        /// <summary>Comets currently in flight. Only projectile mode registers here, so hand placed level
        /// asteroids never appear in it.</summary>
        public static readonly List<Asteroid> ActiveAsteroids = new List<Asteroid>();

        /// <summary>Longest a scattered part animates for, after which the GameObject can go.</summary>
        private const float k_ShatterLifetime = 1.2f;

        [Header("Comet")]
        [SerializeField] private SpriteRenderer m_Fire;
        [SerializeField] private SpriteRenderer m_Comet;
        [SerializeField] private GameObject m_CometParts_Parent;

        [Header("Parts")]
        [SerializeField] private SpriteRenderer[] m_CometParts;

        [Header("Projectile Mode")]
        [Tooltip("Turns the comet so it flies nose first, which keeps the fire trailing behind it. Only " +
            "used when a spawner drives this comet as a projectile.")]
        [SerializeField] private bool m_AlignToTravelDirection = true;
        [SerializeField] private float m_TravelRotationOffsetDegrees;

        private bool m_IsProjectile;
        private ProjectileState m_State;
        private float m_CenteringSpeed;
        private Transform m_PlanetCenter;
        private float m_PlanetBoundsRadius;
        private System.Action m_OnJumpComplete;
        private bool m_NotifiedJumpComplete;
        private bool m_IsShattered;

        /// <summary>True once this comet has arrived inside the planet bounds.</summary>
        public bool HasReachedCenter { get; private set; }

        /// <summary>True while a bullet is already assigned to hit this comet.</summary>
        public bool IsTargeted { get; set; }

        /// <summary>
        /// Approximate current heading and speed (toward the planet, at centering speed), used by bullets to
        /// predict where this comet will be by the time they arrive.
        /// </summary>
        public Vector3 PredictedVelocity
        {
            get
            {
                Vector3 toPlanet = PlanetPosition - transform.position;
                return toPlanet.sqrMagnitude > 0.0001f ? toPlanet.normalized * m_CenteringSpeed : Vector3.zero;
            }
        }

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
            m_Comet.material = new Material(m_Comet.material);
        }

        private void Start()
        {
            if (m_IsProjectile)
            {
                // The spawner already placed this comet and owns its movement from here, so the level drop
                // in must not run: its +5 Y offset would teleport the comet off its spawn point and the
                // tween back down would fight the slide.
                StartCometShake();
                return;
            }

            Vector3 initialPos = transform.localPosition;
            transform.localPosition += new Vector3(0f, 5f, 0f);
            transform.DOLocalMoveY(initialPos.y, Random.Range(0.5f, 1f))
                .SetEase(Ease.InExpo)
                .SetDelay(0.1f)
                .OnComplete(StartCometShake);
        }

        private void StartCometShake()
        {
            m_Comet.transform.DOShakePosition(0.35f, 0.1f).SetLoops(-1);
        }

        private void Update()
        {
            if (!m_IsProjectile || m_IsShattered)
                return;

            if (m_State == ProjectileState.Homing)
                transform.position = Vector3.MoveTowards(transform.position, PlanetPosition, m_CenteringSpeed * Time.deltaTime);

            FaceTravelDirection();

            if (Vector3.Distance(transform.position, PlanetPosition) <= m_PlanetBoundsRadius)
                HandlePlanetHit();
        }

        private void OnDisable()
        {
            if (m_IsProjectile)
                ActiveAsteroids.Remove(this);

            m_Comet.transform.DOKill();
        }

        private void OnDestroy()
        {
            if (!m_IsProjectile)
                return;

            ActiveAsteroids.Remove(this);

            // Guarantees the spawner's wave gate is never left waiting on a comet that was destroyed before
            // it ever finished its slide.
            NotifyJumpComplete();
        }

        #region Projectile mode

        /// <summary>
        /// Hands this comet over to a spawner: it slides from its current position to localJumpPosition at
        /// jumpSpeed, then homes toward planetCenter at centeringSpeed until it arrives within
        /// planetBoundsRadius of it. onJumpComplete fires once the slide finishes, or early if the comet is
        /// destroyed before that.
        /// </summary>
        public void InitializeAsProjectile(Vector3 localJumpPosition, float jumpSpeed, float centeringSpeed,
            Transform planetCenter, float planetBoundsRadius, System.Action onJumpComplete = null)
        {
            m_IsProjectile = true;
            m_State = ProjectileState.Sliding;
            m_CenteringSpeed = centeringSpeed;
            m_PlanetCenter = planetCenter;
            m_PlanetBoundsRadius = planetBoundsRadius;
            m_OnJumpComplete = onJumpComplete;

            ActiveAsteroids.Add(this);

            FaceTravelDirection();

            float distance = Vector3.Distance(transform.localPosition, localJumpPosition);
            float duration = jumpSpeed > 0f ? distance / jumpSpeed : 0f;

            transform.DOLocalMove(localJumpPosition, duration)
                .SetEase(Ease.OutQuad)
                .OnComplete(BeginHoming);
        }

        private void BeginHoming()
        {
            m_State = ProjectileState.Homing;
            NotifyJumpComplete();
        }

        /// <summary>Points the comet's nose at the planet. The nose is local +Y because the Fire child sits
        /// just below the comet sprite, so it ends up trailing.</summary>
        private void FaceTravelDirection()
        {
            if (!m_AlignToTravelDirection)
                return;

            Vector3 toPlanet = PlanetPosition - transform.position;
            if (toPlanet.sqrMagnitude < 0.0001f)
                return;

            float angle = (Mathf.Atan2(toPlanet.y, toPlanet.x) * Mathf.Rad2Deg) - 90f + m_TravelRotationOffsetDegrees;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void HandlePlanetHit()
        {
            HasReachedCenter = true;

            // Removed immediately (rather than waiting for the deferred OnDisable) so a bullet resolving its
            // target later this same frame never aims at a comet that has already arrived.
            ActiveAsteroids.Remove(this);

            EventManager<AsteroidDestroyedByPlanetEvent>.TriggerEvent(new AsteroidDestroyedByPlanetEvent { Asteroid = this });

            DestroyWithEffect();
        }

        /// <summary>
        /// Immediately stops this comet moving and being targetable, plays the same part scatter the level
        /// death uses, then destroys the GameObject once the scatter has played. Unlike SetDead it never
        /// writes to the save. Safe to call more than once.
        /// </summary>
        public void DestroyWithEffect()
        {
            if (m_IsShattered)
                return;

            m_IsShattered = true;
            IsDead = true;

            transform.DOKill();
            ActiveAsteroids.Remove(this);

            // Not deferred to OnDestroy: the spawner's wave gate must not wait through the scatter animation
            // of a comet that died mid slide.
            NotifyJumpComplete();

            ShatterParts();

            Destroy(gameObject, k_ShatterLifetime);
        }

        private void NotifyJumpComplete()
        {
            if (m_NotifiedJumpComplete)
                return;

            m_NotifiedJumpComplete = true;
            m_OnJumpComplete?.Invoke();
        }

        #endregion

        public override BigNumber TakeDamage(BigNumber damage)
        {
            m_Comet.material.SetFloat("_StrongTintFade", 0f);

            return base.TakeDamage(damage);
        }

        protected override IEnumerator DoDamageEffect()
        {
            yield return m_Comet.material.DOFloat(1f, "_StrongTintFade", 0.05f).WaitForCompletion();
            yield return m_Comet.material.DOFloat(0f, "_StrongTintFade", 0.05f).WaitForCompletion();
        }

        public override void SetDead()
        {
            IsDead = true;

            ShatterParts();

            SaveLoadManager.Instance.SaveTargetHealth(m_TargetIndex, -1);
        }

        /// <summary>
        /// Hides the comet and flings its parts outward. Shared by the level death path (SetDead, which also
        /// persists the kill) and by projectile mode, which must never write to the save.
        /// </summary>
        private void ShatterParts()
        {
            m_Fire.gameObject.SetActive(false);
            m_Comet.gameObject.SetActive(false);
            m_CometParts_Parent.SetActive(true);

            float angleStep = 360f / m_CometParts.Length;

            for (int i = 0; i < m_CometParts.Length; i++)
            {
                float force = Random.Range(1.5f, 3f);
                float duration = Random.Range(0.8f, 1.2f);
                float currentAngle = i * angleStep;
                var debris = m_CometParts[i];
                float rad = currentAngle * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                Vector3 targetPosition = transform.position + (Vector3)(direction * force);
                debris.transform.DOMove(targetPosition, duration).SetEase(Ease.OutQuad);
                debris.transform.DORotate(new Vector3(0, 0, Random.Range(-360, 360)), duration);
                debris.transform.DOScale(Vector3.zero, duration).SetEase(Ease.InBack);
                debris.DOFade(0f, duration);
            }
        }
    }
}
