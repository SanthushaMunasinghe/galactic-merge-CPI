using System;
using UnityEngine;
using UnityEngine.UI;

namespace Oxtail.SpaceshipIncremental
{
    /// <summary>
    /// Hourglass-style disintegration.
    ///
    /// Grains fall from the TOP of the image downwards. Each grain carries the color of
    /// the cell it is going to erase. The moment a grain reaches its target cell it
    /// disappears and that cell is erased instantly.
    ///
    /// So the order is the opposite of a normal dissolve: the sand travels FIRST and the
    /// image is destroyed on impact, not before.
    ///
    ///     hourglass.Pour();          // pour one step (PourAmount)
    ///     hourglass.Pour(0.25f);     // pour a specific fraction
    ///     hourglass.PourAll();       // pour the whole image
    ///     hourglass.ResetEffect();   // restore
    ///
    /// Mobile notes: the mask is a tiny R8 texture (32x32 by default = 1 KB) updated with
    /// SetPixels32 only on the frames where a grain actually lands. Grains are simulated
    /// in a plain struct array with no per-frame allocations.
    /// </summary>
    /// <summary>Which rows of the image come apart first.</summary>
    public enum PourOrder
    {
        /// <summary>
        /// Crumbles from the bottom edge upwards. The hole eats its way up while the
        /// grains fall away from it.
        /// </summary>
        BottomFirst = 0,

        /// <summary>Drains from the top down.</summary>
        TopFirst = 1,

        /// <summary>No row ordering: cells go in random order.</summary>
        Random = 2
    }

    /// <summary>Where the falling grains are born.</summary>
    public enum GrainSpawnMode
    {
        /// <summary>
        /// Just above their own cell, inside the sprite. The sand looks like it peels
        /// off from within the image. Use this for the two-layer setup.
        /// </summary>
        InsideImage = 0,

        /// <summary>Outside the top edge, raining down onto the image.</summary>
        AboveSprite = 1
    }

