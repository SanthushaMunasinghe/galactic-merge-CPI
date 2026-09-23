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
    /// Spawns a wave of Asteroid comets (in grid mode, via Asteroid.InitializeInGrid — they never slide or
    /// home, they only ride this transform) and scrolls it down past the circuit. TriggerWave places this
    /// transform Spawn Distance above the Circuit's Y (at the Circuit's X) and spawns every sub-wave of the
    /// current wave at once: each sub-wave is its own Rows x Columns grid, stacked upward one after
    /// another with Sub Wave Spacing of extra empty space between them (0 makes them read as one grid), the
    /// first sub-wave lowest so it arrives first. This transform then moves straight down for as long as the
    /// wave is active, at Move Speed for the first full (sub-wave-having) wave, plus one Move Speed
    /// Increment for every full wave triggered before it. Every grid cell's Health likewise gets one Health
    /// Increment added per full wave triggered before the current one (never applied to the boss).
    ///
    /// A wave whose Spawn Boss is on also spawns one Boss-type asteroid, Boss Spawn Distance above the
    /// Circuit's Y — independent of Spawn Distance and of any sub-waves the same wave might have. Unlike a
    /// grid asteroid it is spawned unparented rather than riding this transform (via Asteroid.InitializeAsBoss
    /// instead of InitializeInGrid), and it spawns held in place: it waits behind the sub-waves until every
    /// grid asteroid from the same wave is gone (see TryReleaseBoss), then walks down at its own fixed Boss
    /// Move Speed — never the incrementing Move Speed — until its Y reaches Boss Stop Distance above the
    /// Circuit's Y (the same kind of line as Spawn Distance, not a radius around Planet Center), where it
    /// stops to attack: see InitializeAsBoss for its held-then-walk-then-attack behavior. A boss wave doesn't
    /// count towards Move Speed Increment either, since it has no sub-waves.
    ///
    /// What each cell spawns is an AsteroidType: every sub-wave has a Default Type, and Cell Overrides can
    /// swap individual cells (row 0 is the row nearest the circuit, column 0 the leftmost) for another type.
    /// Asteroid Types maps each type to its prefab and to its Health, the number of bullets it takes to
    /// destroy it.
    ///
    /// A wave whose Repeat Count is above 0 is played that many extra times in a row (same SubWaves,
    /// SpawnBoss and all) before TriggerWave advances to the next entry in Waves - each repeat is still its
    /// own full TriggerWave call, so Move Speed Increment and Health Increment keep stacking across them the
    /// same way they would across distinct waves.
    ///
    /// If Use Stop Point is on, the wave halts once its frontmost row (nearest the circuit, among those
    /// still alive) reaches Stop Distance above the Circuit's Y (see the Stop Point gizmo line), and only
    /// scrolls again once that row is fully destroyed - repeating row by row until nothing grid-spawned is
    /// left.
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
            [Tooltip("If true, this cell spawns nothing at all and Type is ignored.")]
            public bool Empty;
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
            [Tooltip("If true, a Boss-type asteroid spawns Boss Spacing above this wave's last sub-wave.")]
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
        [SerializeField, Min(0f)] private float m_ColumnSpacing = 1.2f;
        [SerializeField, Min(0f)] private float m_RowSpacing = 1.2f;
        [Tooltip("Extra empty vertical space between the last row of one sub-wave and the first row of the " +
            "next, on top of Row Spacing.")]
        [SerializeField, Min(0f)] private float m_SubWaveSpacing = 2f;

        [Header("Movement")]
        [Tooltip("Units per second the first full (non-boss) wave scrolls down at.")]
        [SerializeField, Min(0f)] private float m_MoveSpeed = 1.5f;
        [Tooltip("Added to Move Speed after each full (non-boss) wave, so later waves scroll faster. The " +
            "boss wave never counts towards this and always moves at Boss Move Speed regardless.")]
        [SerializeField, Min(0f)] private float m_MoveSpeedIncrement = 0f;
        [Tooltip("How far above the Circuit's Y the first row of a wave spawns.")]
        [SerializeField, Min(0f)] private float m_SpawnDistance = 13f;
        [Tooltip("How far below the Circuit's Y an asteroid has to scroll to count as having passed it and " +
            "be removed without any effect.")]
        [SerializeField, Min(0f)] private float m_PassDistance = 7f;
        [Tooltip("Spawn and pass distances are measured from this transform's position.")]
        [SerializeField] private Transform m_Circuit;

        [Header("Stop Point")]
        [Tooltip("If true, the wave halts as soon as its frontmost row (the row nearest the circuit that " +
            "still has a living asteroid) reaches Stop Distance above the Circuit's Y, instead of scrolling " +
            "straight through. It resumes scrolling only once that frontmost row is entirely destroyed, at " +
            "which point the next row becomes the new frontmost row and the wave advances until that one " +
            "reaches the line in turn (or, once no grid asteroid is left, the wave simply clears as usual).")]
        [SerializeField] private bool m_UseStopPoint;
        [Tooltip("How far above the Circuit's Y the wave holds at when Use Stop Point is on, measured the " +
            "same way as Spawn Distance.")]
        [SerializeField, Min(0f)] private float m_StopDistance = 5f;

        [Header("Planet")]
        [Tooltip("What counts as the planet for asteroid arrival (health loss). Required: this transform " +
            "scrolls, so it can't stand in for it.")]
        [SerializeField] private Transform m_PlanetCenter;
        [SerializeField, Min(0f)] private float m_PlanetBoundsRadius = 1.5f;

        [Header("Boss")]
        [Tooltip("How far above the Circuit's Y the boss spawns, independent of Spawn Distance.")]
        [SerializeField, Min(0f)] private float m_BossSpawnDistance = 13f;
        [Tooltip("Units per second the boss scrolls down at, independent of Move Speed.")]
        [SerializeField, Min(0f)] private float m_BossMoveSpeed = 1f;
        [Tooltip("How far above the Circuit's Y the boss stops to attack, measured the same way as Spawn " +
            "Distance (not a radius around Planet Center).")]
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
        private float m_CurrentMoveSpeed;
        private int m_CurrentHealthBonus;
        private bool m_IsWaveActive;
        private Asteroid m_CurrentBoss;
        private bool m_BossReleased;

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

            float deltaY = -m_CurrentMoveSpeed * Time.deltaTime;

            if (m_UseStopPoint && deltaY < 0f)
                deltaY = ClampToStopPoint(deltaY);

            transform.position += new Vector3(0f, deltaY, 0f);

            RemovePassedAsteroids();
        }

        /// <summary>Shortens deltaY (always <= 0 coming in) so the frontmost row - the lowest-local-Y grid
        /// asteroid still alive in m_WaveAsteroids - never moves past Stop Distance above the Circuit's Y.
        /// Grid asteroids stay parented with a fixed local position (see Asteroid.InitializeInGrid), so that
        /// row's world Y is just this transform's position plus its local Y. Returns deltaY unchanged if no
        /// grid asteroid is left alive (e.g. a boss-only wave, or every row already cleared).</summary>
        private float ClampToStopPoint(float deltaY)
        {
            if (!TryGetFrontRowLocalY(out float frontRowLocalY))
                return deltaY;

            float stopWorldY = m_Circuit.position.y + m_StopDistance;
            float frontRowWorldY = transform.position.y + frontRowLocalY;

            return Mathf.Max(deltaY, stopWorldY - frontRowWorldY);
        }

        /// <summary>The local Y (relative to this transform) of the row nearest the circuit that still has a
        /// living asteroid in m_WaveAsteroids, i.e. the smallest local Y among them. False if none are alive.</summary>
        private bool TryGetFrontRowLocalY(out float localY)
        {
            localY = 0f;
            bool found = false;

            foreach (Asteroid asteroid in m_WaveAsteroids)
            {
                if (asteroid == null || asteroid.IsDead)
                    continue;

                float y = asteroid.transform.localPosition.y;
                if (!found || y < localY)
                {
                    localY = y;
                    found = true;
                }
            }

            return found;
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

            // Only a full wave's own grid scroll speed and unit health increment; a boss wave has no
            // sub-waves to apply either to (it has no grid cells, and rides no shared transform), so it just
            // keeps whatever speed was last set (irrelevant, since nothing rides it) and moves independently
            // at Boss Move Speed with its own fixed Health instead (see SpawnBoss/InitializeAsBoss).
            bool isFullWave = config.SubWaves != null && config.SubWaves.Count > 0;
            if (isFullWave)
            {
                m_CurrentMoveSpeed = m_MoveSpeed + (m_FullWaveCount * m_MoveSpeedIncrement);
                m_CurrentHealthBonus = m_FullWaveCount * m_HealthIncrement;
                m_FullWaveCount++;
            }

            transform.position = new Vector3(m_Circuit.position.x, m_Circuit.position.y + m_SpawnDistance, transform.position.z);

            m_WaveAsteroids.Clear();
            m_ReportedMissingTypes.Clear();
            m_CurrentBoss = null;
            m_BossReleased = false;
            m_IsWaveActive = true;
            SpawnSubWaves(config);

            // Covers a boss with no sub-waves (or none that spawned anything), which would otherwise sit
            // held forever since nothing would ever call TryReleaseBoss for it.
            TryReleaseBoss();

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

            if (config.SpawnBoss)
                SpawnBoss();
        }

        private void SpawnBoss()
        {
            if (!m_TypeLookup.TryGetValue(AsteroidType.Boss, out AsteroidTypeConfig typeConfig) || typeConfig.Prefab == null)
            {
                Debug.LogError($"{nameof(WaveManager)}: this wave has Spawn Boss on, but no prefab is assigned for AsteroidType.Boss in Asteroid Types.", this);
                return;
            }

            // Measured from the Circuit directly (like Spawn Distance), not from this transform's current
            // position or any sub-wave stacking, so it's independent of the rest of the wave's own spawn point.
            Vector3 worldPos = new Vector3(m_Circuit.position.x, m_Circuit.position.y + m_BossSpawnDistance, transform.position.z);
            float stopY = m_Circuit.position.y + m_BossStopDistance;

            // Unparented, unlike a grid cell: the boss moves at its own Boss Move Speed and stops to attack,
            // so it can't ride this transform's shared scroll the way SpawnGrid's children do. It spawns
            // held in place (see InitializeAsBoss) - TryReleaseBoss lets it start walking once this wave's
            // own grid is cleared.
            Asteroid boss = Instantiate(typeConfig.Prefab, worldPos, Quaternion.identity);
            boss.InitializeAsBoss(m_BossMoveSpeed, stopY, m_BossAttackDamagePercent, typeConfig.Health);
            m_CurrentBoss = boss;
        }

        /// <summary>Releases m_CurrentBoss (see Asteroid.ReleaseBossAdvance) once every grid asteroid from
        /// this wave (m_WaveAsteroids) is gone, however that happened - bullet-killed, planet-hit, or
        /// scrolled past the circuit. A no-op if there's no boss, or it was already released.</summary>
        private void TryReleaseBoss()
        {
            if (m_BossReleased || m_CurrentBoss == null)
                return;

            foreach (Asteroid asteroid in m_WaveAsteroids)
            {
                if (asteroid != null && !asteroid.IsDead)
                    return;
            }

            m_BossReleased = true;
            m_CurrentBoss.ReleaseBossAdvance();
        }

        private void SpawnGrid(SubWaveConfig subWave, float baseY)
        {
            float rowWidth = (subWave.Columns - 1) * m_ColumnSpacing;

            for (int r = 0; r < subWave.Rows; r++)
            {
                for (int c = 0; c < subWave.Columns; c++)
                {
                    if (!TryGetCellType(subWave, r, c, out AsteroidType type))
                        continue;

                    if (!m_TypeLookup.TryGetValue(type, out AsteroidTypeConfig typeConfig) || typeConfig.Prefab == null)
                    {
                        if (m_ReportedMissingTypes.Add(type))
                            Debug.LogError($"{nameof(WaveManager)}: no prefab assigned for asteroid type {type} in Asteroid Types; its cells are skipped.", this);

                        continue;
                    }

                    Vector3 localPos = new Vector3((c * m_ColumnSpacing) - (rowWidth * 0.5f), baseY + (r * m_RowSpacing), 0f);

                    Asteroid asteroid = Instantiate(typeConfig.Prefab, transform.TransformPoint(localPos), Quaternion.identity, transform);
                    asteroid.InitializeInGrid(m_PlanetCenter, m_PlanetBoundsRadius, typeConfig.Health + m_CurrentHealthBonus);
                    m_WaveAsteroids.Add(asteroid);
                }
            }
        }

        /// <summary>False means the cell is empty and should spawn nothing (a later, empty override for
        /// the same cell beats an earlier, non-empty one, and vice versa).</summary>
        private static bool TryGetCellType(SubWaveConfig subWave, int row, int column, out AsteroidType type)
        {
            type = subWave.DefaultType;
            bool empty = false;

            if (subWave.CellOverrides != null)
            {
                foreach (CellOverride cellOverride in subWave.CellOverrides)
                {
                    if (cellOverride.Row != row || cellOverride.Column != column)
                        continue;

                    empty = cellOverride.Empty;
                    type = cellOverride.Type;
                }
            }

            return !empty;
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

            // Unconditional, not just on removedAny: AsteroidDestroyedByBulletEvent/AsteroidDestroyedByPlanetEvent
            // fire before DestroyWithEffect marks the asteroid IsDead (see BulletProjectile.HitTarget and
            // Asteroid.HandlePlanetHit), so TryReleaseBoss's call from those handlers can miss the last grid
            // asteroid dying. This runs every frame the wave is active (the boss itself keeps it active) and
            // catches that case the very next frame; it's a no-op past the first successful release.
            TryReleaseBoss();

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

            if (m_UseStopPoint)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(new Vector3(circuit.x - halfWidth, circuit.y + m_StopDistance, circuit.z),
                    new Vector3(circuit.x + halfWidth, circuit.y + m_StopDistance, circuit.z));
            }

            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(new Vector3(circuit.x - halfWidth, circuit.y + m_BossSpawnDistance, circuit.z),
                new Vector3(circuit.x + halfWidth, circuit.y + m_BossSpawnDistance, circuit.z));

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(new Vector3(circuit.x - halfWidth, circuit.y + m_BossStopDistance, circuit.z),
                new Vector3(circuit.x + halfWidth, circuit.y + m_BossStopDistance, circuit.z));
        }
    }
}
