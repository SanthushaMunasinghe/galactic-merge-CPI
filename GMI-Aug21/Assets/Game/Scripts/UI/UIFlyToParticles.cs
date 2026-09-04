using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// UI particle burst that flies from a UI element (screen space, e.g. the
/// "ADD REWARD LINE" HUD button) to a WORLD-space target (e.g. the slot on
/// the circuit where the reward line will be placed).
///
/// Rendered inside a Canvas (Screen Space - Overlay recommended), so the
/// particles stay visible on top of both the HUD and the world.
///
/// Mobile notes: fixed-size pool created in Awake, zero per-frame allocations,
/// raycastTarget disabled on particles, one canvas -> batches fine.
/// </summary>
namespace Oxtail.SpaceshipIncremental
{
    public class UIFlyToParticles : MonoBehaviour
    {
        [Tooltip("Recompute the target screen position while flying. Enable it if the camera or the target can move (cost: one conversion per frame while active).")]
        [SerializeField] private bool m_FollowTarget = true;

        [Header("Burst")]
        [Tooltip("Particles per burst.")]
        [SerializeField, Range(1, 32)] private int m_Count = 8;

        [Tooltip("Delay between consecutive particles (seconds).")]
        [SerializeField] private float m_Stagger = 0.06f;

        [Tooltip("Flight duration of one particle (seconds).")]
        [SerializeField] private float m_FlightTime = 0.60f;

        [Tooltip("Random variation applied to the flight time (0 = none, 1 = +-50%).")]
        [SerializeField, Range(0f, 1f)] private float m_FlightTimeRandom = 0.2f;

        [Tooltip("Bezier arc height in canvas units (perpendicular offset of the control point).")]
        [SerializeField] private float m_ArcHeight = 140f;

        [Tooltip("Random variation of the arc height and side (0 = all identical arcs).")]
        [SerializeField, Range(0f, 1f)] private float m_ArcRandom = 0.6f;

        [Tooltip("Spawn position jitter in canvas units, so particles do not start on the same pixel.")]
        [SerializeField] private float m_SpawnJitter = 14f;

        [Tooltip("Spin speed in degrees per second (random sign per particle). Set 0 to disable.")]
        [SerializeField] private float m_SpinSpeed = 240f;

        [Header("Look")]
        [Tooltip("Sprites picked at random per particle (coin, spark...). If empty, a procedural golden dot is used.")]
        [SerializeField] private Sprite[] m_Sprites;

        [Tooltip("Particle size in canvas units.")]
        [SerializeField] private Vector2 m_Size = new Vector2(44f, 44f);

        [Tooltip("Relative scale at spawn; the particle grows to 1 with an elastic overshoot during the first 25% of flight.")]
        [SerializeField] private float m_SpawnScale = 0.4f;

        [Tooltip("Scale peak of the arrival pop.")]
        [SerializeField] private float m_ArrivalPop = 1.5f;

        [Tooltip("Duration of the arrival pop (seconds).")]
        [SerializeField, Min(0.01f)] private float m_PopTime = 0.14f;

        [Tooltip("Pool size. Must be >= m_Count x the bursts you may run simultaneously.")]
        [SerializeField, Min(4)] private int m_PoolSize = 16;

        [Header("Events")]
        [Tooltip("Fired each time one particle reaches the target (good for tick sounds / counters).")]
        [SerializeField] private UnityEvent m_OnParticleArrived = new UnityEvent();

        [Tooltip("Fired once when the whole burst has arrived (good for spawning the reward line, flashes, etc.).")]
        [SerializeField] private UnityEvent m_OnAllArrived = new UnityEvent();

        // ------------------------------------------------------------------

        private enum Phase { Free, Delayed, Flying, Popping }
        private enum TargetMode { None, FixedWorld, FollowWorld, UIRect }

        private sealed class FlyParticle
        {
            public Phase Phase = Phase.Free;
            public float Delay;
            public float T;            // 0..1 flight progress
            public float Dur;          // flight duration
            public float PopT;         // 0..1 pop progress
            public float Angle;        // accumulated spin
            public float SpinMul;      // random sign + speed multiplier
            public Vector2 A;          // start (canvas-local)
            public Vector2 C;          // bezier control (canvas-local)
            public Image Img;
            public RectTransform Rt;
        }

