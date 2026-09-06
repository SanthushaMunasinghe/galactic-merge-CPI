using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    /// <summary>
    /// Decides whether the device should use the PARALLAX background (3 layers)
    /// or the BAKED background (1 opaque sprite), aimed at mobile / low-end.
    ///
    /// How it decides (in order):
    ///   1. Manual overrides (m_ForceLowEnd / m_ForceHighEnd) for testing.
    ///   2. Hardware heuristic via SystemInfo (RAM, VRAM, CPU cores, GPU family).
    ///   3. Adaptive probe: if the high tier was chosen, it measures real FPS on
    ///      the device and downgrades to baked whenever it struggles (this also
    ///      catches thermal throttling and bad drivers, which specs cannot see).
    ///
    /// Wire m_ParallaxBackground and m_BakedBackground in the Inspector; the
    /// script simply activates one and deactivates the other.
    /// </summary>
    public class DevicePerformanceEvaluator : MonoBehaviour
    {
        [Header("Backgrounds")]
        [SerializeField] private GameObject m_ParallaxBackground;
        [SerializeField] private GameObject m_BakedBackground;

        [Header("Overrides (testing)")]
        [SerializeField] private bool m_ForceLowEnd = false;
        [SerializeField] private bool m_ForceHighEnd = false;

        [Header("Hardware thresholds")]
        [SerializeField] private int m_LowEndMaxRamMb = 3000;
        [SerializeField] private int m_LowEndMaxVramMb = 512;
        [SerializeField] private int m_LowEndMinCores = 4;

        [Header("Adaptive probe")]
        [SerializeField] private bool m_AdaptiveProbe = true;
        [SerializeField, Range(15f, 60f)] private float m_MinAcceptableFps = 40f;
        [SerializeField] private float m_ProbeWindowSeconds = 3f;
        [SerializeField] private bool m_InfiniteProbeWindow;

        private float m_AccumulatedTime;
        private int m_AccumulatedFrames;
        private bool m_IsLowEnd;

        public bool IsLowEnd => m_IsLowEnd;

        private void Start()
        {
            // Mobile target: measure against a real frame budget
            if (Application.targetFrameRate <= 0)
                Application.targetFrameRate = 60;

            if (m_ForceLowEnd) m_IsLowEnd = true;
            else if (m_ForceHighEnd) m_IsLowEnd = false;
            else m_IsLowEnd = IsProbablyLowEndHardware();

            ApplyTier();
        }

        private void Update()
        {
            // The probe only matters while the expensive tier is active
            if (!m_AdaptiveProbe || m_IsLowEnd || m_ParallaxBackground == null ||
                !m_ParallaxBackground.activeSelf)
                return;

            m_AccumulatedTime += Time.unscaledDeltaTime;
            m_AccumulatedFrames++;

            if (!m_InfiniteProbeWindow && m_AccumulatedTime >= m_ProbeWindowSeconds)
            {
                float fps = m_AccumulatedFrames / m_AccumulatedTime;
                m_AccumulatedTime = 0f;
                m_AccumulatedFrames = 0;

                if (fps < m_MinAcceptableFps)
                {
                    m_IsLowEnd = true;
                    ApplyTier();
                    // once downgraded we stay on baked: cheap and stable
                }
            }
        }

        private void ApplyTier()
        {
            if (m_ParallaxBackground != null)
                m_ParallaxBackground.SetActive(!m_IsLowEnd);
            if (m_BakedBackground != null)
                m_BakedBackground.SetActive(m_IsLowEnd);
        }

        /// <summary>
        /// Instant pre-guess based on device specs. Every check guards against
        /// unreliable reports (some Androids return 0 for GPU memory, etc.).
        /// </summary>
        private bool IsProbablyLowEndHardware()
        {
            int ramMb = SystemInfo.systemMemorySize;
            int vramMb = SystemInfo.graphicsMemorySize;
            int cores = SystemInfo.processorCount;

            if (ramMb > 0 && ramMb <= m_LowEndMaxRamMb) return true;
            if (vramMb > 0 && vramMb <= m_LowEndMaxVramMb) return true;
            if (cores > 0 && cores <= m_LowEndMinCores) return true;
            return IsWeakGpuFamily(SystemInfo.graphicsDeviceName);
        }

        private static bool IsWeakGpuFamily(string gpuName)
        {
            if (string.IsNullOrEmpty(gpuName)) return false;
            string n = gpuName.ToLowerInvariant();
            // Classic low-end GPU families (older Mali, entry Adreno, old PowerVR)
            string[] weakFamilies =
            {
            "mali-4", "mali-t6", "mali-t7", "mali-t8", "mali-g31", "mali-g51",
            "adreno 3", "adreno 4", "adreno 50",
            "powervr ge", "powervr sgx", "vivante"
        };
            foreach (string family in weakFamilies)
                if (n.Contains(family))
                    return true;
            return false;
        }
    }
}