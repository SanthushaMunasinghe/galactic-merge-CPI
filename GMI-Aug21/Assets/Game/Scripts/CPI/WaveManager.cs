using System;
using System.Collections.Generic;
using Oxtail.Utils;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    /// <summary>
    /// Spawns a wave of Asteroid comets (in grid mode, via Asteroid.InitializeInGrid — they never slide or
    /// home, they only ride this transform) and scrolls it down past the circuit. TriggerWave places this
    /// transform Spawn Distance above the Circuit's Y (at the Circuit's X) and spawns every sub-wave of the
    /// current wave at once: each sub-wave is its own Rows x Columns grid, stacked upward one after
    /// another with Sub Wave Spacing of extra empty space between them (0 makes them read as one grid), the
    /// first sub-wave lowest so it arrives first. This transform then moves straight down at Move Speed for
    /// as long as the wave is active.
    ///
    /// An asteroid reaching the planet (within Planet Bounds Radius of Planet Center) deals the usual
    /// planet-hit damage via the shared AsteroidDestroyedByPlanetEvent (and, unlike a bullet kill, leaves no
    /// collect point behind), and a bullet destroys it the usual way (collect point and all). One that instead scrolls Pass Distance below the Circuit's Y is simply
    /// removed with no effect and no event. The wave is over the moment no asteroid is left alive, however
    /// that happened, at which point OnWaveCleared fires and this transform stops moving. Wave configs are
    /// consumed in order by TriggerWave and clamp to the last entry once exhausted.
    /// </summary>
    public class WaveManager : MonoBehaviour
    {
        [Serializable]
        public struct SubWaveConfig
        {
            public int Rows;
            public int Columns;
        }

        [Serializable]
        public class WaveConfig
        {
            public List<SubWaveConfig> SubWaves = new List<SubWaveConfig>();
        }

        [Header("Prefab")]
        [SerializeField] private Asteroid m_AsteroidPrefab;

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

            if (m_Circuit == null || m_PlanetCenter == null || m_AsteroidPrefab == null)
            {
                Debug.LogError($"{nameof(WaveManager)}: Circuit, Planet Center and Asteroid Prefab must be assigned.", this);
                return;
            }

            WaveConfig config = m_Waves[Mathf.Min(m_CurrentWaveIndex, m_Waves.Count - 1)];
            m_CurrentWaveIndex++;

            transform.position = new Vector3(m_Circuit.position.x, m_Circuit.position.y + m_SpawnDistance, transform.position.z);

            m_WaveAsteroids.Clear();
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
                SpawnGrid(subWave.Rows, subWave.Columns, subWaveBaseY);

                // The next sub-wave's first row sits one Row Spacing plus Sub Wave Spacing above this one's last.
                subWaveBaseY += (Mathf.Max(0, subWave.Rows) * m_RowSpacing) + m_SubWaveSpacing;
            }
        }

        private void SpawnGrid(int rows, int columns, float baseY)
        {
            float rowWidth = (columns - 1) * m_ColumnSpacing;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    Vector3 localPos = new Vector3((c * m_ColumnSpacing) - (rowWidth * 0.5f), baseY + (r * m_RowSpacing), 0f);

                    Asteroid asteroid = Instantiate(m_AsteroidPrefab, transform.TransformPoint(localPos), Quaternion.identity, transform);
                    asteroid.InitializeInGrid(m_PlanetCenter, m_PlanetBoundsRadius);
                    m_WaveAsteroids.Add(asteroid);
                }
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
