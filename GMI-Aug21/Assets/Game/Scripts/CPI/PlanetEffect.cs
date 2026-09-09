using DG.Tweening;
using System;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    /// <summary>
    /// Owns this planet's health when Health Source is left unassigned (defaults to self in Awake,
    /// like Target Object): tracks a 0-100 Health value, instances its Health Renderer's material in
    /// Awake so the shared material asset is never mutated, and exposes ApplyDamage/ApplyHeal for
    /// CPIManager to call once it has resolved which planet a hit/collection belongs to (via the
    /// PlanetEffect component sitting on that planet's trigger Collider GameObject). ApplyDamage takes
    /// one extra hit after Health has already reached zero before flipping IsDestroyed and firing
    /// OnDestroyed — mirroring CPIManager's previous global "one extra hit after zero" rule, now
    /// scoped per planet — and both ApplyDamage and ApplyHeal are no-ops once IsDestroyed is true, so
    /// a destroyed planet never regains health. When Health Source instead points at a different
    /// PlanetEffect (for cosmetic-only sprite/glow/ring/particle instances that share a physical
    /// planet with a health-owning one), this instance owns no health of its own and instead
    /// subscribes to the source's OnHit/OnHealthGained to drive the same scale pulse below, while
    /// OnUpgradePerformed remains a global CPIManager event since upgrades are not per-planet.
    ///
    /// Plays a scale pulse on the assigned target whenever this planet (or its Health Source) reports
    /// a hit, an upgrade, or a health gain: target scale eases to Initial Scale * First Scale
    /// Multiplier, then to Initial Scale * Second Scale Multiplier, then back to Initial Scale. A
    /// multiplier below 1 shrinks, above 1 grows. A trigger received while the pulse is already
    /// playing is ignored.
    /// </summary>
    public class PlanetEffect : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform m_TargetObject;

        [Header("Scale Effect")]
        [SerializeField] private float m_FirstScaleMultiplier = 0.9f;
        [SerializeField] private float m_SecondScaleMultiplier = 1.05f;
        [SerializeField, Min(0f)] private float m_FirstStageDuration = 0.1f;
        [SerializeField, Min(0f)] private float m_SecondStageDuration = 0.1f;
        [SerializeField, Min(0f)] private float m_ReturnDuration = 0.1f;
        [SerializeField] private Ease m_Ease = Ease.OutQuad;

        [Header("Health Source")]
        [SerializeField] private PlanetEffect m_HealthSource;

        [Header("Health")]
        [SerializeField] private Renderer m_PlanetHealthRenderer;
        [SerializeField, Range(0f, 100f)] private float m_HealthGainPercent = 10f;
        [SerializeField, Range(0f, 100f)] private float m_HealthLossPercent = 10f;
        [SerializeField, Min(0f)] private float m_HealthRefillCooldown = 3f;

        [Header("HP Floating Text")]
        [SerializeField] private RectTransform m_HpParticleSpawnPoint;

        private Vector3 m_InitialScale;
        private bool m_IsPlaying;
        private float m_HealthRefillUnlockTime;
        private bool m_IsDestroyed;

        public float Health { get; private set; }
        public bool IsDestroyed => m_IsDestroyed;
        public RectTransform HpParticleSpawnPoint => m_HpParticleSpawnPoint;

        /// <summary>Fired once, the moment this planet transitions to destroyed.</summary>
        public event Action OnDestroyed;

        /// <summary>Fired every time this specific planet takes a hit (including the hit that also
        /// fires OnDestroyed), so cosmetic sibling PlanetEffect instances sharing this Health Source
        /// can react via the same scale pulse without a global CPIManager event.</summary>
        public event Action OnHit;

        /// <summary>Fired every time this specific planet's health is actually restored (not while its
        /// post-hit refill cooldown is active).</summary>
        public event Action OnHealthGained;

        private void Awake()
        {
            if (m_TargetObject == null)
                m_TargetObject = transform;

            if (m_HealthSource == null)
                m_HealthSource = this;

            m_InitialScale = m_TargetObject.localScale;

            if (m_HealthSource == this)
            {
                if (m_PlanetHealthRenderer != null)
                    m_PlanetHealthRenderer.material = new Material(m_PlanetHealthRenderer.material);

                Health = 0f;
                ApplyHealthFill();
            }
        }

        private void OnEnable()
        {
            if (m_HealthSource != null)
            {
                m_HealthSource.OnHit += PlayEffect;
                m_HealthSource.OnHealthGained += PlayEffect;
            }

            if (CPIManager.Instance != null)
                CPIManager.Instance.OnUpgradePerformed += PlayEffect;
        }

        private void OnDisable()
        {
            if (m_HealthSource != null)
            {
                m_HealthSource.OnHit -= PlayEffect;
                m_HealthSource.OnHealthGained -= PlayEffect;
            }

            if (CPIManager.Instance != null)
                CPIManager.Instance.OnUpgradePerformed -= PlayEffect;
        }

        /// <summary>
        /// Applies one asteroid hit's worth of damage to this planet. A no-op if this planet is
        /// already destroyed. Otherwise: records whether health was already at zero BEFORE
        /// subtracting this hit's loss percent (preserving the "one extra hit after already-zero"
        /// rule — this planet only flips to destroyed on the hit AFTER the one that first brought it
        /// to zero), applies the loss, restarts the refill cooldown, and fires OnHit; if health was
        /// already zero going into this hit, also flips IsDestroyed and fires OnDestroyed.
        /// </summary>
        public void ApplyDamage()
        {
            if (m_IsDestroyed)
                return;

            bool wasAtZeroHealth = Health <= 0f;

            ChangeHealth(-m_HealthLossPercent);
            m_HealthRefillUnlockTime = Time.time + m_HealthRefillCooldown;
            OnHit?.Invoke();

            if (wasAtZeroHealth)
            {
                m_IsDestroyed = true;
                OnDestroyed?.Invoke();
            }
        }

        /// <summary>
        /// Applies a Collect Point's worth of healing to this planet. A no-op if this planet is
        /// already destroyed (mirrors ApplyDamage: a dead planet never heals back), or if its
        /// post-hit refill cooldown hasn't elapsed yet.
        /// </summary>
        public void ApplyHeal()
        {
            if (m_IsDestroyed || Time.time < m_HealthRefillUnlockTime)
                return;

            ChangeHealth(m_HealthGainPercent);
            OnHealthGained?.Invoke();
        }

        private void ChangeHealth(float delta)
        {
            Health = Mathf.Clamp(Health + delta, 0f, 100f);
            ApplyHealthFill();

            if (delta > 0f)
                UIParticleActions.PlayHpGained(delta, m_HpParticleSpawnPoint);
            else if (delta < 0f)
                UIParticleActions.PlayHpLost(-delta, m_HpParticleSpawnPoint);
        }

        private void ApplyHealthFill()
        {
            if (m_PlanetHealthRenderer == null)
                return;

            m_PlanetHealthRenderer.material.SetFloat("_Fill", Health / 100f);
        }

        private void PlayEffect()
        {
            if (m_IsPlaying)
                return;

            m_IsPlaying = true;

            m_TargetObject.DOKill();
            DOTween.Sequence()
                .Append(m_TargetObject.DOScale(m_InitialScale * m_FirstScaleMultiplier, m_FirstStageDuration).SetEase(m_Ease))
                .Append(m_TargetObject.DOScale(m_InitialScale * m_SecondScaleMultiplier, m_SecondStageDuration).SetEase(m_Ease))
                .Append(m_TargetObject.DOScale(m_InitialScale, m_ReturnDuration).SetEase(m_Ease))
                .OnComplete(() => m_IsPlaying = false);
        }
    }
}
