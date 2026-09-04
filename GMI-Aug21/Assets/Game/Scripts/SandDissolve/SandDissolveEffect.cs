using System;
using UnityEngine;
using UnityEngine.UI;

namespace Oxtail.SpaceshipIncremental
{
    /// <summary>
    /// Progressive sand-style disintegration for SpriteRenderer or UI Image.
    /// Mobile friendly: no per-frame allocations, no expensive particle modules,
    /// grain table precomputed in Awake and binary search on emit.
    ///
    /// USAGE:
    ///     sand.Disintegrate();        // advance one step (StepAmount)
    ///     sand.Disintegrate(0.25f);   // advance by any amount
    ///     sand.ResetEffect();         // restore the image
    ///
    /// Grains spawn exactly where the sprite just opened up, fall, and vanish
    /// after GrainLifetime seconds. They never pile up.
    /// </summary>
    /// <summary>Which way the image comes apart.</summary>
    public enum DissolvePattern
    {
        /// <summary>Random blotches all over the image. No direction.</summary>
        Random = 0,

        /// <summary>Eaten from the top edge downwards.</summary>
        TopToBottom = 1,

        /// <summary>Eaten from the bottom edge upwards.</summary>
        BottomToTop = 2,

        /// <summary>Eaten from the left edge rightwards.</summary>
        LeftToRight = 3,

        /// <summary>Eaten from the right edge leftwards.</summary>
        RightToLeft = 4,

        /// <summary>Diagonal sweep starting at the top-left corner.</summary>
        TopLeftToBottomRight = 5,

        /// <summary>Diagonal sweep starting at the top-right corner.</summary>
        TopRightToBottomLeft = 6,

        /// <summary>Diagonal sweep starting at the bottom-left corner.</summary>
        BottomLeftToTopRight = 7,

        /// <summary>Diagonal sweep starting at the bottom-right corner.</summary>
        BottomRightToTopLeft = 8,

        /// <summary>Opens up from the center outwards.</summary>
        CenterOut = 9,

        /// <summary>Closes in from the edges towards the center.</summary>
        EdgesIn = 10,

        /// <summary>Free direction set through CustomDirection.</summary>
        Custom = 11
    }

    [AddComponentMenu("Effects/Sand Dissolve Effect")]
    [DisallowMultipleComponent]
    [ExecuteAlways]   // so Grayscale and the other shader settings preview in edit mode
    public class SandDissolveEffect : MonoBehaviour
    {
        // ==================================================================
        #region Serialized fields

        [Header("── Dissolve speed ──")]
        [Tooltip("Progress per second. 0.5 = takes 2s to fully dissolve. " +
                 "Set to 0 to apply every call instantly.")]
        [SerializeField, Min(0f)] private float m_DissolveSpeed = 0.5f;

        [Tooltip("How much a parameterless Disintegrate() / Reintegrate() call moves the dissolve.")]
        [SerializeField, Range(0.01f, 1f)] private float m_StepAmount = 0.1f;

        [Tooltip("Progress per second while REBUILDING. Set to 0 to reuse Dissolve Speed. " +
                 "A higher value makes the image snap back faster than it fell apart.")]
        [SerializeField, Min(0f)] private float m_ReintegrateSpeed = 0f;

        [Header("── Grain fall speed ──")]
        [Tooltip("Initial downward speed, in world units per second.")]
        [SerializeField, Min(0f)] private float m_FallSpeed = 1.0f;

        [Tooltip("Extra acceleration while falling. 0 = constant speed.")]
        [SerializeField, Range(0f, 2f)] private float m_Gravity = 0.15f;

        [Tooltip("Horizontal spread on release, so grains don't fall in straight columns.")]
        [SerializeField, Min(0f)] private float m_SidewaysDrift = 0.25f;

        [Tooltip("Constant lateral wind (units/s). Positive X blows right.")]
        [SerializeField] private Vector2 m_Wind = Vector2.zero;

        [Header("── Time until grains vanish ──")]
        [Tooltip("Seconds each grain lives before disappearing.")]
        [SerializeField, Min(0.05f)] private float m_GrainLifetime = 2f;

        [Tooltip("Random lifetime variation (±%). 0 = they all vanish at once.")]
        [SerializeField, Range(0f, 1f)] private float m_LifetimeVariation = 0.25f;

        [Tooltip("Final portion of the lifetime spent fading out. 0 = instant pop.")]
        [SerializeField, Range(0f, 1f)] private float m_FadeOutPortion = 0.25f;

        [Header("── Grayscale ──")]
        [Tooltip("Desaturate the sprite this effect is attached to.")]
        [SerializeField] private bool m_Grayscale = false;

        [Tooltip("Desaturation strength. 1 = fully gray, 0.5 = half desaturated.")]
        [SerializeField, Range(0f, 1f)] private float m_GrayscaleAmount = 1f;

        [Tooltip("Color multiplied over the gray result. Use it for sepia or cold tints.")]
        [SerializeField] private Color m_GrayscaleTint = Color.white;

        [Tooltip("Also desaturate the falling grains, so they match the sprite.")]
        [SerializeField] private bool m_GrayscaleAffectsGrains = true;

        [Header("── Shader appearance ──")]
        [Tooltip("Noise scale for the DIRECTIONAL patterns (everything except Random).\n" +
                 "Higher = finer grain along the cut line.\n" +
                 "Around 6 gives a sandy crumbling edge; 1-2 crumbles in big chunks.")]
        [SerializeField, Min(0.01f)] private float m_NoiseScale = 6f;

        [Tooltip("Noise scale used ONLY by the Random pattern, which wants big blotches\n" +
                 "instead of a fine edge. Kept separate so tuning the sweep line does not\n" +
                 "change how Random looks.")]
        [SerializeField, Min(0.01f)] private float m_RandomNoiseScale = 1.6f;

        [SerializeField, Range(0.001f, 0.5f)] private float m_EdgeWidth = 0.05f;
        [SerializeField, ColorUsage(true, true)] private Color m_EdgeColor = new Color(1f, 0.78f, 0.42f, 1f);
        [SerializeField, Range(0f, 8f)] private float m_EdgeEmission = 1.8f;