        private RectTransform m_SourceRect;
        private Transform m_TargetWorld;

        private RectTransform m_Rect;
        private Canvas m_Canvas;
        private Camera m_CanvasCam;    // null when the canvas is Screen Space - Overlay
        private Camera m_WorldCam;
        private readonly List<FlyParticle> m_Pool = new List<FlyParticle>();
        private bool m_Running;

        private TargetMode m_TargetMode = TargetMode.None;
        private Transform m_RuntimeWorldTarget;
        private Vector3 m_FixedWorldPos;
        private RectTransform m_RuntimeUiTarget;

        private static Sprite s_FallbackSprite;

        public bool IsRunning { get { return m_Running; } }
        public UnityEvent OnParticleArrived { get { return m_OnParticleArrived; } }
        public UnityEvent OnAllArrived { get { return m_OnAllArrived; } }

        /// <summary>Lets gameplay code swap the source button at runtime.</summary>
        public RectTransform SourceRect { get { return m_SourceRect; } set { m_SourceRect = value; } }

        /// <summary>Use this if your game switches cameras at runtime.</summary>
        public void SetWorldCamera(Camera cam) { m_WorldCam = cam; }

        private void Awake()
        {
            m_Rect = (RectTransform)transform;
            m_Canvas = GetComponentInParent<Canvas>();
            if (m_Canvas == null)
            {
                Debug.LogError("[UIFlyToParticles] This component must live under a Canvas.", this);
                enabled = false;
                return;
            }
            m_CanvasCam = m_Canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : m_Canvas.worldCamera;
            m_WorldCam = Camera.main;   // cached: Camera.main lookup once, not per frame
            BuildPool();
        }

        private void OnValidate()
        {
            // Keep pooled particles in sync when tweaking size in the editor.
            for (int i = 0; i < m_Pool.Count; i++)
                if (m_Pool[i].Rt != null) m_Pool[i].Rt.sizeDelta = m_Size;
        }

        // ------------------------------------------------------------------
        // Public API
        // ------------------------------------------------------------------

        /// <summary>Fire a burst using the endpoints assigned in the Inspector.</summary>
        public void Play(RectTransform source, Transform end)
        {
            m_SourceRect = source;
            m_TargetWorld = end;

            if (m_TargetWorld == null)
            {
                Debug.LogWarning("[UIFlyToParticles] No m_TargetWorld assigned; use Play(Vector3) or Play(RectTransform) instead.", this);
                return;
            }
            m_RuntimeWorldTarget = m_TargetWorld;
            m_TargetMode = m_FollowTarget ? TargetMode.FollowWorld : TargetMode.FixedWorld;
            if (!m_FollowTarget) m_FixedWorldPos = m_TargetWorld.position;
            SpawnBurst();
        }

        /// <summary>Fire a burst from the inspector source to a fixed world position (e.g. slot.position).</summary>
        public void Play(Vector3 worldPos)
        {
            m_FixedWorldPos = worldPos;
            m_TargetMode = TargetMode.FixedWorld;
            SpawnBurst();
        }

        /// <summary>Fire a burst towards a moving world transform.</summary>
        public void Play(Transform worldTarget)
        {
            if (worldTarget == null) return;
            m_RuntimeWorldTarget = worldTarget;
            m_TargetMode = TargetMode.FollowWorld;
            SpawnBurst();
        }

        /// <summary>Fire a burst from the inspector source to another UI element (UI -> UI).</summary>
        public void Play(RectTransform uiTarget)
        {
            if (uiTarget == null) return;
            m_RuntimeUiTarget = uiTarget;
            m_TargetMode = TargetMode.UIRect;
            SpawnBurst();
        }

        /// <summary>Instantly hides every particle and stops the burst.</summary>
        public void Stop()
        {
            for (int i = 0; i < m_Pool.Count; i++)
            {
                m_Pool[i].Phase = Phase.Free;
                if (m_Pool[i].Img != null) m_Pool[i].Img.enabled = false;
            }
            m_Running = false;
        }

        // ------------------------------------------------------------------
        // Internals
        // ------------------------------------------------------------------

