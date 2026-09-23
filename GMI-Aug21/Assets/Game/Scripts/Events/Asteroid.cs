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

    /// <summary>Raised once per attack-animation loop by a boss comet (see Asteroid.InitializeAsBoss),
    /// unlike AsteroidDestroyedByPlanetEvent this never destroys the asteroid.</summary>
    public struct BossAttackEvent
    {
        public Asteroid Asteroid;
        public float DamagePercent;
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
        [SerializeField] private GameObject m_Comet_Parent;

        [Header("Parts")]
        [SerializeField] private SpriteRenderer[] m_CometParts;

        [Header("Shake")]
        [Tooltip("Keeps the comet sprite jittering while it is alive. Turn off for enemies that animate on " +
            "their own (e.g. the blue cyclops).")]
        [SerializeField] private bool m_ShakeComet = true;

        [Header("Death Effects")]
        [Tooltip("Flings Comet Parts outward on death. Turn off for enemies with no debris.")]
        [SerializeField] private bool m_ShowDebris = true;
        [Tooltip("Plays Destroy Particle on death (needs one assigned). Independent of Show Debris.")]
        [SerializeField] private bool m_ShowDestroyParticle;
        [SerializeField] private ParticleSystem m_DestroyParticle;

        [Header("Projectile Mode")]
        [Tooltip("Turns the comet so it flies nose first, which keeps the fire trailing behind it. Only " +
            "used when a spawner drives this comet as a projectile.")]
        [SerializeField] private bool m_AlignToTravelDirection = true;
        [SerializeField] private float m_TravelRotationOffsetDegrees;

        [Header("Hit Detection")]
        [Tooltip("Added to a bullet's own Hit Radius when checking whether it connected with this comet, on " +
            "top of the distance from the bullet to this transform's position (its pivot, not its visual " +
            "edge). Tune this per prefab to roughly match half the sprite's visible width/height — a wide " +
            "sprite (like the boss) needs a much bigger radius than a small one, or bullets that clearly " +
            "clip its edges will still miss.")]
        [SerializeField, Min(0f)] private float m_HitRadius = 0.5f;

        [Header("Boss")]
        [Tooltip("Animator this comet triggers Attack on once it stops to fight. Leave unset on a non-boss " +
            "comet.")]
        [SerializeField] private Animator m_BossAnimator;

        private static readonly int k_BossAttackTrigger = Animator.StringToHash("Attack");

        private bool m_IsProjectile;
        private ProjectileState m_State;
        private float m_CenteringSpeed;
        private Transform m_PlanetCenter;
        private float m_PlanetBoundsRadius;
        private System.Action m_OnJumpComplete;
        private bool m_NotifiedJumpComplete;
        private bool m_IsShattered;
        private int m_HitPointsLeft = 1;

        private bool m_IsBoss;
        private bool m_IsBossAttacking;
        private bool m_BossAttackStopped;
        private bool m_BossHeld;
        private float m_BossMoveSpeed;
        private float m_BossStopY;
        private float m_BossAttackDamagePercent;

        /// <summary>True once this comet has arrived inside the planet bounds.</summary>
        public bool HasReachedCenter { get; private set; }

        /// <summary>How far from this comet's pivot a bullet still counts as having hit it (added to the
        /// bullet's own Hit Radius). See the Hit Detection tooltip for how to tune it per prefab.</summary>
        public float HitRadius => m_HitRadius;

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
            if (!m_ShakeComet)
                return;

            m_Comet.transform.DOShakePosition(0.35f, 0.1f).SetLoops(-1);
        }

        private void Update()
        {
            if (m_IsBoss)
            {
                UpdateBoss();
                return;
            }

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

        /// <summary>
        /// Hands this comet to a WaveManager for grid-formation mode: registers into ActiveAsteroids so
        /// bullets can target/hit it exactly like a projectile-mode comet, but it never slides or homes —
        /// the wave manager's own transform carries it along as a passive child. Reuses the same arrival
        /// check InitializeAsProjectile uses (PlanetPosition / Planet Bounds Radius, evaluated every
        /// Update), just pointed at the Circuits ring instead of the planet: getting within
        /// circleBoundsRadius of circleCenter counts as the asteroid reaching it, and raises
        /// AsteroidDestroyedByPlanetEvent exactly like a real planet hit, so CPIManager's existing
        /// health-loss handling applies unchanged.
        /// </summary>
        public void InitializeInGrid(Transform circleCenter, float circleBoundsRadius, int hitPoints = 1)
        {
            m_IsProjectile = true;
            m_PlanetCenter = circleCenter;
            m_PlanetBoundsRadius = circleBoundsRadius;
            m_HitPointsLeft = Mathf.Max(1, hitPoints);

            ActiveAsteroids.Add(this);
        }

        /// <summary>
        /// Hands this comet to a WaveManager as a boss: it walks straight down at moveSpeed, independent of
        /// any wave transform, until its Y reaches stopY (a world Y, computed by WaveManager the same way as
        /// its Spawn Distance line), then stops and triggers its Boss Animator's Attack state. Unlike
        /// InitializeInGrid it never calls HandlePlanetHit, so it stays alive and targetable by bullets
        /// (HasReachedCenter never becomes true) — the Attack clip loops on its own, and its Animation Event
        /// (OnBossAttackLanded) raises BossAttackEvent once per loop, repeating the attack for as long as the
        /// boss survives. Sets m_IsProjectile too, exactly like InitializeInGrid, so Start() skips the
        /// hand-placed level drop-in tween. Starts held at its spawn point (see m_BossHeld) — WaveManager
        /// releases it with ReleaseBossAdvance once the wave's own sub-wave grid is fully cleared.
        /// </summary>
        public void InitializeAsBoss(float moveSpeed, float stopY, float attackDamagePercent, int hitPoints = 1)
        {
            m_IsProjectile = true;
            m_IsBoss = true;
            m_BossHeld = true;
            m_BossMoveSpeed = moveSpeed;
            m_BossStopY = stopY;
            m_BossAttackDamagePercent = attackDamagePercent;
            m_HitPointsLeft = Mathf.Max(1, hitPoints);

            ActiveAsteroids.Add(this);
        }

        /// <summary>Lets a boss held at its spawn point (see InitializeAsBoss) start walking down toward its
        /// Boss Stop Distance line. Called by WaveManager once every grid asteroid from the same wave is
        /// gone. No-op on a non-boss comet or one already released.</summary>
        public void ReleaseBossAdvance()
        {
            if (!m_IsBoss)
                return;

            m_BossHeld = false;
        }

        private void UpdateBoss()
        {
            if (m_IsShattered || m_IsBossAttacking || m_BossHeld)
                return;

            transform.position += Vector3.down * m_BossMoveSpeed * Time.deltaTime;

            if (transform.position.y <= m_BossStopY)
                BeginBossAttack();
        }

        private void BeginBossAttack()
        {
            m_IsBossAttacking = true;

            if (m_BossAnimator != null)
                m_BossAnimator.SetTrigger(k_BossAttackTrigger);
        }

        /// <summary>Called once per loop of the boss's looping Attack clip, via a BossAttackRelay on the
        /// same GameObject as the Animator (an Animation Event can't reach this directly, since Asteroid is
        /// one level up from the Animator on the prefab's sprite child).</summary>
        public void OnBossAttackLanded()
        {
            if (m_BossAttackStopped)
                return;

            EventManager<BossAttackEvent>.TriggerEvent(new BossAttackEvent { Asteroid = this, DamagePercent = m_BossAttackDamagePercent });
        }

        /// <summary>
        /// Immediately halts this boss's attack: freezes its Animator (so the looping Attack clip stops
        /// dead, mid frame, instead of finishing its loop) and ignores any OnBossAttackLanded that was
        /// already in flight. Called by CPIManager the instant the planet's health reaches zero or the
        /// level fails, so the boss can't land another hit while the fail sequence catches up. A no-op on a
        /// non-boss comet or one already stopped.
        /// </summary>
        public void StopBossAttack()
        {
            if (!m_IsBoss || m_BossAttackStopped)
                return;

            m_BossAttackStopped = true;

            if (m_BossAnimator != null)
                m_BossAnimator.enabled = false;
        }

        /// <summary>
        /// Applies one bullet hit and returns true if it was the killing one (the caller then destroys the
        /// comet the usual way). A non-lethal hit just flashes the comet and leaves it alive. Comets start with
        /// one hit point, so anything that never calls InitializeInGrid with a health dies to the first bullet
        /// exactly as before.
        /// </summary>
        public bool TakeBulletHit()
        {
            m_HitPointsLeft--;

            if (m_HitPointsLeft <= 0)
                return true;

            PlayHitFlash();
            return false;
        }

        private void PlayHitFlash()
        {
            Material material = m_Comet.material;
            if (!material.HasProperty("_StrongTintFade"))
                return;

            // Restarts cleanly if a second bullet lands mid-flash.
            m_Comet.transform.DOKill();
            material.SetFloat("_StrongTintFade", 0f);

            DOTween.Sequence()
                .Append(material.DOFloat(1f, "_StrongTintFade", 0.05f))
                .Append(material.DOFloat(0f, "_StrongTintFade", 0.05f))
                .SetTarget(m_Comet.transform);
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
        /// Immediately stops this comet moving and being targetable, plays the same death effects the level
        /// death uses (debris and/or destroy particle, per their toggles), then destroys the GameObject once
        /// they have played. Unlike SetDead it never writes to the save. Safe to call more than once.
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

            Destroy(gameObject, PlayDeathEffects());
        }

        /// <summary>
        /// Removes this comet immediately: no scatter effect and no destroyed-by-bullet or
        /// destroyed-by-planet event, so nothing (collect points, health loss) follows from it. Used by
        /// WaveManager for grid asteroids that scrolled past the circuit unharmed. Safe to call more than
        /// once, and a no-op on a comet that is already dying.
        /// </summary>
        public void DestroyQuietly()
        {
            if (m_IsShattered)
                return;

            m_IsShattered = true;
            IsDead = true;

            transform.DOKill();
            ActiveAsteroids.Remove(this);

            Destroy(gameObject);
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

            PlayDeathEffects();

            SaveLoadManager.Instance.SaveTargetHealth(m_TargetIndex, -1);
        }

        /// <summary>
        /// Hides the comet (and its fire, if it has one), then plays whichever death effects are switched on:
        /// Show Debris flings the parts outward, and Show Destroy Particle plays the assigned particle
        /// system. The two are independent. Returns how long this GameObject has to stay alive for the
        /// effects to finish, 0 when none played. Shared by the level death path (SetDead, which also
        /// persists the kill) and by projectile mode, which must never write to the save.
        /// </summary>
        private float PlayDeathEffects()
        {
            if (m_Fire != null)
                m_Fire.gameObject.SetActive(false);

            m_Comet.gameObject.SetActive(false);

            float lifetime = 0f;

            if (m_ShowDebris && m_Comet_Parent != null && m_CometParts != null && m_CometParts.Length > 0)
            {
                FlingDebris();
                lifetime = k_ShatterLifetime;
            }

            if (m_ShowDestroyParticle && m_DestroyParticle != null)
            {
                m_DestroyParticle.gameObject.SetActive(true);
                m_DestroyParticle.Play();

                ParticleSystem.MainModule main = m_DestroyParticle.main;
                lifetime = Mathf.Max(lifetime, main.duration + main.startLifetime.constantMax);
            }

            return lifetime;
        }

        private void FlingDebris()
        {
            m_Comet_Parent.SetActive(true);

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
