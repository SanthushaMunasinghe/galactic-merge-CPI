using System;
using System.Collections.Generic;
using Oxtail.Utils;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    /// <summary>Every kind of enemy a wave can spawn. Add a value here, then give it an entry in
    /// WaveManager's Asteroid Types (prefab and health). Keep BlueBat first: it is the default type.</summary>
    public enum AsteroidType
    {
        BlueBat,
        CrystalCreature
    }

    /// <summary>
    /// Spawns a wave of Asteroid comets (in grid mode, via Asteroid.InitializeInGrid — they never slide or
    /// home, they only ride this transform) and scrolls it down past the circuit. TriggerWave places this
    /// transform Spawn Distance above the Circuit's Y (at the Circuit's X) and spawns every sub-wave of the
    /// current wave at once: each sub-wave is its own Rows x Columns grid, stacked upward one after
    /// another with Sub Wave Spacing of extra empty space between them (0 makes them read as one grid), the
    /// first sub-wave lowest so it arrives first. This transform then moves straight down at Move Speed for
    /// as long as the wave is active.
    ///
    /// What each cell spawns is an AsteroidType: every sub-wave has a Default Type, and Cell Overrides can
    /// swap individual cells (row 0 is the row nearest the circuit, column 0 the leftmost) for another type.
    /// Asteroid Types maps each type to its prefab and to its Health, the number of bullets it takes to
    /// destroy it.
    ///
    /// An asteroid reaching the planet (within Planet Bounds Radius of Planet Center) deals the usual
    /// planet-hit damage via the shared AsteroidDestroyedByPlanetEvent (and, unlike a bullet kill, leaves no
    /// collect point behind), and enough bullets destroy it the usual way (collect point and all). One that
    /// instead scrolls Pass Distance below the Circuit's Y is simply
    /// removed with no effect and no event. The wave is over the moment no asteroid is left alive, however
    /// that happened, at which point OnWaveCleared fires and this transform stops moving. Wave configs are
    /// consumed in order by TriggerWave and clamp to the last entry once exhausted.
    /// </summary>
    public class WaveManager : MonoBehaviour
    {
        [Serializable]
        public struct AsteroidTypeConfig
        {
            public AsteroidType Type;
            public Asteroid Prefab;
            [Tooltip("How many bullets it takes to destroy this type.")]
            [Min(1)] public int Health;
        }

        [Serializable]
        public struct CellOverride
        {
            [Tooltip("0 is the row nearest the circuit.")]
            [Min(0)] public int Row;
            [Tooltip("0 is the leftmost column.")]
            [Min(0)] public int Column;
            public AsteroidType Type;
        }

        [Serializable]
        public struct SubWaveConfig
        {
            public int Rows;
            public int Columns;
            [Tooltip("What every cell of this sub-wave spawns unless a Cell Override says otherwise.")]
            public AsteroidType DefaultType;
            [Tooltip("Individual cells that spawn a different type. If a cell is listed twice, the last entry wins.")]
            public List<CellOverride> CellOverrides;
        }

        [Serializable]
        public class WaveConfig
        {
            public List<SubWaveConfig> SubWaves = new List<SubWaveConfig>();
        }

        [Header("Asteroid Types")]
        [SerializeField] private List<AsteroidTypeConfig> m_AsteroidTypes = new List<AsteroidTypeConfig>();

        [Header("Grid")]
        [SerializeField, Min(0f)] private float m_ColumnSpacing = 1.2f;
        [SerializeField, Min(0f)] private float m_RowSpacing = 1.2f;
        [Tooltip("Extra empty vertical space between the last row of one sub-wave and the first row of the " +
            "next, on top of Row Spacing.")]
        [SerializeField, Min(0f)] private float m_SubWaveSpacing = 2f;

        [Header("Movement")]
        [Tooltip("Units per second the wave scrolls down.")]
        [SerializeField, Min(0f)] private float m_MoveSpeed = 1.5f;
        [Tooltip("How far above the Circuit's Y the first row of a wave spawns.")]
        [SerializeField, Min(0f)] private float m_SpawnDistance = 13f;
        [Tooltip("How far below the Circuit's Y an asteroid has to scroll to count as having passed it and " +
            "be removed without any effect.")]
        [SerializeField, Min(0f)] private float m_PassDistance = 7f;
        [Tooltip("Spawn and pass distances are measured from this transform's position.")]
        [SerializeField] private Transform m_Circuit;

        [Header("Planet")]
        [Tooltip("What counts as the planet for asteroid arrival (health loss). Required: this transform " +
            "scrolls, so it can't stand in for it.")]
        [SerializeField] private Transform m_PlanetCenter;
        [SerializeField, Min(0f)] private float m_PlanetBoundsRadius = 1.5f;

        [Header("Wave Settings")]
        [SerializeField] private List<WaveConfig> m_Waves = new List<WaveConfig>();

        private readonly List<Asteroid> m_WaveAsteroids = new List<Asteroid>();
        private readonly Dictionary<AsteroidType, AsteroidTypeConfig> m_TypeLookup = new Dictionary<AsteroidType, AsteroidTypeConfig>();
        private readonly HashSet<AsteroidType> m_ReportedMissingTypes = new HashSet<AsteroidType>();
        private int m_CurrentWaveIndex;
        private bool m_IsWaveActive;

        /// <summary>Fired once no asteroid from the current wave is left alive (bullet-killed, planet-hit
        /// or scrolled past the circuit).</summary>
        public event Action OnWaveCleared;

        private void OnEnable()
        {
            EventManager<AsteroidDestroyedByBulletEvent>.AddListener(OnAsteroidDestroyedByBullet);
            EventManager<AsteroidDestroyedByPlanetEvent>.AddListener(OnAsteroidDestroyedByPlanet);
        }

        private void OnDisable()
        {
            EventManager<AsteroidDestroyedByBulletEvent>.RemoveListener(OnAsteroidDestroyedByBullet);
            EventManager<AsteroidDestroyedByPlanetEvent>.RemoveListener(OnAsteroidDestroyedByPlanet);
        }

        private void Update()
        {
            if (!m_IsWaveActive)
                return;

            transform.position += Vector3.down * m_MoveSpeed * Time.deltaTime;

            RemovePassedAsteroids();
        }

        public void TriggerWave()
        {
            if (m_IsWaveActive)
            {
                Debug.Log($"{nameof(WaveManager)}: a wave is already in progress, ignoring TriggerWave.", this);
                return;
            }

            if (m_Waves == null || m_Waves.Count == 0)
            {
                Debug.LogError($"{nameof(WaveManager)}: no waves configured in Waves.", this);
                return;
            }

            if (m_Circuit == null || m_PlanetCenter == null)
            {
                Debug.LogError($"{nameof(WaveManager)}: Circuit and Planet Center must be assigned.", this);
                return;
            }

            BuildTypeLookup();

            WaveConfig config = m_Waves[Mathf.Min(m_CurrentWaveIndex, m_Waves.Count - 1)];
            m_CurrentWaveIndex++;

            transform.position = new Vector3(m_Circuit.position.x, m_Circuit.position.y + m_SpawnDistance, transform.position.z);

            m_WaveAsteroids.Clear();
            m_ReportedMissingTypes.Clear();
            m_IsWaveActive = true;
            SpawnSubWaves(config);

            // Covers a wave with nothing in it, where no destroy event would ever trigger this check.
            CheckWaveCleared();
        }

        private void SpawnSubWaves(WaveConfig config)
        {
            float subWaveBaseY = 0f;

            foreach (SubWaveConfig subWave in config.SubWaves)
            {
                SpawnGrid(subWave, subWaveBaseY);

                // The next sub-wave's first row sits one Row Spacing plus Sub Wave Spacing above this one's last.
                subWaveBaseY += (Mathf.Max(0, subWave.Rows) * m_RowSpacing) + m_SubWaveSpacing;
            }
        }

        private void SpawnGrid(SubWaveConfig subWave, float baseY)
        {
            float rowWidth = (subWave.Columns - 1) * m_ColumnSpacing;

            for (int r = 0; r < subWave.Rows; r++)
            {
                for (int c = 0; c < subWave.Columns; c++)
                {
                    AsteroidType type = GetCellType(subWave, r, c);
                    if (!m_TypeLookup.TryGetValue(type, out AsteroidTypeConfig typeConfig) || typeConfig.Prefab == null)
                    {
                        if (m_ReportedMissingTypes.Add(type))
                            Debug.LogError($"{nameof(WaveManager)}: no prefab assigned for asteroid type {type} in Asteroid Types; its cells are skipped.", this);

                        continue;
                    }

                    Vector3 localPos = new Vector3((c * m_ColumnSpacing) - (rowWidth * 0.5f), baseY + (r * m_RowSpacing), 0f);

                    Asteroid asteroid = Instantiate(typeConfig.Prefab, transform.TransformPoint(localPos), Quaternion.identity, transform);
                    asteroid.InitializeInGrid(m_PlanetCenter, m_PlanetBoundsRadius, typeConfig.Health);
                    m_WaveAsteroids.Add(asteroid);
                }
            }
        }

        private static AsteroidType GetCellType(SubWaveConfig subWave, int row, int column)
        {
            AsteroidType type = subWave.DefaultType;

            if (subWave.CellOverrides != null)
            {
                foreach (CellOverride cellOverride in subWave.CellOverrides)
                {
                    if (cellOverride.Row == row && cellOverride.Column == column)
                        type = cellOverride.Type;
                }
            }

            return type;
        }

        private void BuildTypeLookup()
        {
            m_TypeLookup.Clear();

            foreach (AsteroidTypeConfig typeConfig in m_AsteroidTypes)
            {
                if (!m_TypeLookup.TryAdd(typeConfig.Type, typeConfig))
                    Debug.LogWarning($"{nameof(WaveManager)}: asteroid type {typeConfig.Type} is listed more than once in Asteroid Types; only the first entry is used.", this);
            }
        }

        private void RemovePassedAsteroids()
        {
            float passY = m_Circuit.position.y - m_PassDistance;
            bool removedAny = false;

            for (int i = m_WaveAsteroids.Count - 1; i >= 0; i--)
            {
                Asteroid asteroid = m_WaveAsteroids[i];

                // Already gone, or already dying from a bullet/planet hit: it's handled elsewhere, and
                // removing it quietly here would cut its scatter effect short.
                if (asteroid == null || asteroid.IsDead)
                {
                    m_WaveAsteroids.RemoveAt(i);
                    continue;
                }

                if (asteroid.transform.position.y < passY)
                {
                    m_WaveAsteroids.RemoveAt(i);
                    asteroid.DestroyQuietly();
                    removedAny = true;
                }
            }

            if (removedAny)
                CheckWaveCleared();
        }

        private void OnAsteroidDestroyedByBullet(AsteroidDestroyedByBulletEvent evt)
        {
            CheckWaveCleared();
        }

        private void OnAsteroidDestroyedByPlanet(AsteroidDestroyedByPlanetEvent evt)
        {
            CheckWaveCleared();
        }

        private void CheckWaveCleared()
        {
            if (!m_IsWaveActive || Asteroid.ActiveAsteroids.Count > 0)
                return;

            m_IsWaveActive = false;
            OnWaveCleared?.Invoke();
        }

        private void OnDrawGizmosSelected()
        {
            if (m_Circuit == null)
                return;

            Vector3 circuit = m_Circuit.position;
            const float halfWidth = 6f;

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(new Vector3(circuit.x - halfWidth, circuit.y + m_SpawnDistance, circuit.z),
                new Vector3(circuit.x + halfWidth, circuit.y + m_SpawnDistance, circuit.z));

            Gizmos.color = Color.red;
            Gizmos.DrawLine(new Vector3(circuit.x - halfWidth, circuit.y - m_PassDistance, circuit.z),
                new Vector3(circuit.x + halfWidth, circuit.y - m_PassDistance, circuit.z));
        }
    }
}
