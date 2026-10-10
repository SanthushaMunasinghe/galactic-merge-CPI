using DG.Tweening;
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

        [Serializable]
        private class PlanetDinosaur
        {
            public GameObject Object;
            [Tooltip("The dinosaur flies away when planet health drops from above this percentage to it " +
                "or below. Starting the scene at or below it does not count: health has to climb above " +
                "it first.")]
            [Range(0f, 100f)] public float DieHealthPercent = 50f;

            [NonSerialized] public bool HasFlownAway;
        }

        /// <summary>Fired once, right after the fail sequence clears the remaining asteroids and
        /// before its delay, so a CameraShake on the camera can react without CPIManager needing a
        /// direct reference to it.</summary>
        public event Action OnFailShake;

        /// <summary>Fired every time the planet is hit by an asteroid (including a hit that also
        /// triggers the fail sequence), so a PlanetEffect can react without a direct reference.</summary>
        public event Action OnPlanetHit;

        /// <summary>Fired every time an upgrade (add spaceship, merge, reward line, circuit) is
        /// performed, so a PlanetEffect can react without a direct reference.</summary>
        public event Action OnUpgradePerformed;

        /// <summary>Fired every time a Collect Point actually restores planet health (not while the
        /// post-hit refill cooldown is dropping the gain), so a PlanetEffect can react without a
        /// direct reference.</summary>
        public event Action OnPlanetHealthGained;

        [Header("CPI Circuit")]
        [SerializeField] private CircuitController m_Circuit;

        [Header("CPI Asteroids")]
        [SerializeField] private AsteroidSpawnManager m_AsteroidSpawnManager;

        [Header("CPI Bullets")]
        [SerializeField] private BulletSpawnManager m_BulletSpawnManager;

        [Header("CPI Planet Health")]
        [SerializeField, Range(0f, 100f)] private float m_StartHealthPercent = 100f;
        [SerializeField, Range(0f, 100f)] private float m_HealthGainPercent = 10f;
        [SerializeField, Range(0f, 100f)] private float m_HealthLossPercent = 10f;
        [Tooltip("After an asteroid hits the planet, how long collect points stop granting health. If " +
            "Stop Shooting During Cooldown is also on, reward lines stop firing new bullets for the " +
            "same duration.")]
        [SerializeField, Min(0f)] private float m_HealthRefillCooldown = 3f;
        [Tooltip("When on, an asteroid hitting the planet also pauses reward line shooting for Health " +
            "Refill Cooldown, resetting together with the health refill block on every hit. When off, " +
            "shooting is never paused by a planet hit.")]
        [SerializeField] private bool m_StopShootingDuringCooldown = true;
        [Tooltip("The dissolve effect on the greyscale planet sprite (the one layered over the colored " +
            "planet). Health drives it: zero health leaves the grey layer intact, full health dissolves " +
            "it away to reveal the colored planet underneath.")]
        [SerializeField] private SandDissolveEffect m_PlanetDissolve;

        [Header("CPI Planet Sprite")]
        [SerializeField] private bool m_OverridePlanetSprite;
        [SerializeField] private Sprite m_PlanetSprite;
        [Tooltip("Which renderers the sprite override is applied to. Assign both the greyscale and the " +
            "normal planet sprite so the two stay the same shape.")]
        [SerializeField] private SpriteRenderer[] m_PlanetSpriteRenderers;

        [Header("CPI Dinosaurs")]
        [SerializeField] private PlanetDinosaur[] m_Dinosaurs;
        [Tooltip("Dinosaurs fly straight away from this point, so each one leaves in its own direction. " +
            "Usually the planet.")]
        [SerializeField] private Transform m_DinosaurFlyAwayOrigin;
        [Tooltip("How long a dinosaur takes to get from where it stands to fully off screen.")]
        [SerializeField, Min(0f)] private float m_DinosaurFlyAwayDuration = 1.2f;
        [Tooltip("Total spin over the whole flight, in degrees. Negative spins clockwise.")]
        [SerializeField] private float m_DinosaurFlyAwaySpinDegrees = 720f;
        [SerializeField] private Ease m_DinosaurFlyAwayEase = Ease.InQuad;

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

        [Header("CPI Circuit Glow")]
        [Tooltip("Soft halo around the two outline strokes of the circuit. Can be tuned from here " +
            "while the scene is playing.")]
        [SerializeField] private bool m_EnableCircuitGlow = true;
        [SerializeField] private Color m_CircuitGlowColor = new Color(1f, 0.45f, 0f, 1f);
        [Tooltip("How far the halo reaches from each outline stroke, in world units.")]
        [SerializeField, Min(0f)] private float m_CircuitGlowWidth = 0.35f;
        [SerializeField, Min(0f)] private float m_CircuitGlowIntensity = 1f;

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

        [Header("CPI Spaceship Speed")]
        [SerializeField] private bool m_OverrideSpaceshipSpeed;
        [SerializeField, Min(0.01f)] private float m_SpaceshipSpeedMultiplier = 1f;

        [Header("CPI Time Scale")]
        [Tooltip("Overrides Time.timeScale for the whole game while this scene plays — slows or speeds " +
            "up everything (movement, tweens, physics), not just CPI-specific systems. Restored to " +
            "whatever it was before as soon as this manager is destroyed, so leaving the scene never " +
            "leaves the rest of the app running at the wrong speed.")]
        [SerializeField] private bool m_OverrideTimeScale;
        [SerializeField, Min(0.01f)] private float m_TimeScaleMultiplier = 1f;

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
        private float m_HealthRefillUnlockTime;
        private bool m_HasFailed;
        private float m_PreOverrideTimeScale = 1f;

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
            ApplyTimeScaleOverride();

            m_FloorTier = m_StartArrowTier;
            m_MaxSpaceShipTierCreated = m_StartArrowTier;

            Money.Value = m_StartMoney;
            RewardLinesActive = false;

            if (m_HandPointer != null)
                m_HandPointer.SetActive(true);

            // Swapped here rather than in Start because SandDissolveEffect builds its grain table from the
            // sprite in its own Awake, which this component's execution order puts after this one.
            ApplyPlanetSpriteOverride();

            PlanetHealth = m_StartHealthPercent;
        }

        private void OnEnable()
        {
            EventManager<ShortcutManager.ShortcutTriggeredEvent>.AddListener(OnShortcutTriggered);
            EventManager<AsteroidDestroyedByBulletEvent>.AddListener(OnAsteroidDestroyedByBullet);
            EventManager<AsteroidDestroyedByPlanetEvent>.AddListener(OnAsteroidDestroyedByPlanet);
            EventManager<CollectPointCollectedEvent>.AddListener(OnCollectPointCollected);

            m_AsteroidSpawnManager.OnWaveCleared += OnWaveCleared;
        }

        private void OnDisable()
        {
            EventManager<ShortcutManager.ShortcutTriggeredEvent>.RemoveListener(OnShortcutTriggered);
            EventManager<AsteroidDestroyedByBulletEvent>.RemoveListener(OnAsteroidDestroyedByBullet);
            EventManager<AsteroidDestroyedByPlanetEvent>.RemoveListener(OnAsteroidDestroyedByPlanet);
            EventManager<CollectPointCollectedEvent>.RemoveListener(OnCollectPointCollected);

            m_AsteroidSpawnManager.OnWaveCleared -= OnWaveCleared;
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

            bool wasAtZeroHealth = PlanetHealth <= 0f;

            ChangePlanetHealth(-m_HealthLossPercent);
            m_HealthRefillUnlockTime = Time.time + m_HealthRefillCooldown;

            if (m_StopShootingDuringCooldown)
                m_BulletSpawnManager.PauseFiring(m_HealthRefillCooldown);

            OnPlanetHit?.Invoke();

            if (wasAtZeroHealth)
                TriggerFailSequence();
        }

        private void OnCollectPointCollected(CollectPointCollectedEvent evt)
        {
            if (m_HasFailed || Time.time < m_HealthRefillUnlockTime)
                return;

            ChangePlanetHealth(m_HealthGainPercent);
            OnPlanetHealthGained?.Invoke();
        }

        private void TriggerFailSequence()
        {
            if (m_HasFailed)
                return;

            m_HasFailed = true;

            if (m_Circuit != null)
                m_Circuit.StopPath();

            foreach (var asteroid in Asteroid.ActiveAsteroids.ToList())
            {
                if (asteroid != null)
                    asteroid.DestroyWithEffect();
            }

            m_BulletSpawnManager.ClearPendingShots();

            OnFailShake?.Invoke();

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

        private void ChangePlanetHealth(float delta)
        {
            float previousHealth = PlanetHealth;

            PlanetHealth = Mathf.Clamp(PlanetHealth + delta, 0f, 100f);
            ApplyPlanetHealthVisual(false);
            FlyAwayDinosaursBelowHealth(previousHealth);

            if (delta > 0f)
                UIParticleActions.PlayHpGained(delta);
            else if (delta < 0f)
                UIParticleActions.PlayHpLost(-delta);
        }

        /// <summary>
        /// Sends off every dinosaur whose die health was crossed by the change that just happened.
        /// Comparing against the health from before the change, rather than only the current value, is
        /// what keeps a dinosaur on the planet when the scene starts at or below its threshold: it only
        /// leaves once health has been above it and comes back down.
        /// </summary>
        private void FlyAwayDinosaursBelowHealth(float previousHealth)
        {
            if (m_Dinosaurs == null)
                return;

            foreach (var dinosaur in m_Dinosaurs)
            {
                if (dinosaur.Object == null || dinosaur.HasFlownAway)
                    continue;

                if (previousHealth > dinosaur.DieHealthPercent && PlanetHealth <= dinosaur.DieHealthPercent)
                    FlyAwayDinosaur(dinosaur);
            }
        }

        private void FlyAwayDinosaur(PlanetDinosaur dinosaur)
        {
            dinosaur.HasFlownAway = true;

            GameObject dinosaurObject = dinosaur.Object;
            Transform dinosaurTransform = dinosaurObject.transform;

            // Detached before it moves: left under the planet it would be dragged around by every
            // PlanetEffect scale pulse, further the further out it has flown.
            dinosaurTransform.SetParent(null, true);

            Vector3 position = dinosaurTransform.position;
            Vector2 direction = m_DinosaurFlyAwayOrigin != null
                ? (Vector2)(position - m_DinosaurFlyAwayOrigin.position)
                : Vector2.zero;
            direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.up;

            Vector3 target = position + (Vector3)(direction * GetDistanceToLeaveScreen(dinosaurObject, direction));

            dinosaurTransform.DOMove(target, m_DinosaurFlyAwayDuration)
                .SetEase(m_DinosaurFlyAwayEase)
                .SetLink(dinosaurObject)
                .OnComplete(() => dinosaurObject.SetActive(false));

            // Spun at a steady rate whatever the movement ease is, so it is already tumbling the
            // instant it leaves the ground instead of winding up.
            dinosaurTransform.DORotate(new Vector3(0f, 0f, m_DinosaurFlyAwaySpinDegrees), m_DinosaurFlyAwayDuration, RotateMode.LocalAxisAdd)
                .SetEase(Ease.Linear)
                .SetLink(dinosaurObject);
        }

        /// <summary>
        /// How far the object has to travel along the direction before none of it is left inside the
        /// camera view. Its size is taken as a circle around its pivot, so it stays clear of the edge
        /// at any angle of its spin.
        /// </summary>
        private static float GetDistanceToLeaveScreen(GameObject target, Vector2 direction)
        {
            Camera camera = Camera.main;
            Vector2 position = target.transform.position;

            float radius = 0f;
            foreach (var renderer in target.GetComponentsInChildren<Renderer>())
            {
                Bounds bounds = renderer.bounds;
                float reach = ((Vector2)bounds.center - position).magnitude + ((Vector2)bounds.extents).magnitude;
                radius = Mathf.Max(radius, reach);
            }

            float halfHeight = camera.orthographicSize + radius;
            float halfWidth = camera.orthographicSize * camera.aspect + radius;
            Vector2 fromCamera = position - (Vector2)camera.transform.position;

            float distanceX = Mathf.Abs(direction.x) > Mathf.Epsilon
                ? (Mathf.Sign(direction.x) * halfWidth - fromCamera.x) / direction.x
                : float.PositiveInfinity;
            float distanceY = Mathf.Abs(direction.y) > Mathf.Epsilon
                ? (Mathf.Sign(direction.y) * halfHeight - fromCamera.y) / direction.y
                : float.PositiveInfinity;

            return Mathf.Max(0f, Mathf.Min(distanceX, distanceY));
        }

        /// <summary>
        /// Maps planet health onto the dissolve. The effect sits on the greyscale planet sprite, which is
        /// layered on top of an identical colored one, so progress tracks health directly: 0 health leaves
        /// the grey layer fully intact (the planet reads as empty) and 100 dissolves it away completely,
        /// revealing the colored planet. Set immediately at startup so the planet opens on the right state
        /// instead of animating there, and animated on every change after that so grains fly.
        /// </summary>
        private void ApplyPlanetHealthVisual(bool immediate)
        {
            if (m_PlanetDissolve == null)
                return;

            float dissolveProgress = PlanetHealth / 100f;

            if (immediate)
                m_PlanetDissolve.SetProgressImmediate(dissolveProgress);
            else
                m_PlanetDissolve.SetProgress(dissolveProgress);
        }

        private void ApplyPlanetSpriteOverride()
        {
            if (!m_OverridePlanetSprite || m_PlanetSprite == null || m_PlanetSpriteRenderers == null)
                return;

            foreach (var spriteRenderer in m_PlanetSpriteRenderers)
            {
                if (spriteRenderer != null)
                    spriteRenderer.sprite = m_PlanetSprite;
            }
        }

        // The circuit is built in Start, not Awake: CircuitController fills its SpaceshipParent and
        // RewardLine arrays in OnEnable, and this component runs before every other Awake/OnEnable.
        // By Start those arrays exist, and this still runs before any UpgradeButton.Start because of
        // the execution order attribute.
        private void Start()
        {
            // Deliberately not in Awake: SandDissolveEffect.Awake finishes by resetting its own dissolve to
            // 0, and this component's execution order runs its Awake first, so a value pushed there would be
            // wiped straight back to a fully intact planet.
            ApplyPlanetHealthVisual(true);

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
            m_CurrentCircuit.SetSpaceshipParentsPath(m_AlignSpaceshipRotationWithPath, m_SpaceshipRotationOffsetDegrees, m_NegateSpaceshipPathDirection);

            if (m_OverrideSpaceshipSpeed)
                m_CurrentCircuit.SetSpeedMultiplier(m_SpaceshipSpeedMultiplier);

            if (m_OverrideRewardLineAppearance)
                m_CurrentCircuit.SetRewardLinesAppearance(m_RewardLineColor, m_RewardLineDashed, m_RewardLineThickness);

            ApplyCircuitGlow();

            return true;
        }

        private void ApplyCircuitGlow()
        {
            // Also reached through a deferred editor call, which can outlive this manager.
            if (this == null || m_CurrentCircuit == null)
                return;

            m_CurrentCircuit.SetOutlineGlow(m_CircuitGlowColor, m_CircuitGlowWidth, m_EnableCircuitGlow ? m_CircuitGlowIntensity : 0f);
        }

#if UNITY_EDITOR
        // Rebuilds the glow when its values are edited in the inspector during play. Deferred
        // because OnValidate is not allowed to create or destroy objects, and unsubscribed first so
        // dragging a slider queues one rebuild rather than one per change.
        private void OnValidate()
        {
            if (!Application.isPlaying || m_CurrentCircuit == null)
                return;

            UnityEditor.EditorApplication.delayCall -= ApplyCircuitGlow;
            UnityEditor.EditorApplication.delayCall += ApplyCircuitGlow;
        }
#endif

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

        /// <summary>Time.timeScale is global engine state, not scoped to this scene, so the value from
        /// before the override is remembered here and put back in OnDestroy rather than hardcoding 1 —
        /// whatever the app had it set to keeps working once this manager is gone.</summary>
        private void ApplyTimeScaleOverride()
        {
            if (!m_OverrideTimeScale)
                return;

            m_PreOverrideTimeScale = Time.timeScale;
            Time.timeScale = m_TimeScaleMultiplier;
        }

        private void OnDestroy()
        {
            if (m_OverrideTimeScale)
                Time.timeScale = m_PreOverrideTimeScale;
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