    [AddComponentMenu("Effects/Sand Hourglass Effect")]
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public class SandHourglassEffect : MonoBehaviour
    {
        // ==================================================================
        #region Serialized fields

        [Header("── Pour ──")]
        [Tooltip("Fraction of the image poured by a parameterless Pour() call.")]
        [SerializeField, Range(0.01f, 1f)] private float m_PourAmount = 0.25f;

        [Tooltip("Seconds it takes to release the grains of a full pour.\n" +
                 "Lower = the sand pours out faster.")]
        [SerializeField, Min(0.01f)] private float m_PourDuration = 1.2f;

        [Tooltip("Cells released per second. 0 = derive it from Pour Duration.")]
        [SerializeField, Min(0f)] private float m_ReleaseRate = 0f;

        [Header("── Fall ──")]
        [Tooltip("Fall speed in world units per second.")]
        [SerializeField, Min(0.01f)] private float m_FallSpeed = 3f;

        [Tooltip("Extra downward acceleration. 0 = constant speed.")]
        [SerializeField, Min(0f)] private float m_Gravity = 4f;

        [Tooltip("Horizontal drift while falling, so the stream is not perfectly vertical.")]
        [SerializeField, Min(0f)] private float m_SidewaysDrift = 0.15f;

        [Tooltip("Where grains are born.\n" +
                 "InsideImage: right above their own cell, so the sand detaches from " +
                 "within the sprite. This is the two-layer look.\n" +
                 "AboveSprite: outside the top edge, raining down onto the image.")]
        [SerializeField] private GrainSpawnMode m_SpawnMode = GrainSpawnMode.InsideImage;

        [Tooltip("How far above its own cell a grain is born, as a fraction of the sprite " +
                 "height. Small values (0.05-0.2) read as the piece peeling off in place.\n" +
                 "In InsideImage mode the spawn point is clamped to stay inside the sprite.")]
        [SerializeField, Range(0.01f, 2f)] private float m_SpawnHeight = 0.12f;

        [Tooltip("Random spread on the spawn height, so grains do not fall in lockstep.")]
        [SerializeField, Range(0f, 1f)] private float m_SpawnHeightJitter = 0.35f;

        [Header("── Grid ──")]
        [Tooltip("Mask resolution. Each cell is one grain. 32 = 1024 cells, plenty on mobile.")]
        [SerializeField, Range(8, 64)] private int m_GridResolution = 32;

        [Tooltip("Softens the straight cell edges with the sand noise. 0 = visible squares.")]
        [SerializeField, Range(0f, 1f)] private float m_MaskNoise = 0.35f;

        [Tooltip("Noise scale used to break up the cell edges.")]
        [SerializeField, Min(0.01f)] private float m_NoiseScale = 6f;

        [Header("── Grain color source ──")]
        [Tooltip("Sprite the grains take their color from.\n\n" +
                 "THE TWO-LAYER SETUP: put this component on the GRAYSCALE sprite and drag " +
                 "the COLOR sprite here. Grains then fall in full color while the gray " +
                 "layer is erased, revealing the color one underneath.\n\n" +
                 "Leave it empty to sample the sprite this component is attached to.")]
        [SerializeField] private SpriteRenderer m_ColorSource;

        [Tooltip("Use this instead of Color Source if the color layer is a UI Image.")]
        [SerializeField] private Image m_ColorSourceImage;

        [Header("── Grain look ──")]
        [SerializeField] private Vector2 m_GrainSizeRange = new Vector2(0.035f, 0.07f);

        [Tooltip("Grain texture. Null = cheap procedural dot.")]
        [SerializeField] private Texture2D m_GrainTexture;

        [Tooltip("Multiplied over the color sampled from the sprite.")]
        [SerializeField] private Color m_GrainTint = Color.white;

        [Tooltip("Which rows come apart first.\n" +
                 "BottomFirst: the image crumbles from its bottom edge upwards, so the " +
                 "hole eats its way up while the sand falls away. Default.\n" +
                 "TopFirst: drains from the top down.\n" +
                 "Random: no row ordering at all.")]
        [SerializeField] private PourOrder m_PourOrder = PourOrder.BottomFirst;

        [Header("── Edge glow ──")]
        [SerializeField, Range(0.001f, 0.5f)] private float m_EdgeWidth = 0.05f;
        [SerializeField, ColorUsage(true, true)] private Color m_EdgeColor = new Color(1f, 0.78f, 0.42f, 1f);
        [SerializeField, Range(0f, 8f)] private float m_EdgeEmission = 1.8f;

        [Header("── Grayscale ──")]
        [SerializeField] private bool m_Grayscale = false;
        [SerializeField, Range(0f, 1f)] private float m_GrayscaleAmount = 1f;
        [SerializeField] private Color m_GrayscaleTint = Color.white;
        [SerializeField] private bool m_GrayscaleAffectsGrains = true;

        [Header("── Performance ──")]
        [Tooltip("Hard cap on grains alive at once.")]
        [SerializeField, Min(16)] private int m_MaxLiveGrains = 512;

        [Header("── Events ──")]
        [SerializeField] private UnityEngine.Events.UnityEvent m_OnPourComplete;
        [SerializeField] private UnityEngine.Events.UnityEvent m_OnRestored;

        #endregion
        // ==================================================================
        #region Public API surface

        /// <summary>Fraction poured by a parameterless Pour() call.</summary>
        public float PourAmount
        {
            get => m_PourAmount;
            set => m_PourAmount = Mathf.Clamp01(value);
        }

        /// <summary>Seconds to release the grains of a full pour.</summary>
        public float PourDuration
        {
            get => m_PourDuration;
            set => m_PourDuration = Mathf.Max(0.01f, value);
        }

        /// <summary>Fall speed in world units per second.</summary>
        public float FallSpeed
        {
            get => m_FallSpeed;
            set => m_FallSpeed = Mathf.Max(0.01f, value);
        }

        /// <summary>Fraction of the image already erased, 0 to 1.</summary>
        public float Progress => m_TotalCells == 0 ? 0f : (float)m_ErasedCells / m_TotalCells;

        /// <summary>True once every cell has been erased.</summary>
        public bool IsFullyDissolved => m_TotalCells > 0 && m_ErasedCells >= m_TotalCells;

        /// <summary>True while grains are still falling or queued.</summary>
        public bool IsPouring => m_LiveGrains > 0 || m_QueueHead < m_QueueCount;

        /// <summary>Grayscale toggle.</summary>
        public bool Grayscale
        {
            get => m_Grayscale;
            set { m_Grayscale = value; ApplyShaderSettings(); }
        }

        /// <summary>
        /// Sprite the grains take their color from. In the two-layer setup this is the
        /// color sprite sitting behind the grayscale one. Rebuilds the cell table.
        /// </summary>
        public SpriteRenderer ColorSource
        {
            get => m_ColorSource;
            set
            {
                m_ColorSource = value;
                if (Application.isPlaying) BuildCellTable();
            }
        }

        /// <summary>Where the grains are born: inside the image, or above it.</summary>
        public GrainSpawnMode SpawnMode
        {
            get => m_SpawnMode;
            set => m_SpawnMode = value;
        }

        /// <summary>
        /// Which rows come apart first. Only affects cells queued from now on, so
        /// changing it mid-pour will not reorder grains already in flight.
        /// </summary>
        public PourOrder Order
        {
            get => m_PourOrder;
            set => m_PourOrder = value;
        }

        /// <summary>Raised when the last grain lands and nothing is left to pour.</summary>
        public UnityEngine.Events.UnityEvent OnPourComplete => m_OnPourComplete;

        /// <summary>Raised by ResetEffect().</summary>
        public UnityEngine.Events.UnityEvent OnRestored => m_OnRestored;

        #endregion
        // ==================================================================
        #region Internal state

        private static readonly int k_IdMaskTex        = Shader.PropertyToID("_MaskTex");
        private static readonly int k_IdMaskNoise      = Shader.PropertyToID("_MaskNoise");
        private static readonly int k_IdNoiseTex       = Shader.PropertyToID("_NoiseTex");
        private static readonly int k_IdNoiseScale     = Shader.PropertyToID("_NoiseScale");
        private static readonly int k_IdEdgeWidth      = Shader.PropertyToID("_EdgeWidth");
        private static readonly int k_IdEdgeColor      = Shader.PropertyToID("_EdgeColor");
        private static readonly int k_IdEdgeEmission   = Shader.PropertyToID("_EdgeEmission");
        private static readonly int k_IdDissolve       = Shader.PropertyToID("_Dissolve");
        private static readonly int k_IdDirStrength    = Shader.PropertyToID("_DirStrength");
        private static readonly int k_IdGrayscaleAmt   = Shader.PropertyToID("_GrayscaleAmount");
        private static readonly int k_IdGrayscaleTint  = Shader.PropertyToID("_GrayscaleTint");

        private const string k_MaskKeyword      = "_MASK_ON";
        private const string k_GrayscaleKeyword = "_GRAYSCALE_ON";
        private const int k_NoiseRes = 64;

        /// <summary>A grain in flight. Plain struct in a pooled array: no GC.</summary>
        private struct FallingGrain
        {
            public Vector2 Position;    // world XY
            public float TargetY;       // world Y where it lands and erases
            public float VelocityY;
            public float VelocityX;
            public int CellIndex;       // which mask cell it erases on impact
            public Color32 Color;
            public float Size;
        }

        private SpriteRenderer m_SpriteRenderer;
        private Image m_Image;
        private Material m_Material;

        private Texture2D m_MaskTexture;
        private Color32[] m_MaskPixels;
        private bool m_MaskDirty;

        private static byte[] s_Noise;
        private static Texture2D s_NoiseTexture;

        private Color32[] m_CellColors;     // sprite color per cell
        private bool[] m_CellOpaque;        // cells worth pouring at all
        private int m_TotalCells;
        private int m_ErasedCells;

        private int[] m_Queue;              // cell indices waiting to be released
        private int m_QueueCount;
        private int m_QueueHead;
        private float m_ReleaseAccumulator;

        private FallingGrain[] m_Grains;
        private int m_LiveGrains;

        private ParticleSystem m_ParticleSystem;
        private ParticleSystem.EmitParams m_EmitParams;
        private bool m_LoggedSetupError;
        private bool m_PourCompleteRaised = true;

        #endregion
        // ==================================================================
        #region Unity lifecycle

        private void Awake()
        {
            EnsureInitialized();
            if (!Application.isPlaying) return;
            BuildCellTable();
            m_ParticleSystem = CreateParticleSystem();
        }

        private void OnEnable()
        {
            EnsureInitialized();
            ApplyShaderSettings();
        }

        private void OnDestroy()
        {
            if (m_Material != null)
            {
                if (Application.isPlaying) Destroy(m_Material);
                else DestroyImmediate(m_Material);
            }
            if (m_MaskTexture != null)
            {
                if (Application.isPlaying) Destroy(m_MaskTexture);
                else DestroyImmediate(m_MaskTexture);
            }
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                EnsureInitialized();
                ApplyShaderSettings();
            };
#else
            ApplyShaderSettings();
#endif
        }

