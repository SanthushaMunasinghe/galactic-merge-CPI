using DG.Tweening;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    /// <summary>
    /// One planet in the CPI scene — the 2D sprite with the sand fill on it. It owns how big it is and how
    /// it shows health, and it detects its own arrivals: every frame it absorbs any comet or collect point
    /// that has come within Bounds Radius of its position, rather than each of those checking a distance
    /// for itself. CPIManager holds the array of these, so any number of planets can exist side by side.
    ///
    /// Health itself is not stored here: CPIManager keeps a single shared pool and pushes the same value
    /// into every planet's sand fill, so all of them fill and drain together.
    ///
    /// It also plays a scale pulse on the assigned target whenever CPIManager reports a planet hit, an
    /// upgrade, or a planet health gain: target scale eases to Initial Scale * First Scale Multiplier,
    /// then to Initial Scale * Second Scale Multiplier, then back to Initial Scale. A multiplier below
    /// 1 shrinks, above 1 grows. A trigger received while the pulse is already playing is ignored.
    /// </summary>
    public class PlanetEffect : MonoBehaviour
    {
        [Header("Planet Bounds")]
        [Tooltip("How close a comet or collect point has to get to this planet to be absorbed by it. Tune " +
            "it against the magenta gizmo.")]
        [SerializeField, Min(0f)] private float m_BoundsRadius = 1.5f;

        [Header("Health Visual")]
        [Tooltip("The sand dissolve on this planet's greyscale sprite (the one layered over the colored " +
            "sprite). Left empty, it is found on this object or its children.")]
        [SerializeField] private SandDissolveEffect m_Dissolve;

        [Header("Target")]
        [SerializeField] private Transform m_TargetObject;

        [Header("Scale Effect")]
        [SerializeField] private float m_FirstScaleMultiplier = 0.9f;
        [SerializeField] private float m_SecondScaleMultiplier = 1.05f;
        [SerializeField, Min(0f)] private float m_FirstStageDuration = 0.1f;
        [SerializeField, Min(0f)] private float m_SecondStageDuration = 0.1f;
        [SerializeField, Min(0f)] private float m_ReturnDuration = 0.1f;
        [SerializeField] private Ease m_Ease = Ease.OutQuad;

        private Vector3 m_InitialScale;
        private bool m_IsPlaying;

        /// <summary>What comets and collect points home toward, and what arrival is measured from.</summary>
        public Vector3 Center => transform.position;

        public float BoundsRadius => m_BoundsRadius;

        private void Awake()
        {
            if (m_TargetObject == null)
                m_TargetObject = transform;

            // Auto found so a planet needs no wiring for this: the sand fill always lives on the
            // greyscale child.
            if (m_Dissolve == null)
                m_Dissolve = GetComponentInChildren<SandDissolveEffect>();

            m_InitialScale = m_TargetObject.localScale;
        }

        /// <summary>True when worldPosition has arrived inside this planet's bounds.</summary>
        public bool Contains(Vector3 worldPosition)
        {
            return (worldPosition - Center).sqrMagnitude <= m_BoundsRadius * m_BoundsRadius;
        }

        /// <summary>
        /// Maps a 0-1 health progress onto this planet's sand fill: 0 leaves the grey layer fully intact
        /// (the planet reads as empty) and 1 dissolves it away completely, revealing the colored planet
        /// underneath. Set immediately at startup so the planet opens on the right state instead of
        /// animating there, and animated on every change after that so grains fly.
        /// </summary>
        public void ApplyHealthVisual(float progress, bool immediate)
        {
            if (m_Dissolve == null)
                return;

            if (immediate)
                m_Dissolve.SetProgressImmediate(progress);
            else
                m_Dissolve.SetProgress(progress);
        }

        // Iterated backwards because absorbing removes the entry from the list it is walking.
        private void Update()
        {
            for (int i = Asteroid.ActiveAsteroids.Count - 1; i >= 0; i--)
            {
                Asteroid asteroid = Asteroid.ActiveAsteroids[i];
                if (asteroid == null || asteroid.HasReachedCenter)
                    continue;

                if (Contains(asteroid.transform.position))
                    asteroid.AbsorbInto(this);
            }

            // Collect points move in FixedUpdate while this runs in Update, which cannot make one slip
            // through: MoveTowards clamps at the planet rather than overshooting it, so a point that
            // arrives between two frames simply sits there until this catches it.
            for (int i = CollectPoint.ActivePoints.Count - 1; i >= 0; i--)
            {
                CollectPoint point = CollectPoint.ActivePoints[i];
                if (point == null)
                    continue;

                if (Contains(point.transform.position))
                    point.CollectInto(this);
            }
        }

        private void OnEnable()
        {
            if (CPIManager.Instance != null)
            {
                CPIManager.Instance.OnPlanetHit += PlayEffect;
                CPIManager.Instance.OnUpgradePerformed += PlayEffect;
                CPIManager.Instance.OnPlanetHealthGained += PlayEffect;
            }
        }

        private void OnDisable()
        {
            if (CPIManager.Instance != null)
            {
                CPIManager.Instance.OnPlanetHit -= PlayEffect;
                CPIManager.Instance.OnUpgradePerformed -= PlayEffect;
                CPIManager.Instance.OnPlanetHealthGained -= PlayEffect;
            }
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

        private void OnDrawGizmosSelected()
        {
            // Center is a world position, and Gizmos.matrix is shared static state another component may
            // have left pointing at its own local space, so it is reset rather than assumed.
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(Center, m_BoundsRadius);
        }
    }
}
