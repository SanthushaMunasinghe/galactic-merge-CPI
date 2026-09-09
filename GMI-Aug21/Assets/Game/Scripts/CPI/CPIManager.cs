using Oxtail.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Oxtail.SpaceshipIncremental
{
    /// <summary>
    /// Local, save free level manager used only by the CPI scene to drive a single circuit for
    /// testing and video capture. It takes over from the regular level managers by becoming
    /// LevelManager.Instance, so every existing UI and gameplay script keeps working untouched.
    /// Nothing is read from or written to SaveLoadManager: money, upgrade levels and spawned
    /// spaceships live for the duration of play mode only.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class CPIManager : LevelManager
    {
        public struct CPIWaveStateChangedEvent
        {
            public bool IsWaveActive;
        }

        /// <summary>Fired once per planet destroyed (including the last one, which also ends the
        /// game), so a CameraShake on the camera can react without CPIManager needing a direct
        /// reference to it. Unlike a single fail-time shake, this can fire once per planet as they die
        /// one at a time.</summary>
        public event Action OnPlanetDestroyed;

        /// <summary>Fired every time an upgrade (add spaceship, merge, reward line, circuit) is
        /// performed, so a PlanetEffect can react without a direct reference.</summary>
        public event Action OnUpgradePerformed;

        [Header("CPI Circuit")]
        [SerializeField] private CircuitController m_Circuit;

        [Header("CPI Asteroids")]
        [SerializeField] private AsteroidSpawnManager m_AsteroidSpawnManager;

        [Header("CPI Bullets")]
        [SerializeField] private BulletSpawnManager m_BulletSpawnManager;

        [Header("CPI Planets")]
        [SerializeField] private List<PlanetEffect> m_Planets = new List<PlanetEffect>();
        [SerializeField] private bool m_HealAllPlanetsSimultaneously;
        [SerializeField] private bool m_DamageAllPlanetsSimultaneously;

        [Header("CPI Wave Timing")]
        [SerializeField, Min(0f)] private float m_InterWaveDelay = 1f;

        [Header("CPI Fail State")]
        [SerializeField, Min(0f)] private float m_FailDelay = 1f;
        [SerializeField] private Volume m_GlobalVolume;
        [SerializeField] private GameObject m_FailObject;

        [Header("CPI Start Values")]
        [SerializeField, Min(0)] private int m_StartArrowCount = 1;
        [SerializeField, Min(1)] private int m_StartArrowTier = 1;
        [SerializeField, Min(0)] private int m_StartLineCount = 1;
        [SerializeField] private double m_StartMoney = 100;
        [SerializeField, Min(0f)] private float m_StartArrowsDelay = 0.5f;

        [Header("CPI Reward Line Appearance")]
        [SerializeField] private bool m_OverrideRewardLineAppearance;
        [SerializeField] private Color m_RewardLineColor = Color.yellow;
        [SerializeField] private bool m_RewardLineDashed = true;
        [SerializeField, Min(0f)] private float m_RewardLineThickness = 0.05f;

        [Header("CPI Floating Text")]
        [SerializeField] private bool m_OverrideFloatingTextGlowColor;
        [SerializeField] private Color m_FloatingTextGlowColor = new Color32(0x15, 0xDB, 0x00, 0x80);

        [Header("CPI Background Music")]
        [SerializeField] private bool m_EnableBackgroundMusic = true;
        [SerializeField] private bool m_OverrideMusicSpeed;
        [SerializeField, Min(0.01f)] private float m_MusicSpeedMultiplier = 1f;

        [Header("CPI Spaceship Rotation")]
        [SerializeField] private bool m_AlignSpaceshipRotationWithPath;
        [SerializeField] private float m_SpaceshipRotationOffsetDegrees;
        [SerializeField] private bool m_NegateSpaceshipPathDirection;
        [SerializeField] private bool m_OverrideSpaceshipRotationSmoothing;
        [SerializeField, Min(0.01f)] private float m_SpaceshipRotationSmoothTime = 0.15f;

        [Header("CPI Spaceship Speed")]
        [SerializeField] private bool m_OverrideSpaceshipSpeed;
        [SerializeField, Min(0.01f)] private float m_SpaceshipSpeedMultiplier = 1f;

        [Header("CPI Spaceship Trail")]
        [SerializeField] private bool m_HideSpaceshipTrail;

        [Header("CPI Cheats")]
        [SerializeField] private bool m_InfiniteMoney;
        [SerializeField] private bool m_ScaleSpawnTier = true;

        [Header("CPI UI")]
        [SerializeField] private GameObject m_UpgradeButtonsRoot;
        [SerializeField] private GameObject[] m_ActivateOnStart;
        [SerializeField] private GameObject[] m_DeactivateOnStart;
        [SerializeField] private GameObject m_HandPointer;

        private int m_FloorTier = 1;
        private bool m_IsWaveActive;
        private bool m_SpawningInitialShips;
        private bool m_HasFailed;

        public static new CPIManager Instance => LevelManager.Instance as CPIManager;

        public AsteroidSpawnManager AsteroidSpawner => m_AsteroidSpawnManager;
        public BulletSpawnManager BulletSpawner => m_BulletSpawnManager;

        public int MergeLevel { get; private set; }
        public int AddSpaceshipLevel { get; private set; }
        public int RewardLineLevel { get; private set; }
        public int CircuitLevel { get; private set; }

        protected override void Awake()
        {
            InheritSerializedFieldsFromSibling();

            base.Awake();

            // Claim the singleton no matter which LevelManager component woke up first.
            m_Instance = this;

            NeutralizeProgression();
            ApplyFloatingTextOverride();

            m_FloorTier = m_StartArrowTier;
            m_MaxSpaceShipTierCreated = m_StartArrowTier;

            Money.Value = m_StartMoney;
            RewardLinesActive = false;

            if (m_HandPointer != null)
                m_HandPointer.SetActive(true);
        }

        private void OnEnable()
        {
            EventManager<ShortcutManager.ShortcutTriggeredEvent>.AddListener(OnShortcutTriggered);
            EventManager<AsteroidDestroyedByBulletEvent>.AddListener(OnAsteroidDestroyedByBullet);
            EventManager<AsteroidDestroyedByPlanetEvent>.AddListener(OnAsteroidDestroyedByPlanet);
            EventManager<CollectPointCollectedEvent>.AddListener(OnCollectPointCollected);

            m_AsteroidSpawnManager.OnWaveCleared += OnWaveCleared;

            for (int i = 0; i < m_Planets.Count; i++)
            {
                if (m_Planets[i] != null)
                    m_Planets[i].OnDestroyed += OnAnyPlanetDestroyed;
            }
        }

        private void OnDisable()
        {
            EventManager<ShortcutManager.ShortcutTriggeredEvent>.RemoveListener(OnShortcutTriggered);
            EventManager<AsteroidDestroyedByBulletEvent>.RemoveListener(OnAsteroidDestroyedByBullet);
            EventManager<AsteroidDestroyedByPlanetEvent>.RemoveListener(OnAsteroidDestroyedByPlanet);
            EventManager<CollectPointCollectedEvent>.RemoveListener(OnCollectPointCollected);

            m_AsteroidSpawnManager.OnWaveCleared -= OnWaveCleared;

            for (int i = 0; i < m_Planets.Count; i++)
            {
                if (m_Planets[i] != null)
                    m_Planets[i].OnDestroyed -= OnAnyPlanetDestroyed;
            }
        }

        private void OnShortcutTriggered(ShortcutManager.ShortcutTriggeredEvent shortcutEvent)
        {
            // S only ever starts a wave from inter-wave state; while a wave is active it's
            // ignored, so a wave can never be triggered on top of another. Returning to inter-wave
            // state happens automatically once the wave's asteroids are all destroyed (OnWaveCleared).
            if (shortcutEvent.Key == KeyCode.S && !m_IsWaveActive && !m_HasFailed)
                SetWaveState(true);
        }

        private void OnWaveCleared()
        {
            if (m_HasFailed)
                return;

            m_BulletSpawnManager.ClearPendingShots();

            StartCoroutine(InterWaveDelayCO());
        }

        private IEnumerator InterWaveDelayCO()
        {
            yield return new WaitForSeconds(m_InterWaveDelay);

            SetWaveState(false);
        }

        private void SetWaveState(bool active)
        {
            m_IsWaveActive = active;
            RewardLinesActive = active;

            if (m_HandPointer != null)
                m_HandPointer.SetActive(!active);

            if (active)
                m_AsteroidSpawnManager.TriggerWave();

            EventManager<CPIWaveStateChangedEvent>.TriggerEvent(new CPIWaveStateChangedEvent { IsWaveActive = active });
        }

        private void OnAsteroidDestroyedByBullet(AsteroidDestroyedByBulletEvent evt)
        {
            m_AsteroidSpawnManager.SpawnCollectPoint(evt.Asteroid.transform.position);
        }

        private void OnAsteroidDestroyedByPlanet(AsteroidDestroyedByPlanetEvent evt)
        {
            if (m_HasFailed)
                return;

            if (m_DamageAllPlanetsSimultaneously)
            {
                for (int i = 0; i < m_Planets.Count; i++)
                {
                    if (m_Planets[i] != null)
                        m_Planets[i].ApplyDamage();
                }
                return;
            }

            if (evt.Planet == null)
            {
                Debug.LogWarning($"{nameof(CPIManager)}: an asteroid hit a planet-layer collider with no PlanetEffect component; no health was applied.", this);
                return;
            }

            evt.Planet.ApplyDamage();
        }

        private void OnCollectPointCollected(CollectPointCollectedEvent evt)
        {
            if (m_HasFailed)
                return;

            if (m_HealAllPlanetsSimultaneously)
            {
                for (int i = 0; i < m_Planets.Count; i++)
                {
                    if (m_Planets[i] != null)
                        m_Planets[i].ApplyHeal();
                }
                return;
            }

            if (evt.Planet == null)
            {
                Debug.LogWarning($"{nameof(CPIManager)}: a collect point reached a planet-layer collider with no PlanetEffect component; no health was applied.", this);
                return;
            }

            evt.Planet.ApplyHeal();
        }

        /// <summary>Fires once per planet death (any planet in Planets). Broadcasts
        /// OnPlanetDestroyed for CameraShake, then checks whether every planet in the list is now
        /// destroyed — an empty list never passes this check, since a scene with no planets configured
        /// has nothing whose destruction should end the game.</summary>
        private void OnAnyPlanetDestroyed()
        {
            if (m_HasFailed)
                return;

            OnPlanetDestroyed?.Invoke();

            if (AllPlanetsDestroyed())
                TriggerFailSequence();
        }

        private bool AllPlanetsDestroyed()
        {
            if (m_Planets.Count == 0)
                return false;

            for (int i = 0; i < m_Planets.Count; i++)
            {
                if (m_Planets[i] != null && !m_Planets[i].IsDestroyed)
                    return false;
            }

            return true;
        }

        private void TriggerFailSequence()
        {
            if (m_HasFailed)
                return;

            m_HasFailed = true;

            if (m_Circuit != null)
                m_Circuit.StopPath();

            foreach (var asteroid in AsteroidProjectile.ActiveAsteroids.ToList())
            {
                if (asteroid != null)
                    asteroid.DestroyWithEffect();
            }

            m_BulletSpawnManager.ClearPendingShots();

            StartCoroutine(FailDelayCO());
        }

        private IEnumerator FailDelayCO()
        {
            yield return new WaitForSeconds(m_FailDelay);

            EnableDepthOfField();

            if (m_FailObject != null)
                m_FailObject.SetActive(true);
        }

        private void EnableDepthOfField()
        {
            if (m_GlobalVolume == null || m_GlobalVolume.profile == null)
                return;

            if (m_GlobalVolume.profile.TryGet(out DepthOfField depthOfField))
                depthOfField.active = true;
        }

        // The circuit is built in Start, not Awake: CircuitController fills its SpaceshipParent and
        // RewardLine arrays in OnEnable, and this component runs before every other Awake/OnEnable.
        // By Start those arrays exist, and this still runs before any UpgradeButton.Start because of
        // the execution order attribute.
        private void Start()
        {
            if (!SetupCircuit())
                return;

            CreateInitialRewardLines();

            if (m_EnableBackgroundMusic)
            {
                AudioManager.Instance.PlayMusic(m_BackgroundMusic);

                if (m_OverrideMusicSpeed)
                    AudioManager.Instance.SetMusicPitch(m_MusicSpeedMultiplier);
            }
            else
            {
                // Explicit stop, not just skipping PlayMusic: AudioManager is a DontDestroyOnLoad
                // singleton, so a track left over from a previous scene would otherwise keep playing.
                AudioManager.Instance.StopMusic();
            }

            CanSpawnSpaceship.Value = m_CurrentCircuit.FreeSpaceShipParents;
            CanChangeCircuit.Value = true;
            CanMerge.Value = false;

            // Done last: activating the buttons runs their Awake, which needs a fully built manager.
            ApplyUIOverrides();

            StartCoroutine(SpawnStartArrowsCO());
        }

        /// <summary>
        /// Copies the serialized object references the base LevelManager needs (spaceship prefab,
        /// speed particles, canvas group, audio clips) from the disabled level manager sitting on the
        /// same GameObject, so they do not have to be assigned twice. Anything already assigned on
        /// this component wins. Arrays (m_Circuits) and the progression assets are never copied:
        /// leaving m_Circuits empty is what makes the circuit upgrade button read MAX.
        /// </summary>
        private void InheritSerializedFieldsFromSibling()
        {
            LevelManager sibling = GetComponents<LevelManager>().FirstOrDefault(manager => manager != this);
            if (sibling == null)
            {
                Debug.LogWarning($"{nameof(CPIManager)}: no other LevelManager on this GameObject to inherit " +
                    "references from. Assign Spaceship Prefab / Speed Particles / Canvas Group manually.", this);
                return;
            }

            var fields = typeof(LevelManager).GetFields(BindingFlags.Instance | BindingFlags.NonPublic |
                BindingFlags.Public | BindingFlags.DeclaredOnly);

            foreach (var field in fields)
            {
                if (!field.IsPublic && !field.IsDefined(typeof(SerializeField), true))
                    continue;

                if (!typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType))
                    continue;

                if (field.Name == "m_LevelProgression" || field.Name == "m_OptionalLevelProgression")
                    continue;

                if (field.GetValue(this) as UnityEngine.Object != null)
                    continue;

                field.SetValue(this, field.GetValue(sibling));
            }
        }

        private bool SetupCircuit()
        {
            if (m_Circuit == null)
            {
                Debug.LogError($"{nameof(CPIManager)}: no circuit assigned, nothing to drive.", this);
                return false;
            }

            m_CircuitIndex = 0;
            m_Circuit.gameObject.SetActive(true);
            m_CurrentCircuit = m_Circuit;
            m_CurrentCircuit.SetSpaceshipParentsPath(m_AlignSpaceshipRotationWithPath, m_SpaceshipRotationOffsetDegrees, m_NegateSpaceshipPathDirection, m_OverrideSpaceshipRotationSmoothing ? m_SpaceshipRotationSmoothTime : -1f);

            if (m_OverrideSpaceshipSpeed)
                m_CurrentCircuit.SetSpeedMultiplier(m_SpaceshipSpeedMultiplier);

            if (m_OverrideRewardLineAppearance)
                m_CurrentCircuit.SetRewardLinesAppearance(m_RewardLineColor, m_RewardLineDashed, m_RewardLineThickness);

            return true;
        }

        /// <summary>
        /// Points the progression info at an objective type nothing ever reports against, so the non
        /// virtual UpdateProgressionStep(type, value) early outs and no goal is ever completed.
        /// </summary>
        private void NeutralizeProgression()
        {
            m_LevelIndex = 0;
            LevelProgressionStepIndex = 0;
            m_ProgressionInfo = new ProgressionStepInfo
            {
                StepType = ProgressionObjectiveType.CompletesInTime,
                StepGoal = BigNumber.Zero
            };
        }

        private void ApplyFloatingTextOverride()
        {
            if (!m_OverrideFloatingTextGlowColor)
                return;

            if (FloatingTextPooler.Instance == null)
            {
                Debug.LogWarning($"{nameof(CPIManager)}: no FloatingTextPooler in the scene, " +
                    "cannot override the floating text glow color.", this);
                return;
            }

            FloatingTextPooler.Instance.SetGlowColorOverride(m_FloatingTextGlowColor);
        }

        private void ApplyUIOverrides()
        {
            SetActiveAll(m_DeactivateOnStart, false);
            SetActiveAll(m_ActivateOnStart, true);

            if (m_UpgradeButtonsRoot != null)
                m_UpgradeButtonsRoot.SetActive(true);
        }

        private static void SetActiveAll(GameObject[] targets, bool active)
        {
            if (targets == null)
                return;

            foreach (var target in targets)
            {
                if (target != null)
                    target.SetActive(active);
            }
        }

        private IEnumerator SpawnStartArrowsCO()
        {
            yield return new WaitForSeconds(m_StartArrowsDelay);

            m_SpawningInitialShips = true;
            for (int i = 0; i < m_StartArrowCount; i++)
            {
                AddDefaultSpaceship();
            }
            m_SpawningInitialShips = false;

            CheckCanMerge();
        }

        /// <summary>
        /// While the initial batch is spawning, fill slots in authored order ("one behind another")
        /// instead of at random, so the starting ships queue up cleanly for recording. Anything added
        /// afterward (e.g. via the Add Spaceship button) falls back to the normal random placement.
        /// </summary>
        protected override SpaceshipParent SelectSpaceshipParentForSpawn()
        {
            if (m_SpawningInitialShips)
            {
                var parent = m_CurrentCircuit.GetNextFreeSpaceshipParentInOrder();
                if (parent != null)
                    return parent;
            }

            return base.SelectSpaceshipParentForSpawn();
        }

        /// <summary>Hides the trail on every newly created spaceship when the override is on.</summary>
        protected override void OnSpaceshipCreated(Spaceship spaceship)
        {
            if (m_HideSpaceshipTrail)
                spaceship.SetTrailVisible(false);
        }

        #region Circuit

        protected override void CreateInitialRewardLines()
        {
            RewardLinesCount = 0;

            for (int i = 0; i < m_StartLineCount; i++)
            {
                CreateRewardLine();
            }
        }

        protected override void CreateRewardLine()
        {
            m_CurrentCircuit.AddRewardLine();
            RewardLinesCount++;
        }

        /// <summary>Only one circuit exists in CPI mode, so there is nothing to change to.</summary>
        public override void ChangeCircuit()
        {
        }

        #endregion

        #region Spaceships

        protected override List<int> GetSpawnedSpaceships()
        {
            return Enumerable.Repeat(m_StartArrowTier, m_StartArrowCount).ToList();
        }

        protected override int GetSpawnSpaceshipTier()
        {
            return m_FloorTier;
        }

        protected override void SetNewFloorTier()
        {
            if (!m_ScaleSpawnTier)
                return;

            int newFloorTier = Mathf.FloorToInt((m_MaxSpaceShipTierCreated / 4f) + 1);
            if (m_FloorTier < newFloorTier)
                m_FloorTier = newFloorTier;
        }

        #endregion

        #region Money and upgrade levels

        public override void RemoveMoney(BigNumber money)
        {
            if (m_InfiniteMoney)
                return;

            base.RemoveMoney(money);
        }

        public override void IncreaseAddSpaceshipLevel()
        {
            AddSpaceshipLevel++;
            OnUpgradePerformed?.Invoke();
        }

        public override void SaveIncreaseMergeSpaceshipLevel()
        {
            MergeLevel++;
            OnUpgradePerformed?.Invoke();
        }

        protected override void SaveIncreaseRewardLineLevel()
        {
            RewardLineLevel++;
            OnUpgradePerformed?.Invoke();
        }

        public override void IncreaseCircuitLevel()
        {
            // No money is spent and no circuit is unlocked: the upgrade button reads MAX in CPI mode.
            CircuitLevel++;
            OnUpgradePerformed?.Invoke();
        }

        #endregion

        #region Progression and persistence, all disabled in CPI mode

        protected override void UpdateProgressionStep()
        {
        }

        protected override void StepCompleted()
        {
        }

        protected override void IncreaseProgressionStep()
        {
        }

        protected override void CompleteLevel()
        {
        }

        protected override void SaveMoney()
        {
        }

        protected override void SaveSpawnedSpaceshipsTiers(List<int> spaceships)
        {
        }

        protected override void SaveStepAccumulatedObjective()
        {
        }

        protected override void SaveLevelCompletionData()
        {
        }

        #endregion
    }
}