        private void Update()
        {
            if (!Application.isPlaying) return;

            float dt = Time.deltaTime;
            ReleaseQueuedGrains(dt);
            SimulateGrains(dt);

            if (m_MaskDirty)
            {
                m_MaskTexture.SetPixels32(m_MaskPixels);
                m_MaskTexture.Apply(false);
                m_MaskDirty = false;
            }

            if (!m_PourCompleteRaised && !IsPouring)
            {
                m_PourCompleteRaised = true;
                m_OnPourComplete?.Invoke();
            }
        }

        #endregion
        // ==================================================================
        #region Public methods

        /// <summary>Pours the configured PourAmount of the image.</summary>
        public void Pour() => Pour(m_PourAmount);

        /// <summary>
        /// Pours the given fraction of the image (0-1). Grains start falling now and
        /// erase their cell when they land.
        /// </summary>
        public void Pour(float amount)
        {
            if (!Application.isPlaying) return;
            EnsureInitialized();
            if (m_TotalCells == 0) BuildCellTable();

            int remaining = m_QueueCount - m_QueueHead;
            int wanted = Mathf.RoundToInt(Mathf.Clamp01(amount) * m_TotalCells);
            // Only queue cells that are neither erased nor already queued.
            wanted = Mathf.Min(wanted, m_TotalCells - m_ErasedCells - remaining);
            if (wanted <= 0) return;

            EnqueueCells(wanted);
            m_PourCompleteRaised = false;
        }