        private void BuildPool()
        {
            Sprite fallback = GetFallbackSprite();
            for (int i = 0; i < m_PoolSize; i++)
            {
                GameObject go = new GameObject("FlyParticle_" + i, typeof(RectTransform), typeof(Image));
                RectTransform rt = (RectTransform)go.transform;
                rt.SetParent(m_Rect, false);
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = m_Size;
                Image img = go.GetComponent<Image>();
                img.sprite = fallback;
                img.raycastTarget = false;   // never block taps
                img.enabled = false;
                m_Pool.Add(new FlyParticle { Img = img, Rt = rt });
            }
        }

        private void SpawnBurst()
        {
            if (m_SourceRect == null)
            {
                Debug.LogWarning("[UIFlyToParticles] m_SourceRect is not assigned.", this);
                return;
            }
            Vector2 aBase = UiToLocal(m_SourceRect);
            Vector2 bBase = GetTargetLocal();
            int spawned = 0;
            for (int i = 0; i < m_Pool.Count && spawned < m_Count; i++)
            {
                FlyParticle p = m_Pool[i];
                if (p.Phase != Phase.Free) continue;
                SetupParticle(p, spawned, aBase, bBase);
                spawned++;
            }
            if (spawned > 0) m_Running = true;
            else Debug.LogWarning("[UIFlyToParticles] Pool exhausted: raise m_PoolSize or lower m_Count.", this);
        }

        private void SetupParticle(FlyParticle p, int index, Vector2 aBase, Vector2 bBase)
        {
            p.A = aBase + Random.insideUnitCircle * m_SpawnJitter;
            Vector2 dir = bBase - p.A;
            Vector2 perp = dir.sqrMagnitude > 0.001f ? new Vector2(-dir.y, dir.x) / dir.magnitude : Vector2.up;
            float side = Random.value < 0.5f ? -1f : 1f;
            float arc = m_ArcHeight * (1f - m_ArcRandom * 0.5f + m_ArcRandom * Random.value) * side;
            p.C = (p.A + bBase) * 0.5f + perp * arc;

            p.Dur = m_FlightTime * (1f - m_FlightTimeRandom * 0.5f + m_FlightTimeRandom * Random.value);
            p.Delay = index * m_Stagger;
            p.T = 0f;
            p.PopT = 0f;
            p.Angle = 0f;
            p.SpinMul = (Random.value < 0.5f ? -1f : 1f) * (0.7f + 0.6f * Random.value);

            if (m_Sprites != null && m_Sprites.Length > 0)
                p.Img.sprite = m_Sprites[Random.Range(0, m_Sprites.Length)];
            p.Img.color = new Color(1f, 1f, 1f, 0f);
            p.Rt.localPosition = p.A;
            p.Rt.localEulerAngles = Vector3.zero;
            p.Rt.localScale = Vector3.one * m_SpawnScale;
            p.Phase = Phase.Delayed;
        }

        private void Update()
        {
            if (!m_Running) return;
            float dt = Time.deltaTime;
            bool anyActive = false;

            for (int i = 0; i < m_Pool.Count; i++)
            {
                FlyParticle p = m_Pool[i];
                switch (p.Phase)
                {
                    case Phase.Delayed:
                        anyActive = true;
                        p.Delay -= dt;
                        if (p.Delay <= 0f)
                        {
                            p.Phase = Phase.Flying;
                            p.Img.enabled = true;
                        }
                        break;

                    case Phase.Flying:
                        {
                            anyActive = true;
                            p.T += dt / Mathf.Max(0.01f, p.Dur);
                            float t = Mathf.Clamp01(p.T);
                            Vector2 b = GetTargetLocal();
                            Vector2 pos = Bezier(p.A, p.C, b, EaseInOutQuad(t));

                            float growT = t < 0.25f ? t / 0.25f : 1f;
                            float scale = Mathf.LerpUnclamped(m_SpawnScale, 1f, EaseOutBack(growT));
                            p.Angle += m_SpinSpeed * p.SpinMul * dt;

                            p.Rt.localPosition = pos;
                            p.Rt.localEulerAngles = new Vector3(0f, 0f, p.Angle);
                            p.Rt.localScale = new Vector3(scale, scale, 1f);
                            Color c = p.Img.color;
                            c.a = Mathf.Clamp01(t / 0.12f);
                            p.Img.color = c;

                            if (p.T >= 1f)
                            {
                                p.Rt.localPosition = b;
                                p.Phase = Phase.Popping;
                                p.PopT = 0f;
                                m_OnParticleArrived.Invoke();
                            }
                            break;
                        }

                    case Phase.Popping:
                        {
                            anyActive = true;
                            p.PopT += dt / Mathf.Max(0.01f, m_PopTime);
                            float q = Mathf.Clamp01(p.PopT);
                            float pop = q < 0.5f
                                ? Mathf.Lerp(1f, m_ArrivalPop, q / 0.5f)
                                : m_ArrivalPop * (1f - (q - 0.5f) / 0.5f);
                            p.Rt.localEulerAngles = Vector3.zero;
                            p.Rt.localScale = new Vector3(pop, pop, 1f);
                            Color c = p.Img.color;
                            c.a = 1f - q * q;
                            p.Img.color = c;

                            if (q >= 1f)
                            {
                                p.Phase = Phase.Free;
                                p.Img.enabled = false;
                            }
                            break;
                        }
                }
            }

            if (!anyActive)
            {
                m_Running = false;
                m_OnAllArrived.Invoke();
            }
        }