        [Tooltip("Which way the image comes apart.")]
        [SerializeField] private DissolvePattern m_Pattern = DissolvePattern.TopToBottom;

        [Tooltip("How clean the sweep line is.\n" +
                 "1 = perfectly straight edge, 0 = fully random (same as Random pattern).\n" +
                 "0.85 reads as a clear line with a ragged sandy edge.\n" +
                 "Think of (1 - this) as the width of the ragged band.\n" +
                 "Ignored when Pattern is Random.")]
        [SerializeField, Range(0f, 1f)] private float m_PatternStrength = 0.85f;

        [Tooltip("Only used when Pattern is Custom. Direction in UV space.")]
        [SerializeField] private Vector2 m_CustomDirection = new Vector2(0f, -1f);

        [Header("── Particles ──")]
        [SerializeField] private bool m_EmitParticles = true;

        [Tooltip("Leave empty to auto-create a child ParticleSystem.")]
        [SerializeField] private ParticleSystem m_SandParticles;

        [Tooltip("Grain texture. If null, a procedural dot is used (cheaper).")]
        [SerializeField] private Texture2D m_GrainTexture;

        [Tooltip("Grains emitted across a full dissolve.")]
        [SerializeField, Min(0)] private int m_ParticlesPerFullDissolve = 350;

        [Tooltip("Cap on grains emitted in a single frame. Lower it if you see hitches.")]
        [SerializeField, Min(1)] private int m_MaxParticlesPerBurst = 60;

        [SerializeField] private Vector2 m_GrainSizeRange = new Vector2(0.03f, 0.07f);

        [Tooltip("Grains take the color of the sprite pixel (needs Read/Write on the texture).")]
        [SerializeField] private bool m_GrainInheritsSpriteColor = true;

        [Tooltip("Grain color when not inherited from the sprite.")]
        [SerializeField] private Gradient m_GrainTint;

        [Header("── Performance (mobile) ──")]
        [Tooltip("Grain table resolution. 48 is plenty on mobile; 96 for large sprites.")]
        [SerializeField, Range(16, 128)] private int m_GrainTableResolution = 48;

        [Tooltip("Share ONE ParticleSystem across every object that enables this. " +
                 "Fewer draw calls when many enemies dissolve at once. " +
                 "Uses the fall settings of whichever instance creates it first.")]
        [SerializeField] private bool m_UseSharedParticleSystem = false;

        [Tooltip("Adds high frequency grain in the shader. Disable to save ALU on low-end.")]
        [SerializeField] private bool m_HighFrequencyGrain = true;

        [Header("── Events ──")]
        [SerializeField] private UnityEngine.Events.UnityEvent m_OnFullyDissolved;
        [SerializeField] private UnityEngine.Events.UnityEvent m_OnRestored;

        #endregion
        // ==================================================================
        #region Public properties

        /// <summary>Progress per second. 0.5 means a full dissolve takes 2 seconds.</summary>
        public float DissolveSpeed
        {
            get => m_DissolveSpeed;
            set => m_DissolveSpeed = Mathf.Max(0f, value);
        }

        /// <summary>How much a parameterless Disintegrate() / Reintegrate() call moves.</summary>
        public float StepAmount
        {
            get => m_StepAmount;
            set => m_StepAmount = Mathf.Clamp01(value);
        }

        /// <summary>
        /// Progress per second while rebuilding. Returns DissolveSpeed when left at 0,
        /// so by default the image reassembles at the same rate it fell apart.
        /// </summary>
        public float ReintegrateSpeed
        {
            get => m_ReintegrateSpeed > 0f ? m_ReintegrateSpeed : m_DissolveSpeed;
            set => m_ReintegrateSpeed = Mathf.Max(0f, value);
        }

        /// <summary>True while the image is currently rebuilding itself.</summary>
        public bool IsReintegrating => m_Dirty && m_Target < m_Current;

        /// <summary>True when the image is fully restored (progress back at 0).</summary>
        public bool IsFullyRestored => m_Current <= 1e-4f;

        /// <summary>Initial downward speed of the grains, in world units per second.</summary>
        public float FallSpeed
        {
            get => m_FallSpeed;
            set => m_FallSpeed = Mathf.Max(0f, value);
        }

        /// <summary>Extra downward acceleration applied to the grains.</summary>
        public float Gravity
        {
            get => m_Gravity;
            set { m_Gravity = Mathf.Clamp(value, 0f, 2f); ApplyParticleSettings(); }
        }

        /// <summary>Constant lateral wind applied while grains fall.</summary>
        public Vector2 Wind
        {
            get => m_Wind;
            set { m_Wind = value; ApplyParticleSettings(); }
        }

        /// <summary>Seconds each grain lives before vanishing.</summary>
        public float GrainLifetime
        {
            get => m_GrainLifetime;
            set { m_GrainLifetime = Mathf.Max(0.05f, value); ApplyParticleSettings(); }
        }

        /// <summary>Turns the grayscale effect on or off.</summary>
        public bool Grayscale
        {
            get => m_Grayscale;
            set { m_Grayscale = value; ApplyShaderSettings(); }
        }

        /// <summary>Desaturation strength, 0 to 1.</summary>
        public float GrayscaleAmount
        {
            get => m_GrayscaleAmount;
            set { m_GrayscaleAmount = Mathf.Clamp01(value); ApplyShaderSettings(); }
        }

        /// <summary>Color multiplied over the gray result (sepia, cold tints, etc).</summary>
        public Color GrayscaleTint
        {
            get => m_GrayscaleTint;
            set { m_GrayscaleTint = value; ApplyShaderSettings(); }
        }

        /// <summary>
        /// Noise scale for the directional patterns. Higher values give a finer edge.
        /// Rebuilds the grain table, since the noise field itself changes.
        /// </summary>
        public float NoiseScale
        {
            get => m_NoiseScale;
            set
            {
                m_NoiseScale = Mathf.Max(0.01f, value);
                ApplyShaderSettings();
                if (Application.isPlaying) BuildGrainTable();
            }
        }