        /// <summary>Pours everything that is left.</summary>
        public void PourAll() => Pour(1f);

        /// <summary>Restores the image and clears every grain in flight.</summary>
        public void ResetEffect()
        {
            m_ErasedCells = 0;
            m_LiveGrains = 0;
            m_QueueCount = m_QueueHead = 0;
            m_ReleaseAccumulator = 0f;
            m_PourCompleteRaised = true;

            if (m_MaskPixels != null)
            {
                for (int i = 0; i < m_MaskPixels.Length; i++)
                    m_MaskPixels[i] = new Color32(0, 0, 0, 255);
                m_MaskDirty = true;
            }
            if (m_ParticleSystem != null) m_ParticleSystem.Clear();
            m_OnRestored?.Invoke();
        }

        /// <summary>Rebuilds the cell table. Call it if you swap the sprite at runtime.</summary>
        public void Rebuild()
        {
            BuildCellTable();
            ApplyShaderSettings();
        }

        /// <summary>
        /// Builds the whole two-layer setup from a single sprite in one call.
        ///
        /// Creates a child that renders the same sprite in grayscale, sitting in front of
        /// the original, and returns the effect attached to it. Pouring then erases the
        /// gray layer and reveals the untouched color sprite behind it.
        ///
        ///     var fx = SandHourglassEffect.CreateTwoLayer(myColorSprite);
        ///     fx.PourAll();
        /// </summary>
        public static SandHourglassEffect CreateTwoLayer(SpriteRenderer colorLayer)
        {
            if (colorLayer == null)
            {
                Debug.LogError("[SandHourglass] CreateTwoLayer needs a SpriteRenderer.");
                return null;
            }

            var go = new GameObject(colorLayer.name + " (gray layer)");
            go.transform.SetParent(colorLayer.transform, false);
            go.transform.localPosition = Vector3.zero;

            var gray = go.AddComponent<SpriteRenderer>();
            gray.sprite = colorLayer.sprite;
            gray.sortingLayerID = colorLayer.sortingLayerID;
            gray.sortingOrder = colorLayer.sortingOrder + 1;   // in front of the color one
            gray.flipX = colorLayer.flipX;
            gray.flipY = colorLayer.flipY;

            var fx = go.AddComponent<SandHourglassEffect>();
            fx.m_ColorSource = colorLayer;                     // grains take the real colors
            fx.m_Grayscale = true;                             // this layer renders gray
            fx.m_SpawnMode = GrainSpawnMode.InsideImage;
            return fx;
        }

        #endregion
        // ==================================================================
        #region Setup

        private void EnsureInitialized()
        {
            if (m_SpriteRenderer == null) m_SpriteRenderer = GetComponent<SpriteRenderer>();
            if (m_Image == null)          m_Image          = GetComponent<Image>();

            EnsureNoise();
            EnsureMask();

            if (m_Material == null)
            {
                SetupMaterial();
                ApplyShaderSettings();
            }
        }

        private void SetupMaterial()
        {
            if (m_SpriteRenderer == null && m_Image == null)
            {
                if (!m_LoggedSetupError)
                {
                    Debug.LogError("[SandHourglass] A SpriteRenderer or an Image is required.", this);
                    m_LoggedSetupError = true;
                }
                return;
            }

            Shader shader = Shader.Find("SandDissolve/SpriteDissolve");
            if (shader == null)
            {
                if (!m_LoggedSetupError)
                {
                    Debug.LogError("[SandHourglass] Shader 'SandDissolve/SpriteDissolve' not found. " +
                                   "Add it to Always Included Shaders for builds.", this);
                    m_LoggedSetupError = true;
                }
                return;
            }

            m_Material = new Material(shader)
            {
                name = "SandHourglass (instance)",
                hideFlags = HideFlags.HideAndDontSave
            };

            if (m_SpriteRenderer != null) m_SpriteRenderer.sharedMaterial = m_Material;
            else                          m_Image.material = m_Material;
        }