        // ---------------- coordinate helpers ----------------

        private Vector2 GetTargetLocal()
        {
            switch (m_TargetMode)
            {
                case TargetMode.UIRect:
                    return UiToLocal(m_RuntimeUiTarget);
                case TargetMode.FollowWorld:
                    return m_RuntimeWorldTarget != null ? WorldToLocal(m_RuntimeWorldTarget.position)
                                                        : WorldToLocal(m_FixedWorldPos);
                default:
                    return WorldToLocal(m_FixedWorldPos);
            }
        }

        /// <summary>UI element center -> local point in this canvas rect.</summary>
        private Vector2 UiToLocal(RectTransform r)
        {
            Vector2 sp = RectTransformUtility.WorldToScreenPoint(m_CanvasCam, r.position);
            Vector2 lp;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(m_Rect, sp, m_CanvasCam, out lp);
            return lp;
        }

        /// <summary>World position -> local point in this canvas rect.</summary>
        private Vector2 WorldToLocal(Vector3 worldPos)
        {
            if (m_WorldCam == null)
            {
                m_WorldCam = Camera.main;
                if (m_WorldCam == null)
                {
                    Debug.LogWarning("[UIFlyToParticles] No main camera found; call SetWorldCamera().", this);
                    return Vector2.zero;
                }
            }
            Vector3 sp3 = m_WorldCam.WorldToScreenPoint(worldPos);
            Vector2 lp;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(m_Rect, new Vector2(sp3.x, sp3.y), m_CanvasCam, out lp);
            return lp;
        }

        private static Vector2 Bezier(Vector2 a, Vector2 c, Vector2 b, float t)
        {
            float u = 1f - t;
            return u * u * a + 2f * u * t * c + t * t * b;
        }

        private static float EaseInOutQuad(float t)
        {
            return t < 0.5f ? 2f * t * t : 1f - ((-2f * t + 2f) * (-2f * t + 2f)) / 2f;
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            t = Mathf.Clamp01(t);
            return 1f + c3 * (t - 1f) * (t - 1f) * (t - 1f) + c1 * (t - 1f) * (t - 1f);
        }

        /// <summary>Procedural golden dot used when m_Sprites is empty, so the script always works out of the box.</summary>
        private static Sprite GetFallbackSprite()
        {
            if (s_FallbackSprite != null) return s_FallbackSprite;
            const int S = 64;
            Texture2D tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Vector2 center = new Vector2(S * 0.5f - 0.5f, S * 0.5f - 0.5f);
            for (int y = 0; y < S; y++)
            {
                for (int x = 0; x < S; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center) / (S * 0.5f);
                    float a = Mathf.Clamp01((1f - d) / 0.15f);
                    float core = Mathf.Clamp01(1f - d * 0.55f);
                    tex.SetPixel(x, y, new Color(1f, 0.84f * core + 0.1f, 0.35f * core, a));
                }
            }
            tex.Apply();
            s_FallbackSprite = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f));
            return s_FallbackSprite;
        }
    }
}