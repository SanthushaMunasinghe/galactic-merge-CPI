using System.Collections.Generic;
using DG.Tweening;
using Oxtail.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    /// <summary>
    /// Bursts a swarm of items out of a source point and flies them into a target,
    /// filling a progress bar as each one lands.
    ///
    /// The VALUE carried is a BigNumber, so it works at any point in the
    /// incremental curve. The COUNT of coins stays an int on purpose: it is
    /// capped in the low tens and driven by feel, not by how much gold was won.
    ///
    /// Count rule, deliberately not random: below the cap the payout spawns one
    /// coin per unit of gold, so 7 gold visibly means 7 coins. At or above the
    /// cap it always spawns exactly Max Coins. Randomising it would make two
    /// identical payouts look different for no reason, and the player reads the
    /// swarm size as information.
    ///
    /// Everything happens in UI space, not world space: the coins are plain Image
    /// objects inside the canvas, so they stay pixel perfect at any resolution
    /// and can sit on top of the parchment panel.
    ///
    /// Feel rules that matter, and why:
    ///  - Coins scatter outward FIRST, then fly in. Going straight to the target
    ///    reads as a transfer; the scatter reads as a payout.
    ///  - Each coin is staggered a few frames apart, producing a stream.
    ///  - The counter updates as each coin LANDS, never before, so the number
    ///    always matches what the eye is seeing.
    /// </summary>
    public class UIBurstEffect : MonoBehaviour
    {
        [TabGroup("Setup")]
        [Required]
        [Tooltip("A UI Image with the coin sprite. No scripts needed on it.")]
        [SerializeField] private RectTransform m_ImagePrefab;

        [TabGroup("Setup")]
        [Required]
        [Tooltip("Where the coins burst from, usually the gold icon.")]
        [SerializeField] private RectTransform m_Source;

        [TabGroup("Setup")]
        [Required]
        [Tooltip("Where they fly to, usually the progress bar.")]
        [SerializeField] private RectTransform m_Target;

        [TabGroup("Setup")]
        [Tooltip("Parent for the spawned coins. Must be above the panel in draw order.")]
        [SerializeField] private RectTransform m_ImageLayer;

        [TabGroup("Amount")]
        [Tooltip("Coins spawned once the payout reaches this value. Small payouts " +
                 "spawn one coin per unit instead. More than ~30 is just noise " +
                 "and dropped frames.")]
        [MinValue(1)]
        [SerializeField] private int m_MaxCoins = 30;

        [TabGroup("Scatter")]
        [Tooltip("How far coins scatter from the source, in canvas units.")]
        [MinMaxSlider(20f, 400f, true)]
        [SerializeField] private Vector2 m_ScatterRadius = new Vector2(60f, 160f);

        [TabGroup("Scatter")]
        [Tooltip("Seconds of the outward scatter.")]
        [MinMaxSlider(0.1f, 1f, true)]
        [SerializeField] private Vector2 m_ScatterDuration = new Vector2(0.25f, 0.4f);

        [TabGroup("Scatter")]
        [SerializeField] private Ease m_ScatterEase = Ease.OutQuad;

        [TabGroup("Scatter")]
        [Tooltip("Bias the scatter upward so coins arc instead of sinking.")]
        [SerializeField] private float m_UpwardBias = 40f;

        [TabGroup("Flight")]
        [Tooltip("Seconds each coin takes to reach the target.")]
        [MinMaxSlider(0.2f, 1.5f, true)]
        [SerializeField] private Vector2 m_FlightDuration = new Vector2(0.4f, 0.65f);

        [TabGroup("Flight")]
        [SerializeField] private Ease m_FlightEase = Ease.InBack;

        [TabGroup("Flight")]
        [Tooltip("Seconds between each coin launching. Creates the stream effect.")]
        [PropertyRange(0f, 0.15f)]
        [SerializeField] private float m_LaunchStagger = 0.035f;

        [TabGroup("Flight")]
        [Tooltip("Pause after the scatter, before coins start flying in.")]
        [MinValue(0f)]
        [SerializeField] private float m_HoldBeforeFlight = 0.15f;

        [TabGroup("Visual")]
        [Tooltip("Coin size in canvas units.")]
        [SerializeField] private float m_CoinSize = 48f;

        [TabGroup("Visual")]
        [Tooltip("Random size variation, so the swarm does not look uniform.")]
        [PropertyRange(0f, 0.5f)]
        [SerializeField] private float m_SizeVariance = 0.25f;

        [TabGroup("Visual")]
        [Tooltip("Shrink as the coin reaches the target, selling the absorption.")]
        [SerializeField] private bool m_ShrinkOnArrival = true;

        [TabGroup("Visual")]
        [Tooltip("Spin speed in degrees per second. 0 disables spinning.")]
        [SerializeField] private float m_SpinSpeed = 220f;

        [TabGroup("Impact")]
        [Tooltip("Punch the target when a coin lands.")]
        [SerializeField] private bool m_PunchTarget = true;

        [TabGroup("Impact")]
        [SerializeField] private float m_PunchStrength = 0.12f;

        [TabGroup("Impact")]
        [Tooltip("Effect played at the target on each landing.")]
        [SerializeField] private ParticleSystem m_LandEffect;

        [TabGroup("Debug")]
        [ShowInInspector, ReadOnly] private bool m_IsPlaying;

        private readonly List<RectTransform> m_Pool = new(32);
        private readonly List<RectTransform> m_Active = new(32);
        private readonly List<BigNumber> m_Values = new(32);

        private Sequence m_Sequence;
        private Tween m_PunchTween;

        /// <summary>Raised as each item lands. Argument is the value it carried.</summary>
        public event System.Action<BigNumber> OnItemLanded;

        /// <summary>Raised once the whole burst has finished.</summary>
        public event System.Action OnBurstCompleted;

        public bool IsPlaying => m_IsPlaying;

        private void Awake()
        {
            if (m_ImageLayer == null) m_ImageLayer = transform as RectTransform;
            if (m_ImagePrefab != null) m_ImagePrefab.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            m_Sequence?.Kill();
            m_PunchTween?.Kill();
        }

        // ---------------- PUBLIC API ----------------

        /// <summary>
        /// Plays the burst. The total value is split across the coins, and
        /// OnCoinLanded fires with each portion so the counter can tick up.
        /// </summary>
        public void Play(BigNumber totalValue)
        {
            if (m_IsPlaying) Stop();
            if (m_ImagePrefab == null || m_Source == null || m_Target == null) return;
            if (totalValue.Sign <= 0) return;

            m_IsPlaying = true;

            SplitValue(totalValue, GetCoinCount(totalValue), m_Values);
            BuildSequence(m_Values);
        }

        /// <summary>Cancels the burst and returns every coin to the pool.</summary>
        [Button, DisableInEditorMode]
        public void Stop()
        {
            m_Sequence?.Kill();
            m_Sequence = null;

            for (int i = m_Active.Count - 1; i >= 0; i--)
                Recycle(m_Active[i]);

            m_Active.Clear();
            m_IsPlaying = false;
        }

        // ---------------- SEQUENCE ----------------

        private void BuildSequence(List<BigNumber> values)
        {
            m_Sequence = DOTween.Sequence().SetLink(gameObject).SetUpdate(true);

            Vector2 sourcePosition = ToLayerSpace(m_Source);

            for (int i = 0; i < values.Count; i++)
            {
                BigNumber value = values[i];
                int index = i;

                // Insert instead of Append so every coin starts at its own offset
                // on the same timeline, producing an overlapping stream.
                m_Sequence.InsertCallback(index * m_LaunchStagger,
                    () => LaunchCoin(sourcePosition, value));
            }

            float lastLaunch = (values.Count - 1) * m_LaunchStagger;
            float tail = m_ScatterDuration.y + m_HoldBeforeFlight + m_FlightDuration.y;

            m_Sequence.InsertCallback(lastLaunch + tail, () =>
            {
                m_IsPlaying = false;
                OnBurstCompleted?.Invoke();
            });
        }

        private void LaunchCoin(Vector2 sourcePosition, BigNumber value)
        {
            RectTransform coin = GetCoin();
            coin.anchoredPosition = sourcePosition;

            float size = m_CoinSize * (1f + Random.Range(-m_SizeVariance, m_SizeVariance));
            coin.sizeDelta = new Vector2(size, size);
            coin.localScale = Vector3.zero;
            coin.localRotation = Quaternion.identity;

            // Scatter point: random direction, biased upward so it arcs.
            float angle = Random.value * Mathf.PI * 2f;
            float distance = Random.Range(m_ScatterRadius.x, m_ScatterRadius.y);
            Vector2 scatterPoint = sourcePosition + new Vector2(
                Mathf.Cos(angle) * distance,
                Mathf.Sin(angle) * distance + m_UpwardBias);

            float scatterTime = Random.Range(m_ScatterDuration.x, m_ScatterDuration.y);
            float flightTime = Random.Range(m_FlightDuration.x, m_FlightDuration.y);

            Sequence coinSequence = DOTween.Sequence().SetLink(coin.gameObject).SetUpdate(true);

            coinSequence.Append(coin.DOScale(1f, scatterTime * 0.6f).SetEase(Ease.OutBack));
            coinSequence.Join(coin.DOAnchorPos(scatterPoint, scatterTime).SetEase(m_ScatterEase));
            coinSequence.AppendInterval(m_HoldBeforeFlight);

            // The target is read at flight time, not at build time: the bar may
            // have moved and a stale position would send coins to the wrong spot.
            coinSequence.AppendCallback(() =>
            {
                Vector2 targetPosition = ToLayerSpace(m_Target);

                Tween move = coin.DOAnchorPos(targetPosition, flightTime).SetEase(m_FlightEase);

                if (m_ShrinkOnArrival)
                    coin.DOScale(0.25f, flightTime).SetEase(Ease.InQuad).SetUpdate(true);

                move.SetUpdate(true).OnComplete(() =>
                {
                    Land(value);
                    Recycle(coin);
                });
            });

            if (Mathf.Approximately(m_SpinSpeed, 0f)) return;

            float turnTime = 360f / Mathf.Abs(m_SpinSpeed);

            coin.DOLocalRotate(new Vector3(0f, 0f, 360f * Mathf.Sign(m_SpinSpeed)), turnTime,
                               RotateMode.LocalAxisAdd)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart)
                .SetUpdate(true)
                .SetLink(coin.gameObject);
        }

        private void Land(BigNumber value)
        {
            OnItemLanded?.Invoke(value);

            if (m_LandEffect != null) m_LandEffect.Play();
            if (!m_PunchTarget) return;

            m_PunchTween?.Kill(complete: true);
            m_PunchTween = m_Target
                .DOPunchScale(Vector3.one * m_PunchStrength, 0.18f, 6, 0.8f)
                .SetUpdate(true)
                .SetLink(m_Target.gameObject);
        }

        // ---------------- POOL ----------------

        private RectTransform GetCoin()
        {
            RectTransform coin;

            if (m_Pool.Count > 0)
            {
                coin = m_Pool[m_Pool.Count - 1];
                m_Pool.RemoveAt(m_Pool.Count - 1);
            }
            else
            {
                coin = Instantiate(m_ImagePrefab, m_ImageLayer);
            }

            coin.gameObject.SetActive(true);
            coin.SetAsLastSibling();
            m_Active.Add(coin);

            return coin;
        }

        private void Recycle(RectTransform coin)
        {
            if (coin == null) return;

            coin.DOKill();
            coin.gameObject.SetActive(false);

            m_Active.Remove(coin);
            m_Pool.Add(coin);
        }

        // ---------------- HELPERS ----------------

        /// <summary>
        /// Converts any RectTransform position into the coin layer's local space,
        /// which is what anchoredPosition expects. Doing this properly is what
        /// makes the burst land exactly on the bar at every resolution, on both
        /// Overlay and Camera canvases.
        /// </summary>
        private Vector2 ToLayerSpace(RectTransform target)
        {
            Canvas canvas = m_ImageLayer.GetComponentInParent<Canvas>();
            Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(camera, target.position);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                m_ImageLayer, screenPoint, camera, out Vector2 localPoint);

            return localPoint;
        }

        /// <summary>
        /// One coin per unit of gold while the payout is small, capped at
        /// Max Coins after that. Comparing a BigNumber against an int works
        /// through the implicit conversion, so huge payouts short circuit here.
        /// </summary>
        private int GetCoinCount(BigNumber totalValue)
        {
            if (totalValue >= m_MaxCoins) return m_MaxCoins;

            // Safe to narrow: we already know it is below the cap.
            int asInt = Mathf.FloorToInt(totalValue.ToFloat());
            return Mathf.Clamp(asInt, 1, m_MaxCoins);
        }

        /// <summary>
        /// Splits the total across the coins so the sum is EXACT.
        ///
        /// Uses integer style division and modulo: every coin gets the base
        /// share, and the first "remainder" coins get one extra unit. That is
        /// how 83 across 8 coins becomes three 11s and five 10s rather than
        /// eight 10.375s rounded into a total that no longer adds up.
        ///
        /// The last coin still absorbs whatever is left over. Above 1e15 the
        /// BigNumber floor cannot resolve single units any more, so the modulo
        /// stops being meaningful and this guarantees the sum still matches the
        /// payout exactly.
        /// </summary>
        private static void SplitValue(BigNumber total, int count, List<BigNumber> result)
        {
            result.Clear();

            if (count <= 1)
            {
                result.Add(total);
                return;
            }

            BigNumber baseShare = BigNumber.Floor(total / count);
            BigNumber remainder = total % count;

            // Clamped because the modulo loses resolution on very large values.
            int extraCoins = Mathf.Clamp(Mathf.RoundToInt(remainder.ToFloat()), 0, count);

            BigNumber distributed = BigNumber.Zero;

            for (int i = 0; i < count - 1; i++)
            {
                BigNumber share = i < extraCoins ? baseShare + 1 : baseShare;

                result.Add(share);
                distributed += share;
            }

            result.Add(total - distributed);
        }

#if UNITY_EDITOR
        [TabGroup("Debug")]
        [Button("Test Burst")]
        private void TestBurst(string value = "1500")
        {
            if (!Application.isPlaying) return;
            Play(BigNumber.FromString(value));
        }
#endif
    }
}