        private void EnsureMask()
        {
            int res = Mathf.Clamp(m_GridResolution, 8, 64);
            if (m_MaskTexture != null && m_MaskTexture.width == res) return;

            if (m_MaskTexture != null)
            {
                if (Application.isPlaying) Destroy(m_MaskTexture);
                else DestroyImmediate(m_MaskTexture);
            }

            m_MaskTexture = new Texture2D(res, res, TextureFormat.R8, false, true)
            {
                name = "SandHourglassMask",
                // Bilinear so cell edges blend instead of showing hard squares.
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            m_MaskPixels = new Color32[res * res];
            for (int i = 0; i < m_MaskPixels.Length; i++)
                m_MaskPixels[i] = new Color32(0, 0, 0, 255);
            m_MaskTexture.SetPixels32(m_MaskPixels);
            m_MaskTexture.Apply(false);
        }

        private void ApplyShaderSettings()
        {
            if (m_Material == null) SetupMaterial();
            if (m_Material == null) return;

            m_Material.EnableKeyword(k_MaskKeyword);      // this component is mask-driven
            m_Material.SetTexture(k_IdMaskTex, m_MaskTexture);
            m_Material.SetFloat(k_IdMaskNoise, m_MaskNoise);
            m_Material.SetFloat(k_IdNoiseScale, m_NoiseScale);
            if (s_NoiseTexture != null) m_Material.SetTexture(k_IdNoiseTex, s_NoiseTexture);

            m_Material.SetFloat(k_IdEdgeWidth, m_EdgeWidth);
            m_Material.SetColor(k_IdEdgeColor, m_EdgeColor);
            m_Material.SetFloat(k_IdEdgeEmission, m_EdgeEmission);

            // The global threshold path is unused in mask mode.
            m_Material.SetFloat(k_IdDissolve, 0f);
            m_Material.SetFloat(k_IdDirStrength, 0f);

            m_Material.SetFloat(k_IdGrayscaleAmt, m_GrayscaleAmount);
            m_Material.SetColor(k_IdGrayscaleTint, m_GrayscaleTint);
            if (m_Grayscale) m_Material.EnableKeyword(k_GrayscaleKeyword);
            else             m_Material.DisableKeyword(k_GrayscaleKeyword);
        }

        #endregion
        // ==================================================================
        #region Cell table

        /// <summary>
        /// A sprite unpacked for CPU sampling. Handles atlas sub-rects, so a packed
        /// sprite is read from the right region of its texture.
        /// </summary>
        private struct SampledSprite
        {
            public Color32[] Pixels;
            public int Width, Height;
            public Rect UvRect;

            public Color32 Sample(float u, float v)
            {
                int px = Mathf.Clamp((int)((UvRect.x + u * UvRect.width) * Width), 0, Width - 1);
                int py = Mathf.Clamp((int)((UvRect.y + v * UvRect.height) * Height), 0, Height - 1);
                return Pixels[py * Width + px];
            }

            public byte SampleAlpha(float u, float v) => Sample(u, v).a;
        }

        /// <summary>Reads a sprite's pixels, warning clearly if Read/Write is off.</summary>
        private SampledSprite SampleSprite(Sprite sprite, string role)
        {
            var result = new SampledSprite { UvRect = new Rect(0, 0, 1, 1) };
            if (sprite == null || sprite.texture == null) return result;

            var tex = sprite.texture;
            result.Width = tex.width;
            result.Height = tex.height;
            Rect r = sprite.textureRect;
            result.UvRect = new Rect(r.x / tex.width, r.y / tex.height,
                                     r.width / tex.width, r.height / tex.height);
            try
            {
                result.Pixels = tex.GetPixels32();
            }
            catch (Exception)
            {
                result.Pixels = null;
                Debug.LogWarning($"[SandHourglass] '{tex.name}' ({role} sprite) has no " +
                                 "Read/Write Enabled in its Texture Importer, so grains " +
                                 "cannot read its pixels.", this);
            }
            return result;
        }

        /// <summary>
        /// Samples the sprite once into a grid of cells, storing the color each grain
        /// will carry and skipping fully transparent cells.
        /// </summary>
        private void BuildCellTable()
        {
            EnsureMask();
            int res = m_MaskTexture.width;
            int count = res * res;

            if (m_CellColors == null || m_CellColors.Length != count)
            {
                m_CellColors = new Color32[count];
                m_CellOpaque = new bool[count];
                m_Queue      = new int[count];
            }
            if (m_Grains == null || m_Grains.Length != m_MaxLiveGrains)
                m_Grains = new FallingGrain[Mathf.Max(16, m_MaxLiveGrains)];

            // The sprite this component is on decides WHICH cells exist (its shape).
            Sprite ownSprite = m_SpriteRenderer != null ? m_SpriteRenderer.sprite
                                                        : (m_Image != null ? m_Image.sprite : null);
            SampledSprite own = SampleSprite(ownSprite, "shape");

            // The color source decides WHAT COLOR each grain carries. In the two-layer
            // setup this is the color sprite sitting behind the grayscale one.
            Sprite colorSprite = null;
            if (m_ColorSource != null)           colorSprite = m_ColorSource.sprite;
            else if (m_ColorSourceImage != null) colorSprite = m_ColorSourceImage.sprite;

            SampledSprite colorSrc = colorSprite != null && colorSprite != ownSprite
                                   ? SampleSprite(colorSprite, "color")
                                   : own;

            m_TotalCells = 0;
            for (int y = 0; y < res; y++)
            {
                float v = (y + 0.5f) / res;
                for (int x = 0; x < res; x++)
                {
                    int idx = y * res + x;
                    float u = (x + 0.5f) / res;

                    // Shape from the own sprite.
                    if (own.Pixels != null)
                    {
                        m_CellOpaque[idx] = own.SampleAlpha(u, v) >= 64;
                    }
                    else
                    {
                        m_CellOpaque[idx] = true;
                    }

                    // Color from the source layer, sampled at the same UV so the grain
                    // matches the piece that is about to be revealed underneath.
                    if (colorSrc.Pixels != null)
                    {
                        Color32 c = colorSrc.Sample(u, v);
                        c.a = 255;
                        m_CellColors[idx] = c;
                    }
                    else
                    {
                        m_CellColors[idx] = new Color32(220, 200, 160, 255);
                    }

                    if (m_CellOpaque[idx]) m_TotalCells++;
                }
            }
            m_ErasedCells = 0;
        }

        /// <summary>
        /// Picks the next cells to pour. Top rows first by default, which is what makes it
        /// read as an hourglass draining rather than random specks.
        /// </summary>
        private void EnqueueCells(int wanted)
        {
            int res = m_MaskTexture.width;

            // Compact the queue so the head does not creep forward forever.
            if (m_QueueHead > 0)
            {
                int remaining = m_QueueCount - m_QueueHead;
                for (int i = 0; i < remaining; i++) m_Queue[i] = m_Queue[m_QueueHead + i];
                m_QueueCount = remaining;
                m_QueueHead = 0;
            }

            int added = 0;
            if (m_PourOrder == PourOrder.Random)
            {
                for (int idx = 0; idx < m_CellOpaque.Length && added < wanted; idx++)
                {
                    if (!m_CellOpaque[idx] || IsErased(idx) || IsQueued(idx)) continue;
                    m_Queue[m_QueueCount++] = idx;
                    added++;
                }
                for (int i = m_QueueCount - 1; i > m_QueueHead; i--)
                {
                    int j = UnityEngine.Random.Range(m_QueueHead, i + 1);
                    (m_Queue[i], m_Queue[j]) = (m_Queue[j], m_Queue[i]);
                }
                return;
            }

            // Row 0 is the BOTTOM of the sprite (UV v grows upwards), so BottomFirst
            // walks rows ascending and TopFirst walks them descending.
            bool bottomFirst = m_PourOrder == PourOrder.BottomFirst;
            int start = bottomFirst ? 0 : res - 1;
            int step  = bottomFirst ? 1 : -1;

            for (int n = 0; n < res && added < wanted; n++)
            {
                int y = start + step * n;
                int rowStart = y * res;
                int firstOfRow = m_QueueCount;

                for (int x = 0; x < res && added < wanted; x++)
                {
                    int idx = rowStart + x;
                    if (!m_CellOpaque[idx] || IsErased(idx) || IsQueued(idx)) continue;
                    m_Queue[m_QueueCount++] = idx;
                    added++;
                }

                // Shuffle within the row so the release line is not perfectly straight.
                for (int i = m_QueueCount - 1; i > firstOfRow; i--)
                {
                    int j = UnityEngine.Random.Range(firstOfRow, i + 1);
                    (m_Queue[i], m_Queue[j]) = (m_Queue[j], m_Queue[i]);
                }
            }
        }

        private bool IsErased(int idx) => m_MaskPixels[idx].r > 127;

        /// <summary>Marks a cell as spoken for so a second Pour() cannot double-queue it.</summary>
        private bool IsQueued(int idx)
        {
            for (int i = m_QueueHead; i < m_QueueCount; i++)
                if (m_Queue[i] == idx) return true;
            return false;
        }

        #endregion
        // ==================================================================
        #region Simulation

        private void ReleaseQueuedGrains(float dt)
        {
            if (m_QueueHead >= m_QueueCount) return;

            float rate = m_ReleaseRate > 0f
                ? m_ReleaseRate
                : m_TotalCells / Mathf.Max(0.01f, m_PourDuration);

            m_ReleaseAccumulator += rate * dt;
            int toRelease = (int)m_ReleaseAccumulator;
            if (toRelease <= 0) return;
            m_ReleaseAccumulator -= toRelease;

            Bounds b = GetWorldBounds();
            int res = m_MaskTexture.width;
            float sizeRef = Mathf.Max(0.001f, Mathf.Max(b.size.x, b.size.y));

            while (toRelease-- > 0 && m_QueueHead < m_QueueCount)
            {
                if (m_LiveGrains >= m_Grains.Length) break;   // respect the cap

                int idx = m_Queue[m_QueueHead++];
                int cx = idx % res;
                int cy = idx / res;

                float u = (cx + 0.5f) / res;
                float v = (cy + 0.5f) / res;

                float targetX = Mathf.Lerp(b.min.x, b.max.x, u);
                float targetY = Mathf.Lerp(b.min.y, b.max.y, v);

                // Where the grain is born.
                float rise = m_SpawnHeight * b.size.y;
                float jitter = UnityEngine.Random.value * m_SpawnHeightJitter * rise;
                float spawnY;

                if (m_SpawnMode == GrainSpawnMode.InsideImage)
                {
                    // Born just above its OWN cell, so the grain detaches from within the
                    // image instead of raining in from outside. Clamped to the sprite so
                    // it never pops out of the top edge.
                    spawnY = Mathf.Min(targetY + rise + jitter, b.max.y);
                    spawnY = Mathf.Max(spawnY, targetY + 0.001f);
                }
                else
                {
                    float spawnBase = b.max.y + m_SpawnHeight * b.size.y;
                    spawnY = Mathf.Max(spawnBase + jitter, targetY + 0.01f);
                }

                Color32 color = m_CellColors[idx];
                if (m_Grayscale && m_GrayscaleAffectsGrains) color = ApplyGrayscale(color);
                color = Multiply(color, m_GrainTint);

                m_Grains[m_LiveGrains++] = new FallingGrain
                {
                    Position = new Vector2(targetX, spawnY),
                    TargetY = targetY,
                    VelocityY = -m_FallSpeed,
                    VelocityX = (UnityEngine.Random.value - 0.5f) * 2f * m_SidewaysDrift,
                    CellIndex = idx,
                    Color = color,
                    Size = UnityEngine.Random.Range(m_GrainSizeRange.x, m_GrainSizeRange.y) * sizeRef
                };
            }
        }

        private void SimulateGrains(float dt)
        {
            if (m_LiveGrains == 0) return;

            float z = transform.position.z - 0.01f;

            for (int i = m_LiveGrains - 1; i >= 0; i--)
            {
                ref FallingGrain g = ref m_Grains[i];

                g.VelocityY -= m_Gravity * dt;
                g.Position.x += g.VelocityX * dt;
                g.Position.y += g.VelocityY * dt;

                if (g.Position.y <= g.TargetY)
                {
                    // Landed: the grain vanishes and its cell is erased instantly.
                    EraseCell(g.CellIndex);
                    m_Grains[i] = m_Grains[--m_LiveGrains];   // swap-remove, no allocation
                    continue;
                }

                // Draw it for this frame. One-frame lifetime keeps the particle system
                // stateless: we own the simulation, it only rasterizes.
                m_EmitParams.position = new Vector3(g.Position.x, g.Position.y, z);
                m_EmitParams.velocity = Vector3.zero;
                m_EmitParams.startLifetime = dt * 1.5f;
                m_EmitParams.startSize = g.Size;
                m_EmitParams.startColor = g.Color;
                m_EmitParams.applyShapeToPosition = false;
                m_ParticleSystem.Emit(m_EmitParams, 1);
            }
        }

        private void EraseCell(int idx)
        {
            if (m_MaskPixels[idx].r > 127) return;   // already gone
            m_MaskPixels[idx] = new Color32(255, 255, 255, 255);
            m_MaskDirty = true;
            m_ErasedCells++;
        }

        private Color32 ApplyGrayscale(Color32 src)
        {
            float r = src.r / 255f, g = src.g / 255f, b = src.b / 255f;
            float luma = r * 0.299f + g * 0.587f + b * 0.114f;
            r = Mathf.Lerp(r, luma * m_GrayscaleTint.r, m_GrayscaleAmount);
            g = Mathf.Lerp(g, luma * m_GrayscaleTint.g, m_GrayscaleAmount);
            b = Mathf.Lerp(b, luma * m_GrayscaleTint.b, m_GrayscaleAmount);
            return new Color32((byte)(Mathf.Clamp01(r) * 255f),
                               (byte)(Mathf.Clamp01(g) * 255f),
                               (byte)(Mathf.Clamp01(b) * 255f), 255);
        }

        private static Color32 Multiply(Color32 c, Color tint)
        {
            return new Color32((byte)(c.r * Mathf.Clamp01(tint.r)),
                               (byte)(c.g * Mathf.Clamp01(tint.g)),
                               (byte)(c.b * Mathf.Clamp01(tint.b)), 255);
        }

        private Bounds GetWorldBounds()
        {
            if (m_SpriteRenderer != null) return m_SpriteRenderer.bounds;
            if (m_Image != null)
            {
                var rt = m_Image.rectTransform;
                Vector3 c = rt.position;
                Vector2 size = Vector2.Scale(rt.rect.size, rt.lossyScale);
                Vector2 pivot = rt.pivot;
                var min = new Vector3(c.x - size.x * pivot.x, c.y - size.y * pivot.y, c.z);
                var max = new Vector3(min.x + size.x, min.y + size.y, c.z);
                var bounds = new Bounds();
                bounds.SetMinMax(min, max);
                return bounds;
            }
            return new Bounds(transform.position, Vector3.one);
        }

        #endregion
        // ==================================================================
        #region Noise + particles

        private static void EnsureNoise()
        {
            if (s_Noise != null && s_NoiseTexture != null) return;

            int n = k_NoiseRes * k_NoiseRes;
            var acc = new float[n];
            var rnd = new System.Random(1337);
            float amp = 1f, total = 0f;
            int[] octaves = { 4, 8, 16, 32 };
            foreach (int o in octaves)
            {
                AddValueNoise(acc, o, k_NoiseRes, rnd, amp);
                total += amp; amp *= 0.55f;
            }

            float min = float.MaxValue, max = float.MinValue;
            for (int i = 0; i < n; i++)
            {
                acc[i] /= total;
                if (acc[i] < min) min = acc[i];
                if (acc[i] > max) max = acc[i];
            }
            float inv = 1f / Mathf.Max(1e-5f, max - min);

            s_Noise = new byte[n];
            var px = new Color32[n];
            for (int i = 0; i < n; i++)
            {
                byte b = (byte)Mathf.RoundToInt(Mathf.Clamp01((acc[i] - min) * inv) * 255f);
                s_Noise[i] = b;
                px[i] = new Color32(b, b, b, 255);
            }

            s_NoiseTexture = new Texture2D(k_NoiseRes, k_NoiseRes, TextureFormat.R8, false, true)
            {
                name = "SandNoise (runtime)",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            s_NoiseTexture.SetPixels32(px);
            s_NoiseTexture.Apply(false, true);
        }

        private static void AddValueNoise(float[] dst, int res, int size, System.Random rnd, float amp)
        {
            int gw = res + 1;
            var grid = new float[gw * gw];
            for (int y = 0; y < res; y++)
                for (int x = 0; x < res; x++)
                    grid[y * gw + x] = (float)rnd.NextDouble();
            for (int i = 0; i <= res; i++)
            {
                grid[i * gw + res] = grid[i * gw];
                grid[res * gw + i] = grid[i];
            }

            float scale = (float)res / size;
            for (int y = 0; y < size; y++)
            {
                float fy = y * scale; int y0 = (int)fy; float ty = fy - y0;
                ty = ty * ty * (3f - 2f * ty);
                int r0 = y0 * gw, r1 = (y0 + 1) * gw;
                for (int x = 0; x < size; x++)
                {
                    float fx = x * scale; int x0 = (int)fx; float tx = fx - x0;
                    tx = tx * tx * (3f - 2f * tx);
                    float a = grid[r0 + x0], b = grid[r0 + x0 + 1];
                    float c = grid[r1 + x0], d = grid[r1 + x0 + 1];
                    dst[y * size + x] += Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), ty) * amp;
                }
            }
        }

