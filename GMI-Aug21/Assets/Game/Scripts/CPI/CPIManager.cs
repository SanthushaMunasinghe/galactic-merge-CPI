using Oxtail.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

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
        [Header("CPI Circuit")]
        [SerializeField] private CircuitController m_Circuit;

        [Header("CPI Asteroids")]
        [SerializeField] private AsteroidSpawnManager m_AsteroidSpawnManager;

        [Header("CPI Bullets")]
        [SerializeField] private BulletSpawnManager m_BulletSpawnManager;

        [Header("CPI Planet Health")]
        [SerializeField] private Renderer m_PlanetHealthRenderer;
        [SerializeField, Range(0f, 100f)] private float m_HealthGainPercent = 10f;
        [SerializeField, Range(0f, 100f)] private float m_HealthLossPercent = 10f;

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

        [Header("CPI Spaceship Rotation")]
        [SerializeField] private bool m_AlignSpaceshipRotationWithPath;
        [SerializeField] private float m_SpaceshipRotationOffsetDegrees;

        [Header("CPI Cheats")]
        [SerializeField] private bool m_InfiniteMoney;
        [SerializeField] private bool m_ScaleSpawnTier = true;

        [Header("CPI UI")]
        [SerializeField] private GameObject m_UpgradeButtonsRoot;
        [SerializeField] private GameObject[] m_ActivateOnStart;
        [SerializeField] private GameObject[] m_DeactivateOnStart;

        private int m_FloorTier = 1;

        public static new CPIManager Instance => LevelManager.Instance as CPIManager;

        public AsteroidSpawnManager AsteroidSpawner => m_AsteroidSpawnManager;
        public BulletSpawnManager BulletSpawner => m_BulletSpawnManager;

        public float PlanetHealth { get; private set; }

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

            if (m_PlanetHealthRenderer != null)
                m_PlanetHealthRenderer.material = new Material(m_PlanetHealthRenderer.material);

            ApplyPlanetHealthFill();
        }

        private void OnEnable()
        {
            EventManager<ShortcutManager.ShortcutTriggeredEvent>.AddListener(OnShortcutTriggered);
            EventManager<AsteroidDestroyedByBulletEvent>.AddListener(OnAsteroidDestroyedByBullet);
            EventManager<AsteroidDestroyedByPlanetEvent>.AddListener(OnAsteroidDestroyedByPlanet);
            EventManager<CollectPointCollectedEvent>.AddListener(OnCollectPointCollected);
        }

        private void OnDisable()
        {
            EventManager<ShortcutManager.ShortcutTriggeredEvent>.RemoveListener(OnShortcutTriggered);
            EventManager<AsteroidDestroyedByBulletEvent>.RemoveListener(OnAsteroidDestroyedByBullet);
            EventManager<AsteroidDestroyedByPlanetEvent>.RemoveListener(OnAsteroidDestroyedByPlanet);
            EventManager<CollectPointCollectedEvent>.RemoveListener(OnCollectPointCollected);
        }

        private void OnShortcutTriggered(ShortcutManager.ShortcutTriggeredEvent shortcutEvent)
        {
            if (shortcutEvent.Key == KeyCode.S)
                m_AsteroidSpawnManager.TriggerWave();
        }

        private void OnAsteroidDestroyedByBullet(AsteroidDestroyedByBulletEvent evt)
        {
            m_AsteroidSpawnManager.SpawnCollectPoint(evt.Asteroid.transform.position);
        }

        private void OnAsteroidDestroyedByPlanet(AsteroidDestroyedByPlanetEvent evt)
        {
            ChangePlanetHealth(-m_HealthLossPercent);
        }

        private void OnCollectPointCollected(CollectPointCollectedEvent evt)
        {
            ChangePlanetHealth(m_HealthGainPercent);
        }

        private void ChangePlanetHealth(float delta)
        {
            PlanetHealth = Mathf.Clamp(PlanetHealth + delta, 0f, 100f);
            ApplyPlanetHealthFill();
        }

        private void ApplyPlanetHealthFill()
        {
            if (m_PlanetHealthRenderer == null)
                return;

            m_PlanetHealthRenderer.material.SetFloat("_Fill", PlanetHealth / 100f);
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
            m_CurrentCircuit.SetSpaceshipParentsPath(m_AlignSpaceshipRotationWithPath, m_SpaceshipRotationOffsetDegrees);

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

            for (int i = 0; i < m_StartArrowCount; i++)
            {
                AddDefaultSpaceship();
            }

            CheckCanMerge();
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
        }

        public override void SaveIncreaseMergeSpaceshipLevel()
        {
            MergeLevel++;
        }

        protected override void SaveIncreaseRewardLineLevel()
        {
            RewardLineLevel++;
        }

        public override void IncreaseCircuitLevel()
        {
            // No money is spent and no circuit is unlocked: the upgrade button reads MAX in CPI mode.
            CircuitLevel++;
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
