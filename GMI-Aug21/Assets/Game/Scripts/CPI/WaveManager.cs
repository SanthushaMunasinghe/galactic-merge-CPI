using System;
using System.Collections.Generic;
using Oxtail.Utils;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    /// <summary>Every kind of enemy a wave can spawn. Add a value here, then give it an entry in
    /// WaveManager's Asteroid Types (prefab and health). Keep BlueCyclops first: it is the default type.
    /// Boss is special: a sub-wave never spawns it (see SpawnBoss on WaveConfig instead), it just needs an
    /// Asteroid Types entry for its prefab and Health.</summary>
    public enum AsteroidType
    {
        BlueCyclops,
        CrystalCreature,
        GreenWing,
        Boss,
        GrayBellyBug,
        GrayLeafWings,
        SilverInsect
    }

    /// <summary>
    /// Spawns waves of Asteroid comets into one shared, static grid (via Asteroid.InitializeInGrid) above
    /// the Circuit. Nothing in the grid moves on its own once spawned - a cell's world position is fixed for
    /// its whole life, set once from Grid Center/Width/Height/Rows/Columns at spawn time. Only the boss (see
    /// below) ever scrolls.
    ///
    /// A WaveConfig's Sub Waves are sequential phases sharing that same grid, not stacked rows: TriggerWave
    /// spawns the first phase's cells, and only once every one of them is gone (bullet-killed or otherwise
    /// removed) does the next phase's cells spawn, and so on. Each phase is purely additive - Cells lists
    /// which (Row, Column) get which AsteroidType, and any cell not listed spawns nothing (Row 0 is the row
    /// nearest the circuit, Column 0 is the leftmost; if a cell is listed twice in the same phase, the last
    /// entry wins). A cell can also carry its own Overlay Color, tinting that one creature's sprite instead
    /// of its prefab's own color.
    ///
    /// A wave whose Spawn Boss is on also spawns one Boss-type asteroid immediately, independent of the grid
    /// - it starts held at Boss Spawn Distance above the Circuit's Y and only begins walking down (via
    /// Asteroid.ReleaseBossAdvance, see TryReleaseBoss) once every phase of the wave, including the last, is
    /// fully cleared. It then walks at its own fixed Boss Move Speed (never affected by Health Increment or
    /// any per-wave scaling) until its Y reaches Boss Stop Distance above the Circuit's Y, where it stops to
    /// attack. A boss wave doesn't need any Sub Waves at all - it just needs Spawn Boss on.
    ///
    /// Because grid cells never move, they can never drift into Planet Bounds Radius of Planet Center on
    /// their own, so a non-boss wave cannot damage the planet by an asteroid reaching it - that only happens
    /// via the boss's own attack once it stops (see BossAttackEvent). Planet Center/Bounds Radius are still
    /// required and passed into every spawned asteroid (grid and boss alike), since Asteroid checks arrival
    /// every frame regardless - place Grid Center's Y and Height so no cell's spawn point ever ends up
    /// inside Planet Bounds Radius, unless a cell is deliberately meant to count as an instant planet hit the
    /// moment it spawns.
    ///
    /// Asteroid Types maps each AsteroidType to its prefab and to its Health, the number of bullets it takes
    /// to destroy it (Health Increment is added to every non-boss type's Health after each full - i.e.
    /// has-Sub-Waves - wave is triggered, so later waves take more hits to kill; the boss always uses its own
    /// Health as-is).
    ///
    /// A wave whose Repeat Count is above 0 is played that many extra times in a row (same SubWaves,
    /// SpawnBoss and all) before TriggerWave advances to the next entry in Waves - each repeat is still its
    /// own full TriggerWave call, so Health Increment keeps stacking across them the same way it would across
    /// distinct waves.
    ///
    /// The wave is over the moment no asteroid is left alive (grid cell or boss, however it died), at which
    /// point OnWaveCleared fires. Wave configs are consumed in order by TriggerWave and clamp to the last
    /// entry once exhausted.
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
        public struct GridCell
        {
            [Tooltip("0 is the row nearest the circuit (bottom row).")]
            [Min(0)] public int Row;
            [Tooltip("0 is the leftmost column.")]
            [Min(0)] public int Column;
            public AsteroidType Type;
            [Tooltip("If true, tints this creature's sprite with Overlay Color instead of its prefab's own color.")]
            public bool UseOverlayColor;
            public Color OverlayColor;
        }

        [Serializable]
        public class SubWaveConfig
        {
            [Tooltip("Cells to fill in the shared grid for this phase. Any cell not listed spawns nothing. " +
                "If a cell is listed twice, the last entry wins.")]
            public List<GridCell> Cells = new List<GridCell>();
        }

        [Serializable]
        public class WaveConfig
        {
            [Tooltip("Sequential phases sharing the one grid: the next phase's cells spawn only once every " +
                "cell of the current phase is gone.")]
            public List<SubWaveConfig> SubWaves = new List<SubWaveConfig>();
            [Tooltip("If true, a Boss-type asteroid spawns immediately (held) and releases once every phase " +
                "of this wave is cleared.")]
            public bool SpawnBoss;
            [Tooltip("How many extra times TriggerWave repeats this same wave before moving on to the next " +
                "one. 0 plays it once and progresses as usual; 2 plays it a total of three times.")]
            [Min(0)] public int RepeatCount;
        }

        [Header("Asteroid Types")]
        [SerializeField] private List<AsteroidTypeConfig> m_AsteroidTypes = new List<AsteroidTypeConfig>();
        [Tooltip("Added to every non-boss asteroid type's Health after each full (non-boss) wave, so later " +
            "waves take more hits to kill. Never applied to the boss, which always uses its own Health as-is.")]
        [SerializeField, Min(0)] private int m_HealthIncrement;

        [Header("Grid")]
        [Tooltip("Center of the shared spawn grid every phase fills cells into. Position this by hand in the " +
            "Scene view so it sits inside the wave camera's frame, above the circuit.")]
        [SerializeField] private Transform m_GridCenter;
        [SerializeField, Min(0f)] private float m_GridWidth = 9f;
        [SerializeField, Min(0f)] private float m_GridHeight = 6f;
        [SerializeField, Min(1)] private int m_GridRows = 6;
        [SerializeField, Min(1)] private int m_GridColumns = 6;

        [Header("Circuit")]
        [Tooltip("Boss Spawn/Stop Distance are measured from this transform's position.")]
        [SerializeField] private Transform m_Circuit;

        [Header("Planet")]
        [SerializeField] private Transform m_PlanetCenter;
        [SerializeField, Min(0f)] private float m_PlanetBoundsRadius = 1.5f;

        [Header("Boss")]
        [Tooltip("How far above the Circuit's Y the boss spawns, independent of the grid.")]
        [SerializeField, Min(0f)] private float m_BossSpawnDistance = 13f;
        [SerializeField, Min(0f)] private float m_BossMoveSpeed = 1f;
        [Tooltip("How far above the Circuit's Y the boss stops to attack.")]
        [SerializeField, Min(0f)] private float m_BossStopDistance = 3f;
        [Tooltip("Planet health percent lost each time the boss's attack animation lands a hit.")]
        [SerializeField, Range(0f, 100f)] private float m_BossAttackDamagePercent = 10f;

        [Header("Wave Settings")]
        [SerializeField] private List<WaveConfig> m_Waves = new List<WaveConfig>();

        private readonly List<Asteroid> m_WaveAsteroids = new List<Asteroid>();
        private readonly Dictionary<AsteroidType, AsteroidTypeConfig> m_TypeLookup = new Dictionary<AsteroidType, AsteroidTypeConfig>();
        private readonly HashSet<AsteroidType> m_ReportedMissingTypes = new HashSet<AsteroidType>();
        private int m_CurrentWaveIndex;
        private int m_WaveRepeatsPlayed;
        private int m_FullWaveCount;
        private int m_CurrentHealthBonus;
        private bool m_IsWaveActive;
        private WaveConfig m_CurrentWaveConfig;
        private int m_CurrentPhaseIndex;
        private Asteroid m_CurrentBoss;
        private bool m_BossReleased;

        /// <summary>Fired once no asteroid from the current wave is left alive (bullet-killed, planet-hit,
        /// or - for the boss - however it eventually dies).</summary>
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

            if (m_Circuit == null || m_PlanetCenter == null || m_GridCenter == null)
            {
                Debug.LogError($"{nameof(WaveManager)}: Circuit, Planet Center and Grid Center must be assigned.", this);
                return;
            }

            BuildTypeLookup();

            WaveConfig config = m_Waves[Mathf.Min(m_CurrentWaveIndex, m_Waves.Count - 1)];

            // Repeats the same wave config Repeat Count extra times before moving on: only advance to the
            // next wave once that many repeats have already been played.
            if (m_WaveRepeatsPlayed < config.RepeatCount)
            {
                m_WaveRepeatsPlayed++;
            }
            else
            {
                m_WaveRepeatsPlayed = 0;
                m_CurrentWaveIndex++;
            }

            // Only a full wave (one with Sub Waves) bumps the per-type Health bonus; a boss-only wave just
            // keeps whatever bonus was last set (irrelevant to the boss, which always uses its own Health).
            bool isFullWave = config.SubWaves != null && config.SubWaves.Count > 0;
            if (isFullWave)
            {
                m_CurrentHealthBonus = m_FullWaveCount * m_HealthIncrement;
                m_FullWaveCount++;
            }

            m_WaveAsteroids.Clear();
            m_ReportedMissingTypes.Clear();
            m_CurrentBoss = null;
            m_BossReleased = false;
            m_CurrentWaveConfig = config;
            m_CurrentPhaseIndex = 0;
            m_IsWaveActive = true;

            if (config.SpawnBoss)
                SpawnBoss();

            SpawnNextNonEmptyPhaseOrFinish();
        }

        /// <summary>World position of a grid cell, from Grid Center/Width/Height divided into
        /// Rows x Columns (position only - Grid Center's rotation/scale are ignored).</summary>
        private Vector3 GridCellPosition(int row, int column)
        {
            int rows = Mathf.Max(1, m_GridRows);
            int columns = Mathf.Max(1, m_GridColumns);
            float cellWidth = m_GridWidth / columns;
            float cellHeight = m_GridHeight / rows;
            Vector3 center = m_GridCenter.position;

            float x = center.x - (m_GridWidth * 0.5f) + (cellWidth * (column + 0.5f));
            float y = center.y - (m_GridHeight * 0.5f) + (cellHeight * (row + 0.5f));

            return new Vector3(x, y, center.z);
        }

        /// <summary>Spawns phases in order, skipping any that end up spawning nothing at all (e.g. an empty
        /// Cells list, or every cell missing its type's prefab), until one actually spawns something (then
        /// waits for it to clear) or every phase has been tried (then releases the boss and checks for wave
        /// clear).</summary>
        private void SpawnNextNonEmptyPhaseOrFinish()
        {
            List<SubWaveConfig> phases = m_CurrentWaveConfig.SubWaves;

            while (phases != null && m_CurrentPhaseIndex < phases.Count)
            {
                SpawnPhaseCells(phases[m_CurrentPhaseIndex]);

                if (m_WaveAsteroids.Count > 0)
                    return;

                m_CurrentPhaseIndex++;
            }

            TryReleaseBoss();
            CheckWaveCleared();
        }

        private void SpawnPhaseCells(SubWaveConfig phase)
        {
            m_WaveAsteroids.Clear();

            // Resolves duplicate (Row, Column) entries within the same phase - the last one listed wins -
            // before spawning anything, since Cells is a flat additive list rather than a default-plus-
            // overrides grid.
            var resolved = new Dictionary<(int Row, int Column), GridCell>();
            foreach (GridCell cell in phase.Cells)
            {
                if (cell.Row < 0 || cell.Row >= m_GridRows || cell.Column < 0 || cell.Column >= m_GridColumns)
                {
                    Debug.LogWarning($"{nameof(WaveManager)}: cell ({cell.Row}, {cell.Column}) is outside the " +
                        $"{m_GridRows}x{m_GridColumns} grid; skipping it.", this);
                    continue;
                }

                resolved[(cell.Row, cell.Column)] = cell;
            }

            foreach (GridCell cell in resolved.Values)
            {
                if (!m_TypeLookup.TryGetValue(cell.Type, out AsteroidTypeConfig typeConfig) || typeConfig.Prefab == null)
                {
                    if (m_ReportedMissingTypes.Add(cell.Type))
                        Debug.LogError($"{nameof(WaveManager)}: no prefab assigned for asteroid type {cell.Type} in Asteroid Types; its cells are skipped.", this);

                    continue;
                }

                Vector3 worldPos = GridCellPosition(cell.Row, cell.Column);

                Asteroid asteroid = Instantiate(typeConfig.Prefab, worldPos, Quaternion.identity, transform);
                asteroid.InitializeInGrid(m_PlanetCenter, m_PlanetBoundsRadius, typeConfig.Health + m_CurrentHealthBonus);

                if (cell.UseOverlayColor)
                    asteroid.SetOverlayColor(cell.OverlayColor);

                m_WaveAsteroids.Add(asteroid);
            }
        }

        private void SpawnBoss()
        {
            if (!m_TypeLookup.TryGetValue(AsteroidType.Boss, out AsteroidTypeConfig typeConfig) || typeConfig.Prefab == null)
            {
                Debug.LogError($"{nameof(WaveManager)}: this wave has Spawn Boss on, but no prefab is assigned for AsteroidType.Boss in Asteroid Types.", this);
                return;
            }

            Vector3 worldPos = new Vector3(m_Circuit.position.x, m_Circuit.position.y + m_BossSpawnDistance, transform.position.z);
            float stopY = m_Circuit.position.y + m_BossStopDistance;

            // Unparented: the boss moves at its own Boss Move Speed and stops to attack, entirely
            // independent of the grid. It spawns held in place (see InitializeAsBoss) - TryReleaseBoss lets
            // it start walking once every phase of this wave is cleared.
            Asteroid boss = Instantiate(typeConfig.Prefab, worldPos, Quaternion.identity);
            boss.InitializeAsBoss(m_BossMoveSpeed, stopY, m_BossAttackDamagePercent, typeConfig.Health);
            m_CurrentBoss = boss;
        }

        /// <summary>Releases m_CurrentBoss (see Asteroid.ReleaseBossAdvance) once every asteroid the current
        /// phase spawned (m_WaveAsteroids) is gone - however that happened. A no-op if there's no boss, it
        /// was already released, or the current phase still has survivors.</summary>
        private void TryReleaseBoss()
        {
            if (m_BossReleased || m_CurrentBoss == null || AnyWaveAsteroidAlive())
                return;

            m_BossReleased = true;
            m_CurrentBoss.ReleaseBossAdvance();
        }

        /// <summary>Checks Asteroid.ActiveAsteroids membership rather than IsDead: every removal path
        /// (bullet kill, planet hit, DestroyQuietly) removes an asteroid from ActiveAsteroids synchronously,
        /// before firing the event that leads here, but only sets IsDead afterward (inside
        /// DestroyWithEffect). Checking IsDead here would miss the very asteroid whose own death just
        /// triggered this check, since its DestroyWithEffect call hasn't run yet at this point.</summary>
        private bool AnyWaveAsteroidAlive()
        {
            foreach (Asteroid asteroid in m_WaveAsteroids)
            {
                if (asteroid != null && Asteroid.ActiveAsteroids.Contains(asteroid))
                    return true;
            }

            return false;
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

        private void OnAsteroidDestroyedByBullet(AsteroidDestroyedByBulletEvent evt)
        {
            OnAsteroidRemoved();
        }

        private void OnAsteroidDestroyedByPlanet(AsteroidDestroyedByPlanetEvent evt)
        {
            OnAsteroidRemoved();
        }

        /// <summary>Reacts to any asteroid dying while a wave is active. If the current phase still has a
        /// survivor, does nothing. Otherwise: if there's another phase, spawns it (or skips ahead through any
        /// further empty ones); if every phase has already been spawned and cleared, this was the boss dying
        /// (or a stray event) - just rechecks whether the whole wave is finally over.</summary>
        private void OnAsteroidRemoved()
        {
            if (!m_IsWaveActive || AnyWaveAsteroidAlive())
                return;

            List<SubWaveConfig> phases = m_CurrentWaveConfig.SubWaves;

            if (phases != null && m_CurrentPhaseIndex < phases.Count)
            {
                m_CurrentPhaseIndex++;
                SpawnNextNonEmptyPhaseOrFinish();
            }
            else
            {
                CheckWaveCleared();
            }
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
            if (m_Circuit != null)
            {
                Vector3 circuit = m_Circuit.position;
                const float halfWidth = 6f;

                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(new Vector3(circuit.x - halfWidth, circuit.y + m_BossSpawnDistance, circuit.z),
                    new Vector3(circuit.x + halfWidth, circuit.y + m_BossSpawnDistance, circuit.z));

                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(new Vector3(circuit.x - halfWidth, circuit.y + m_BossStopDistance, circuit.z),
                    new Vector3(circuit.x + halfWidth, circuit.y + m_BossStopDistance, circuit.z));
            }

            if (m_GridCenter != null)
            {
                Vector3 center = m_GridCenter.position;

                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(center, new Vector3(m_GridWidth, m_GridHeight, 0f));

                int rows = Mathf.Max(1, m_GridRows);
                int columns = Mathf.Max(1, m_GridColumns);
                float left = center.x - (m_GridWidth * 0.5f);
                float bottom = center.y - (m_GridHeight * 0.5f);
                float cellWidth = m_GridWidth / columns;
                float cellHeight = m_GridHeight / rows;

                for (int c = 1; c < columns; c++)
                {
                    float x = left + (cellWidth * c);
                    Gizmos.DrawLine(new Vector3(x, bottom, center.z), new Vector3(x, bottom + m_GridHeight, center.z));
                }

                for (int r = 1; r < rows; r++)
                {
                    float y = bottom + (cellHeight * r);
                    Gizmos.DrawLine(new Vector3(left, y, center.z), new Vector3(left + m_GridWidth, y, center.z));
                }
            }
        }
    }
}