        /// <summary>
        /// Noise scale used only by the Random pattern, which wants big blotches.
        /// Kept separate so tuning the sweep line does not change how Random looks.
        /// </summary>
        public float RandomNoiseScale
        {
            get => m_RandomNoiseScale;
            set
            {
                m_RandomNoiseScale = Mathf.Max(0.01f, value);
                ApplyShaderSettings();
                if (Application.isPlaying) BuildGrainTable();
            }
        }

        /// <summary>
        /// Which way the image comes apart. Changing this rebuilds the grain table,
        /// so grains keep spawning exactly where the sprite opens up.
        /// </summary>
        public DissolvePattern Pattern
        {
            get => m_Pattern;
            set
            {
                if (m_Pattern == value) return;
                m_Pattern = value;
                ApplyShaderSettings();
                if (Application.isPlaying) BuildGrainTable();
            }
        }

        /// <summary>
        /// How strongly the pattern overrides the random noise, 0 to 1.
        /// 0 is pure random, 1 is a hard sweep line.
        /// </summary>
        public float PatternStrength
        {
            get => m_PatternStrength;
            set
            {
                m_PatternStrength = Mathf.Clamp01(value);
                ApplyShaderSettings();
                if (Application.isPlaying) BuildGrainTable();
            }
        }

        /// <summary>Direction used when Pattern is Custom.</summary>
        public Vector2 CustomDirection
        {
            get => m_CustomDirection;
            set
            {
                m_CustomDirection = value;
                ApplyShaderSettings();
                if (Application.isPlaying && m_Pattern == DissolvePattern.Custom) BuildGrainTable();
            }
        }

        /// <summary>Whether grains are emitted at all.</summary>
        public bool EmitParticles
        {
            get => m_EmitParticles;
            set => m_EmitParticles = value;
        }

        /// <summary>Current dissolve progress. 0 = intact, 1 = fully gone.</summary>
        public float Progress => m_Current;

        /// <summary>True once the image is completely dissolved.</summary>
        public bool IsFullyDissolved => m_Current >= 1f - 1e-4f;

        /// <summary>Raised when progress reaches 1.</summary>
        public UnityEngine.Events.UnityEvent OnFullyDissolved => m_OnFullyDissolved;

        /// <summary>Raised by ResetEffect().</summary>
        public UnityEngine.Events.UnityEvent OnRestored => m_OnRestored;

        #endregion
        // ==================================================================
        #region Internal state

        private static readonly int k_IdDissolve        = Shader.PropertyToID("_Dissolve");
        private static readonly int k_IdNoiseScale      = Shader.PropertyToID("_NoiseScale");
        private static readonly int k_IdEdgeWidth       = Shader.PropertyToID("_EdgeWidth");
        private static readonly int k_IdEdgeColor       = Shader.PropertyToID("_EdgeColor");
        private static readonly int k_IdEdgeEmission    = Shader.PropertyToID("_EdgeEmission");
        private static readonly int k_IdDirX            = Shader.PropertyToID("_DirX");
        private static readonly int k_IdDirY            = Shader.PropertyToID("_DirY");
        private static readonly int k_IdDirStrength  = Shader.PropertyToID("_DirStrength");
        private static readonly int k_IdPatternMode  = Shader.PropertyToID("_PatternMode");
        private static readonly int k_IdNoiseTex        = Shader.PropertyToID("_NoiseTex");
        private static readonly int k_IdGrayscaleAmount = Shader.PropertyToID("_GrayscaleAmount");
        private static readonly int k_IdGrayscaleTint   = Shader.PropertyToID("_GrayscaleTint");

        private const int k_NoiseRes = 64;      // 4 KB of noise, cache friendly
        private const string k_GrayscaleKeyword = "_GRAYSCALE_ON";
        private const string k_GrainKeyword     = "_HFGRAIN_ON";

        /// <summary>One precomputed grain: where it is, when it detaches, what color it is.</summary>
        private struct Grain
        {
            public float Noise;     // noise value -> decides WHEN it detaches
            public Vector2 Uv;      // normalized position inside the sprite
            public Color32 Color;
        }

        /// <summary>Cached comparer so Rebuild() does not allocate.</summary>
        private sealed class GrainComparer : System.Collections.Generic.IComparer<Grain>
        {
            public int Compare(Grain a, Grain b) => a.Noise.CompareTo(b.Noise);
        }
        private static readonly GrainComparer s_GrainComparer = new GrainComparer();

        private SpriteRenderer m_SpriteRenderer;
        private Image m_Image;
        private Material m_Material;
        private MaterialPropertyBlock m_PropertyBlock;

        private static byte[] s_Noise;                  // shared across all instances
        private static Texture2D s_NoiseTexture;
        private static ParticleSystem s_SharedParticleSystem;

        private Grain[] m_Grains;                       // SORTED by Noise ascending
        private int m_GrainCount;

        private float m_Current;
        private float m_Target;
        private float m_LastEmitted;
        private bool m_Dirty;                           // is there anything left to animate?

        private ParticleSystem.EmitParams m_EmitParams;
        private ParticleSystem m_ActiveParticleSystem;  // own or shared
        private float m_SizeRef = 1f;
        private bool m_LoggedSetupError;                // avoids log spam from lazy retries

        #endregion
        // ==================================================================
        #region Unity lifecycle

        private void Awake()
        {
            EnsureInitialized();

            // Runtime-only work: the grain table and the particle system are useless
            // in edit mode and would spawn junk objects in the scene.
            if (!Application.isPlaying) return;

            if (m_GrainTint == null || m_GrainTint.colorKeys.Length == 0)
                m_GrainTint = CreateDefaultSandGradient();

            BuildGrainTable();      // the heavy work, done ONCE

            if (m_EmitParticles)
                m_ActiveParticleSystem = m_SandParticles != null ? m_SandParticles : AcquireParticleSystem();

            ApplyDissolve(0f);
        }

        private void OnEnable()
        {
            // Covers edit mode, domain reloads and prefab reopening, where Awake
            // may have run before the renderer was ready.
            EnsureInitialized();
            ApplyShaderSettings();
        }