        private ParticleSystem CreateParticleSystem()
        {
            var go = new GameObject("HourglassGrains");
            go.transform.SetParent(transform, false);

            var ps = go.GetComponent<ParticleSystem>();
            if (ps == null) ps = go.AddComponent<ParticleSystem>();
            ps.Stop();

            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startSpeed      = 0f;
            main.startLifetime   = 0.1f;
            main.startSize       = 0.05f;
            main.startRotation   = 0f;
            main.playOnAwake     = false;
            main.maxParticles    = Mathf.Max(64, m_MaxLiveGrains * 2);
            main.gravityModifier = 0f;      // we simulate the fall ourselves
            main.cullingMode     = ParticleSystemCullingMode.Automatic;

            // Everything off: modules are structs returned by value, hence the locals.
            var emission = ps.emission; emission.enabled = false;
            var shape = ps.shape; shape.enabled = false;
            var sol = ps.sizeOverLifetime; sol.enabled = false;
            var rol = ps.rotationOverLifetime; rol.enabled = false;
            var noiseMod = ps.noise; noiseMod.enabled = false;
            var lvol = ps.limitVelocityOverLifetime; lvol.enabled = false;
            var col = ps.collision; col.enabled = false;
            var trig = ps.trigger; trig.enabled = false;
            var sub = ps.subEmitters; sub.enabled = false;
            var trails = ps.trails; trails.enabled = false;
            var lights = ps.lights; lights.enabled = false;
            var tsa = ps.textureSheetAnimation; tsa.enabled = false;
            var vol = ps.velocityOverLifetime; vol.enabled = false;
            var colOverLife = ps.colorOverLifetime; colOverLife.enabled = false;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.sortMode = ParticleSystemSortMode.None;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            renderer.allowRoll = false;
            renderer.maxParticleSize = 0.1f;

            Shader grainShader = Shader.Find("SandDissolve/GrainParticle")
                              ?? Shader.Find("Mobile/Particles/Alpha Blended")
                              ?? Shader.Find("Sprites/Default");
            var mat = new Material(grainShader) { name = "HourglassGrain (runtime)" };
            if (m_GrainTexture != null) mat.mainTexture = m_GrainTexture;
            mat.renderQueue = 3000;
            renderer.sharedMaterial = mat;

            if (m_SpriteRenderer != null)
            {
                renderer.sortingLayerID = m_SpriteRenderer.sortingLayerID;
                renderer.sortingOrder = m_SpriteRenderer.sortingOrder + 1;
            }

            ps.Play();
            return ps;
        }

        #endregion
    }
}