        /// <summary>
        /// Creates the material instance if it does not exist yet. Safe to call
        /// repeatedly and from edit mode.
        /// </summary>
        private void EnsureInitialized()
        {
            if (m_SpriteRenderer == null) m_SpriteRenderer = GetComponent<SpriteRenderer>();
            if (m_Image == null)          m_Image          = GetComponent<Image>();
            if (m_PropertyBlock == null)  m_PropertyBlock  = new MaterialPropertyBlock();

            EnsureNoise();

            if (m_Material == null)
            {
                SetupMaterial();
                ApplyShaderSettings();
            }
        }

        private void OnValidate()
        {
            // Runs in edit mode too, so the Grayscale checkbox previews live.
            // Deferred because Unity forbids creating objects during OnValidate.
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null) return;   // component destroyed meanwhile
                EnsureInitialized();
                ApplyShaderSettings();
                ApplyParticleSettings();
            };
#else
            ApplyShaderSettings();
            ApplyParticleSettings();
#endif
        }

        private void OnDestroy()
        {
            // The material is our own instance, release it or it leaks.
            if (m_Material != null)
            {
                if (Application.isPlaying) Destroy(m_Material);
                else DestroyImmediate(m_Material);
            }
        }

        private void Update()
        {
            // [ExecuteAlways] also ticks in edit mode; never animate there.
            if (!Application.isPlaying || !m_Dirty) return;

            // Rebuilding can run at its own pace; falls back to DissolveSpeed when unset.
            bool rebuilding = m_Target < m_Current;
            float speed = rebuilding ? ReintegrateSpeed : m_DissolveSpeed;

            if (speed <= 0f)
            {
                StepTo(m_Target);
            }
            else
            {
                float next = Mathf.MoveTowards(m_Current, m_Target, speed * Time.deltaTime);
                StepTo(next);
            }

            if (Mathf.Approximately(m_Current, m_Target))
                m_Dirty = false;
        }

        #endregion
        // ==================================================================
        #region Public API

        /// <summary>Advances the dissolve by the configured StepAmount.</summary>
        public void Disintegrate() => Disintegrate(m_StepAmount);

        /// <summary>Advances the dissolve by the given amount (0-1). Accumulates and clamps at 1.</summary>
        public void Disintegrate(float amount) => SetProgress(m_Target + amount);

        /// <summary>
        /// Rebuilds the image by the configured StepAmount. Exact opposite of Disintegrate().
        /// </summary>
        public void Reintegrate() => Reintegrate(m_StepAmount);

        /// <summary>
        /// Rebuilds the image by the given amount (0-1). Exact opposite of Disintegrate(amount):
        /// it accumulates the same way and clamps at 0.
        ///
        ///     Disintegrate(0.25f);   // 0.00 -> 0.25
        ///     Disintegrate(0.25f);   // 0.25 -> 0.50
        ///     Reintegrate(0.25f);    // 0.50 -> 0.25
        ///     Reintegrate(0.25f);    // 0.25 -> 0.00, raises OnRestored
        /// </summary>
        public void Reintegrate(float amount) => SetProgress(m_Target - amount);

        /// <summary>Rebuilds the image completely, animated at ReintegrateSpeed.</summary>
        public void ReintegrateAll() => SetProgress(0f);

        /// <summary>Sets the target progress; it is reached at DissolveSpeed per second.</summary>
        public void SetProgress(float progress)
        {
            m_Target = Mathf.Clamp01(progress);
            m_Dirty = true;
        }

        /// <summary>Sets progress instantly, without animating or emitting grains.</summary>
        public void SetProgressImmediate(float progress)
        {
            m_Current = m_Target = m_LastEmitted = Mathf.Clamp01(progress);
            m_Dirty = false;
            ApplyDissolve(m_Current);
        }

        /// <summary>Restores the image to its intact state.</summary>
        public void ResetEffect()
        {
            SetProgressImmediate(0f);
            m_OnRestored?.Invoke();
        }

        /// <summary>
        /// Toggles grayscale with an explicit value, handy for UnityEvents and Animator.
        /// </summary>
        public void SetGrayscale(bool enabled)
        {
            m_Grayscale = enabled;
            ApplyShaderSettings();
        }

        /// <summary>
        /// Call this if you swap the sprite at runtime, to rebuild the grain table.
        /// This is the only expensive operation here; do not call it every frame.
        /// </summary>
        public void Rebuild()
        {
            BuildGrainTable();
            ApplyShaderSettings();
        }

        private void StepTo(float value)
        {
            float previous = m_Current;
            m_Current = Mathf.Clamp01(value);
            ApplyDissolve(m_Current);

            if (m_EmitParticles && m_ActiveParticleSystem != null && m_Current > m_LastEmitted + 1e-4f)
            {
                EmitBand(m_LastEmitted, m_Current);
                m_LastEmitted = m_Current;
            }
            else if (m_Current < m_LastEmitted)
            {
                m_LastEmitted = m_Current;
            }

            if (previous < 1f - 1e-4f && IsFullyDissolved)
                m_OnFullyDissolved?.Invoke();

            // Symmetric event: raised when the image finishes rebuilding, not just
            // from ResetEffect(). Guarded by 'previous' so it fires once per arrival.
            if (previous > 1e-4f && IsFullyRestored)
                m_OnRestored?.Invoke();
        }

        #endregion
        // ==================================================================
        #region Material

        private void SetupMaterial()
        {
            // Called lazily, so make sure the renderer refs exist first.
            if (m_SpriteRenderer == null) m_SpriteRenderer = GetComponent<SpriteRenderer>();
            if (m_Image == null)          m_Image          = GetComponent<Image>();

            if (m_SpriteRenderer == null && m_Image == null)
            {
                if (!m_LoggedSetupError)
                {
                    Debug.LogError("[SandDissolve] A SpriteRenderer or an Image is required.", this);
                    m_LoggedSetupError = true;
                }
                return;
            }

            Shader shader = Shader.Find("SandDissolve/SpriteDissolve");
            if (shader == null)
            {
                if (!m_LoggedSetupError)
                {
                    Debug.LogError("[SandDissolve] Shader 'SandDissolve/SpriteDissolve' not found. " +
                                   "Add it to Project Settings > Graphics > Always Included Shaders for builds.", this);
                    m_LoggedSetupError = true;
                }
                return;
            }

            EnsureNoise();

            m_Material = new Material(shader)
            {
                name = "SandDissolve (instance)",
                hideFlags = HideFlags.HideAndDontSave   // never saved into the scene
            };
            if (s_NoiseTexture != null) m_Material.SetTexture(k_IdNoiseTex, s_NoiseTexture);

            // sharedMaterial in edit mode avoids leaking a material instance per frame.
            if (m_SpriteRenderer != null) m_SpriteRenderer.sharedMaterial = m_Material;
            else                          m_Image.material = m_Material;
        }

        private void ApplyShaderSettings()
        {
            // Lazy-create so setting properties before Awake still works.
            if (m_Material == null) SetupMaterial();
            if (m_Material == null) return;   // shader missing, already logged

            m_Material.SetFloat(k_IdNoiseScale,   GetActiveNoiseScale());
            m_Material.SetFloat(k_IdEdgeWidth,    m_EdgeWidth);
            m_Material.SetColor(k_IdEdgeColor,    m_EdgeColor);
            m_Material.SetFloat(k_IdEdgeEmission, m_EdgeEmission);

            Vector2 direction = GetPatternDirection();
            m_Material.SetFloat(k_IdDirX,        direction.x);
            m_Material.SetFloat(k_IdDirY,        direction.y);
            m_Material.SetFloat(k_IdDirStrength, HasPatternBias ? m_PatternStrength : 0f);
            m_Material.SetFloat(k_IdPatternMode, GetPatternMode());

            // Grayscale is a shader variant: zero cost when disabled.
            m_Material.SetFloat(k_IdGrayscaleAmount, m_GrayscaleAmount);
            m_Material.SetColor(k_IdGrayscaleTint,   m_GrayscaleTint);
            if (m_Grayscale) m_Material.EnableKeyword(k_GrayscaleKeyword);
            else             m_Material.DisableKeyword(k_GrayscaleKeyword);

            if (m_HighFrequencyGrain) m_Material.EnableKeyword(k_GrainKeyword);
            else                      m_Material.DisableKeyword(k_GrainKeyword);
        }

        private void ApplyDissolve(float value)
        {
            // MaterialPropertyBlock keeps batching intact and avoids extra material instances.
            if (m_SpriteRenderer != null)
            {
                m_SpriteRenderer.GetPropertyBlock(m_PropertyBlock);
                m_PropertyBlock.SetFloat(k_IdDissolve, value);
                m_SpriteRenderer.SetPropertyBlock(m_PropertyBlock);
            }
            else if (m_Material != null)
            {
                m_Material.SetFloat(k_IdDissolve, value);
            }
        }

        #endregion
        // ==================================================================
        #region Noise (shared, generated once per session)

        private static void EnsureNoise()
        {
            if (s_Noise != null && s_NoiseTexture != null) return;

            int count = k_NoiseRes * k_NoiseRes;
            var accumulator = new float[count];
            var random = new System.Random(1337);
            float amplitude = 1f, total = 0f;

            int[] octaves = { 4, 8, 16, 32 };
            for (int i = 0; i < octaves.Length; i++)
            {
                AddValueNoise(accumulator, octaves[i], k_NoiseRes, random, amplitude);
                total += amplitude;
                amplitude *= 0.55f;
            }

            float min = float.MaxValue, max = float.MinValue;
            for (int i = 0; i < count; i++)
            {
                accumulator[i] /= total;
                if (accumulator[i] < min) min = accumulator[i];
                if (accumulator[i] > max) max = accumulator[i];
            }
            float inverseRange = 1f / Mathf.Max(1e-5f, max - min);

            s_Noise = new byte[count];
            var pixels = new Color32[count];
            for (int i = 0; i < count; i++)
            {
                byte value = (byte)Mathf.RoundToInt(Mathf.Clamp01((accumulator[i] - min) * inverseRange) * 255f);
                s_Noise[i] = value;
                pixels[i] = new Color32(value, value, value, 255);
            }

            s_NoiseTexture = new Texture2D(k_NoiseRes, k_NoiseRes, TextureFormat.R8, false, true)
            {
                name = "SandNoise (runtime)",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            s_NoiseTexture.SetPixels32(pixels);
            s_NoiseTexture.Apply(false, true);   // makeNoLongerReadable -> frees the CPU copy
        }

        private static void AddValueNoise(float[] destination, int resolution, int size,
                                          System.Random random, float amplitude)
        {
            int gridWidth = resolution + 1;
            var grid = new float[gridWidth * gridWidth];
            for (int y = 0; y < resolution; y++)
                for (int x = 0; x < resolution; x++)
                    grid[y * gridWidth + x] = (float)random.NextDouble();

            for (int i = 0; i <= resolution; i++)      // wrap -> tileable
            {
                grid[i * gridWidth + resolution] = grid[i * gridWidth];
                grid[resolution * gridWidth + i] = grid[i];
            }

            float scale = (float)resolution / size;
            for (int y = 0; y < size; y++)
            {
                float fy = y * scale; int y0 = (int)fy; float ty = fy - y0;
                ty = ty * ty * (3f - 2f * ty);
                int row0 = y0 * gridWidth, row1 = (y0 + 1) * gridWidth;
                for (int x = 0; x < size; x++)
                {
                    float fx = x * scale; int x0 = (int)fx; float tx = fx - x0;
                    tx = tx * tx * (3f - 2f * tx);
                    float a = grid[row0 + x0], b = grid[row0 + x0 + 1];
                    float c = grid[row1 + x0], d = grid[row1 + x0 + 1];
                    destination[y * size + x] += Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), ty) * amplitude;
                }
            }
        }

        /// <summary>
        /// Turns the chosen pattern into a UV-space direction.
        /// Note the Y axis: UV y=0 is the BOTTOM of the sprite, so "top to bottom"
        /// needs a direction of (0,-1) to make the top dissolve first.
        /// Radial patterns return zero here and are handled by PatternMode instead.
        /// </summary>
        private Vector2 GetPatternDirection()
        {
            const float d = 0.70710678f;   // 1/sqrt(2), pre-normalized diagonals
            switch (m_Pattern)
            {
                case DissolvePattern.TopToBottom:          return new Vector2( 0f, -1f);
                case DissolvePattern.BottomToTop:          return new Vector2( 0f,  1f);
                case DissolvePattern.LeftToRight:          return new Vector2( 1f,  0f);
                case DissolvePattern.RightToLeft:          return new Vector2(-1f,  0f);
                case DissolvePattern.TopLeftToBottomRight: return new Vector2( d, -d);
                case DissolvePattern.TopRightToBottomLeft: return new Vector2(-d, -d);
                case DissolvePattern.BottomLeftToTopRight: return new Vector2( d,  d);
                case DissolvePattern.BottomRightToTopLeft: return new Vector2(-d,  d);
                case DissolvePattern.Custom:
                    return m_CustomDirection.sqrMagnitude > 1e-6f
                         ? m_CustomDirection.normalized
                         : Vector2.zero;
                default:                                   return Vector2.zero;   // Random, radial
            }
        }

        /// <summary>
        /// Random wants big blotches, the directional patterns want a fine edge,
        /// so each gets its own noise scale.
        /// </summary>
        private float GetActiveNoiseScale() =>
            m_Pattern == DissolvePattern.Random ? m_RandomNoiseScale : m_NoiseScale;

        /// <summary>0 = directional/none, 1 = center out, 2 = edges in.</summary>
        private float GetPatternMode()
        {
            if (m_Pattern == DissolvePattern.CenterOut) return 1f;
            if (m_Pattern == DissolvePattern.EdgesIn)   return 2f;
            return 0f;
        }

        /// <summary>True when the pattern actually biases the noise.</summary>
        private bool HasPatternBias =>
            m_PatternStrength > 0f &&
            m_Pattern != DissolvePattern.Random &&
            (GetPatternDirection().sqrMagnitude > 1e-6f || GetPatternMode() > 0f);

        /// <summary>CPU replica of the noise value the fragment shader sees.</summary>
        private float SampleNoise(Vector2 uv)
        {
            float scale = GetActiveNoiseScale();
            float nx = uv.x * scale, ny = uv.y * scale;
            int x = (int)(Mathf.Repeat(nx, 1f) * k_NoiseRes); if (x >= k_NoiseRes) x = k_NoiseRes - 1;
            int y = (int)(Mathf.Repeat(ny, 1f) * k_NoiseRes); if (y >= k_NoiseRes) y = k_NoiseRes - 1;
            float value = s_Noise[y * k_NoiseRes + x] * (1f / 255f);

            if (!HasPatternBias) return value;

            // Must mirror the shader exactly, or grains spawn where the sprite is
            // still solid. Keep both implementations in sync.
            float projection;
            float mode = GetPatternMode();

            if (mode > 0f)
            {
                // Radial: distance from center, normalized so corners reach ~1.
                float dx = uv.x - 0.5f, dy = uv.y - 0.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy) * 1.41421356f;
                // Low projection dissolves first: CenterOut wants the center to go
                // first, so it uses dist directly; EdgesIn inverts it.
                projection = (mode > 1.5f) ? 1f - dist : dist;   // EdgesIn : CenterOut
            }
            else
            {
                Vector2 direction = GetPatternDirection();
                projection = (uv.x - 0.5f) * direction.x + (uv.y - 0.5f) * direction.y + 0.5f;
            }

            // Cross-fade around the 0.5 midpoint: the gradient IS the mask and the
            // noise only jitters the cut line. Exact at both ends:
            //   strength 1 -> projection      (perfectly straight edge)
            //   strength 0 -> value           (pure random, same as Random)
            float s = m_PatternStrength;
            return Mathf.Clamp01(0.5f + (projection - 0.5f) * s + (value - 0.5f) * (1f - s));
        }

        #endregion
        // ==================================================================
        #region Grain table (precomputed ONCE)

        /// <summary>
        /// Walks the sprite a single time, stores every opaque point with its noise value
        /// and color, then sorts by noise. Emitting becomes a binary search after that.
        /// </summary>
        private void BuildGrainTable()
        {
            int resolution = Mathf.Clamp(m_GrainTableResolution, 16, 128);
            Sprite sprite = m_SpriteRenderer != null ? m_SpriteRenderer.sprite
                                                     : (m_Image != null ? m_Image.sprite : null);

            Color32[] pixels = null;
            int textureWidth = 0, textureHeight = 0;
            Rect uvRect = new Rect(0, 0, 1, 1);

            if (sprite != null && sprite.texture != null)
            {
                var texture = sprite.texture;
                textureWidth = texture.width;
                textureHeight = texture.height;
                Rect rect = sprite.textureRect;
                uvRect = new Rect(rect.x / textureWidth, rect.y / textureHeight,
                                  rect.width / textureWidth, rect.height / textureHeight);
                try { pixels = texture.GetPixels32(); }
                catch (Exception)
                {
                    pixels = null;
                    if (m_GrainInheritsSpriteColor)
                        Debug.LogWarning($"[SandDissolve] '{texture.name}' has no Read/Write Enabled: " +
                                         "grains will use the 'Grain Tint' gradient instead.", this);
                }
            }

            if (m_Grains == null || m_Grains.Length < resolution * resolution)
                m_Grains = new Grain[resolution * resolution];
            m_GrainCount = 0;

            for (int y = 0; y < resolution; y++)
            {
                float v = (y + 0.5f) / resolution;
                for (int x = 0; x < resolution; x++)
                {
                    float u = (x + 0.5f) / resolution;
                    var uv = new Vector2(u, v);

                    Color32 color;
                    if (pixels != null)
                    {
                        int px = (int)((uvRect.x + u * uvRect.width)  * textureWidth);
                        int py = (int)((uvRect.y + v * uvRect.height) * textureHeight);
                        px = Mathf.Clamp(px, 0, textureWidth - 1);
                        py = Mathf.Clamp(py, 0, textureHeight - 1);
                        color = pixels[py * textureWidth + px];
                        if (color.a < 64) continue;             // transparent area, no sand here
                        if (!m_GrainInheritsSpriteColor)
                            color = m_GrainTint.Evaluate(u * 0.37f + v * 0.63f);
                    }
                    else
                    {
                        color = m_GrainTint.Evaluate((u + v) * 0.5f);
                    }

                    // Stored at full color. Grayscale is applied on emit instead, so
                    // toggling it at runtime affects grains without rebuilding the table.
                    color.a = 255;

                    m_Grains[m_GrainCount++] = new Grain
                    {
                        Noise = SampleNoise(uv),
                        Uv = uv,
                        Color = color
                    };
                }
            }

            // Sort by noise so the [cutFrom, cutTo] band is a contiguous range.
            Array.Sort(m_Grains, 0, m_GrainCount, s_GrainComparer);

            // 'pixels' goes out of scope here, so the GC reclaims the texture copy
            // instead of keeping it alive for the whole session.
        }

        /// <summary>Same luminance weights the shader uses, so CPU and GPU agree.</summary>
        private Color32 ApplyGrayscale(Color32 source)
        {
            float r = source.r / 255f, g = source.g / 255f, b = source.b / 255f;
            float luma = r * 0.299f + g * 0.587f + b * 0.114f;

            float grayR = luma * m_GrayscaleTint.r;
            float grayG = luma * m_GrayscaleTint.g;
            float grayB = luma * m_GrayscaleTint.b;

            r = Mathf.Lerp(r, grayR, m_GrayscaleAmount);
            g = Mathf.Lerp(g, grayG, m_GrayscaleAmount);
            b = Mathf.Lerp(b, grayB, m_GrayscaleAmount);

            return new Color32((byte)(Mathf.Clamp01(r) * 255f),
                               (byte)(Mathf.Clamp01(g) * 255f),
                               (byte)(Mathf.Clamp01(b) * 255f),
                               source.a);
        }

        /// <summary>First index whose noise is >= value. O(log n).</summary>
        private int LowerBound(float value)
        {
            int low = 0, high = m_GrainCount;
            while (low < high)
            {
                int mid = (low + high) >> 1;
                if (m_Grains[mid].Noise < value) low = mid + 1;
                else high = mid;
            }
            return low;
        }

        #endregion
        // ==================================================================
        #region Emission

        private void EmitBand(float from, float to)
        {
            if (m_GrainCount == 0) return;

            float edge = m_EdgeWidth;
            float cutFrom = from * (1f + edge * 2f) - edge;
            float cutTo   = to   * (1f + edge * 2f) - edge;

            int low = LowerBound(cutFrom);
            int high = LowerBound(cutTo);
            int available = high - low;
            if (available <= 0) return;

            int wanted = Mathf.RoundToInt(m_ParticlesPerFullDissolve * (to - from));
            wanted = Mathf.Clamp(wanted, 1, m_MaxParticlesPerBurst);
            if (wanted > available) wanted = available;

            Bounds bounds = GetWorldBounds();
            m_SizeRef = Mathf.Max(0.001f, Mathf.Max(bounds.size.x, bounds.size.y));
            float z = transform.position.z - 0.01f;

            // Strided sampling: covers the whole band without shuffling or allocating.
            float stride = (float)available / wanted;
            float cursor = UnityEngine.Random.value * stride;

            // Checked once per burst, not per grain.
            bool applyGray = m_Grayscale && m_GrayscaleAffectsGrains && m_GrayscaleAmount > 0.001f;

            for (int i = 0; i < wanted; i++)
            {
                int index = low + (int)cursor;
                if (index >= high) index = high - 1;
                cursor += stride;

                ref Grain grain = ref m_Grains[index];

                var position = new Vector3(
                    Mathf.Lerp(bounds.min.x, bounds.max.x, grain.Uv.x),
                    Mathf.Lerp(bounds.min.y, bounds.max.y, grain.Uv.y),
                    z);

                float drift = (UnityEngine.Random.value - 0.5f) * 2f * m_SidewaysDrift;
                float lifetime = m_GrainLifetime *
                                 (1f + (UnityEngine.Random.value - 0.5f) * 2f * m_LifetimeVariation);

                m_EmitParams.position      = position;
                m_EmitParams.velocity      = new Vector3(drift + m_Wind.x, -m_FallSpeed + m_Wind.y, 0f);
                m_EmitParams.startLifetime = lifetime;
                m_EmitParams.startSize     = UnityEngine.Random.Range(m_GrainSizeRange.x, m_GrainSizeRange.y) * m_SizeRef;
                m_EmitParams.startColor    = applyGray ? ApplyGrayscale(grain.Color) : grain.Color;
                m_EmitParams.applyShapeToPosition = false;

                m_ActiveParticleSystem.Emit(m_EmitParams, 1);
            }
        }

        private Bounds GetWorldBounds()
        {
            if (m_SpriteRenderer != null) return m_SpriteRenderer.bounds;
            if (m_Image != null)
            {
                var rectTransform = m_Image.rectTransform;
                Vector3 center = rectTransform.position;
                Vector2 size = Vector2.Scale(rectTransform.rect.size, rectTransform.lossyScale);
                Vector2 pivot = rectTransform.pivot;
                var min = new Vector3(center.x - size.x * pivot.x, center.y - size.y * pivot.y, center.z);
                var max = new Vector3(min.x + size.x, min.y + size.y, center.z);
                var bounds = new Bounds();
                bounds.SetMinMax(min, max);
                return bounds;
            }
            return new Bounds(transform.position, Vector3.one);
        }

        #endregion
        // ==================================================================
        #region ParticleSystem

        private ParticleSystem AcquireParticleSystem()
        {
            if (m_UseSharedParticleSystem)
            {
                if (s_SharedParticleSystem == null)
                {
                    var host = new GameObject("~SandGrains (shared)");
                    DontDestroyOnLoad(host);
                    s_SharedParticleSystem = BuildParticleSystem(host);
                }
                return s_SharedParticleSystem;
            }

            var go = new GameObject("SandParticles");
            go.transform.SetParent(transform, false);
            m_SandParticles = BuildParticleSystem(go);
            return m_SandParticles;
        }

        private ParticleSystem BuildParticleSystem(GameObject host)
        {
            var particleSystem = host.GetComponent<ParticleSystem>();
            if (particleSystem == null) particleSystem = host.AddComponent<ParticleSystem>();
            particleSystem.Stop();

            // Live particle budget: emission rate x lifetime, with headroom.
            int budget = Mathf.Clamp(
                Mathf.CeilToInt(m_ParticlesPerFullDissolve * Mathf.Max(1f, m_GrainLifetime)), 64, 2000);

            var main = particleSystem.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startSpeed      = 0f;          // velocity comes from EmitParams
            main.startLifetime   = m_GrainLifetime;
            main.startSize       = 0.05f;
            main.startRotation   = 0f;          // no rotation, saves vertex work
            main.playOnAwake     = false;
            main.maxParticles    = budget;
            main.gravityModifier = m_Gravity;
            main.cullingMode     = ParticleSystemCullingMode.Automatic;   // pauses off-screen

            // Everything else off: on mobile each active module costs per particle.
            // NOTE: particle modules are structs returned BY VALUE, so they must be
            // copied into a local before assigning (otherwise: error CS1612).
            var emission = particleSystem.emission;
            emission.enabled = false;                       // we emit manually

            var shape = particleSystem.shape;
            shape.enabled = false;

            var sizeOverLifetime = particleSystem.sizeOverLifetime;
            sizeOverLifetime.enabled = false;

            var rotationOverLifetime = particleSystem.rotationOverLifetime;
            rotationOverLifetime.enabled = false;

            var noiseModule = particleSystem.noise;
            noiseModule.enabled = false;                    // costly, SidewaysDrift replaces it

            var limitVelocity = particleSystem.limitVelocityOverLifetime;
            limitVelocity.enabled = false;

            var collisionModule = particleSystem.collision;
            collisionModule.enabled = false;                // grains never pile up

            var triggerModule = particleSystem.trigger;
            triggerModule.enabled = false;

            var subEmitters = particleSystem.subEmitters;
            subEmitters.enabled = false;

            var trails = particleSystem.trails;
            trails.enabled = false;

            var lights = particleSystem.lights;
            lights.enabled = false;

            var textureSheetAnimation = particleSystem.textureSheetAnimation;
            textureSheetAnimation.enabled = false;

            // Wind: one cheap module, and only when it is actually used.
            var velocityOverLifetime = particleSystem.velocityOverLifetime;
            if (m_Wind.sqrMagnitude > 1e-6f)
            {
                velocityOverLifetime.enabled = true;
                velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
                velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(m_Wind.x);
                velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(m_Wind.y);
            }
            else velocityOverLifetime.enabled = false;

            // Fade at the end so grains do not pop out of existence.
            var colorOverLifetime = particleSystem.colorOverLifetime;
            if (m_FadeOutPortion > 0.001f)
            {
                colorOverLifetime.enabled = true;
                var gradient = new Gradient();
                float keep = Mathf.Clamp01(1f - m_FadeOutPortion);
                gradient.SetKeys(
                    new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                    new[]
                    {
                        new GradientAlphaKey(1f, 0f),
                        new GradientAlphaKey(1f, keep),
                        new GradientAlphaKey(0f, 1f)
                    });
                colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);
            }
            else colorOverLifetime.enabled = false;

            var renderer = host.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode        = ParticleSystemRenderMode.Billboard;
            renderer.alignment         = ParticleSystemRenderSpace.View;
            renderer.sortMode          = ParticleSystemSortMode.None;   // big saving, no depth sorting
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows    = false;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            renderer.allowRoll         = false;
            renderer.maxParticleSize   = 0.1f;

            Shader grainShader = Shader.Find("SandDissolve/GrainParticle")
                              ?? Shader.Find("Mobile/Particles/Alpha Blended")
                              ?? Shader.Find("Sprites/Default");
            var grainMaterial = new Material(grainShader) { name = "SandGrain (runtime)" };
            if (m_GrainTexture != null)
            {
                grainMaterial.mainTexture = m_GrainTexture;
                grainMaterial.DisableKeyword("_SOFTDOT_ON"); // Aseguramos que se apague
            }
            else
            {
                grainMaterial.EnableKeyword("_SOFTDOT_ON");  // Forzamos la bolita difuminada
            }
            grainMaterial.renderQueue = 3000;
            renderer.sharedMaterial = grainMaterial;

            if (m_SpriteRenderer != null)
            {
                renderer.sortingLayerID = m_SpriteRenderer.sortingLayerID;
                renderer.sortingOrder   = m_SpriteRenderer.sortingOrder + 1;
            }

            particleSystem.Play();
            return particleSystem;
        }

        /// <summary>Pushes fall/wind/lifetime to the ParticleSystem. Allows live tweaking.</summary>
        public void ApplyParticleSettings()
        {
            if (m_ActiveParticleSystem == null) return;

            var main = m_ActiveParticleSystem.main;
            main.gravityModifier = m_Gravity;
            main.startLifetime   = m_GrainLifetime;

            var velocityOverLifetime = m_ActiveParticleSystem.velocityOverLifetime;
            if (m_Wind.sqrMagnitude > 1e-6f)
            {
                velocityOverLifetime.enabled = true;
                velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
                velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(m_Wind.x);
                velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(m_Wind.y);
            }
            else velocityOverLifetime.enabled = false;
        }

        private static Gradient CreateDefaultSandGradient()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.93f, 0.82f, 0.60f), 0f),
                    new GradientColorKey(new Color(0.80f, 0.65f, 0.42f), 0.5f),
                    new GradientColorKey(new Color(0.62f, 0.48f, 0.30f), 1f)
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return gradient;
        }

        #endregion
    }
}
