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
using UnityEngine.Serialization;

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

        private enum DinosaurDeathEffect
        {
            FlyAway,
            Explode
        }

        [Serializable]
        private class PlanetDinosaur
        {
            public GameObject Object;
            [Tooltip("The dinosaur dies when planet health drops from above this percentage to it or " +
                "below. Starting the scene at or below it does not count: health has to climb above it " +
                "first.")]
            [Range(0f, 100f)] public float DieHealthPercent = 50f;
            [Tooltip("Fly Away spins it off screen. Explode plays the explosion prefab on it and removes " +
                "it on the spot.")]
            public DinosaurDeathEffect DeathEffect;

            [NonSerialized] public bool IsDead;
            [NonSerialized] public Tween IdleTween;
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
        [Tooltip("When on, the first asteroid to hit the planet does not take Health Loss Percent off " +
            "it. Instead health drains smoothly from wherever it is down to zero over One Hit Drain " +
            "Duration, and the fail sequence runs when it gets there. Every other asteroid hit and every " +
            "collect point is ignored from that first hit on.")]
        [SerializeField] private bool m_OneHitDrainsPlanetHealth;
        [SerializeField, Min(0f)] private float m_OneHitDrainDuration = 4f;
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
        [Tooltip("Loops on every dinosaur until it dies: it stretches taller, squashes shorter and " +
            "settles back, then does the same three steps with its width. The bottom of the sprite stays " +
            "where it is, so the feet do not slide. A dinosaur with a DinosaurWalker ignores these and " +
            "uses the Scale Animation settings on that component instead.")]
        [SerializeField] private bool m_DinosaurIdleAnimation = true;
        [SerializeField, Min(0f)] private float m_DinosaurIdleStretchPercent = 5f;
        [SerializeField, Min(0f)] private float m_DinosaurIdleSquashPercent = 6f;
        [SerializeField, Min(0f)] private float m_DinosaurIdleStretchDuration = 0.4f;
        [SerializeField, Min(0f)] private float m_DinosaurIdleSquashDuration = 0.3f;
        [SerializeField, Min(0f)] private float m_DinosaurIdleReturnDuration = 0.25f;
        [Tooltip("Spawned on a dinosaur whose death effect is Explode.")]
        [SerializeField] private GameObject m_DinosaurExplosionPrefab;
        [SerializeField, Min(0f)] private float m_DinosaurExplosionScale = 0.6f;
        [Tooltip("The spawned explosion is destroyed after this long, so set it no shorter than the " +
            "effect takes to play.")]
        [SerializeField, Min(0f)] private float m_DinosaurExplosionLifetime = 3f;

        [Header("CPI Zoom")]
        [Tooltip("Z toggles between the camera as it starts and a close up on this. Usually the planet.")]
        [SerializeField] private Transform m_ZoomInTarget;
        [Tooltip("Added to the target position, to frame the close up off centre.")]
        [SerializeField] private Vector2 m_ZoomInOffset;
        [Tooltip("Camera orthographic size when zoomed in. Smaller is closer.")]
        [SerializeField, Min(0.01f)] private float m_ZoomInOrthographicSize = 3.8f;
        [SerializeField, Min(0f)] private float m_ZoomDuration = 1f;
        [SerializeField] private Ease m_ZoomEase = Ease.InOutSine;

        [Header("CPI Hide UI And Circuit")]
        [Tooltip("U hides the UI together with everything on these layers, leaving just the planet, and " +
            "U again brings both back. The layers are hidden by the camera no longer drawing them: " +
            "nothing is switched off, so the circuit, the reward lines and the ships keep running while " +
            "out of sight.")]
        [FormerlySerializedAs("m_ZoomInHiddenLayers")]
        [SerializeField] private LayerMask m_HiddenLayers;

        [Header("CPI Zoom Effect")]
        [Tooltip("L plays the whole close up in one go: zoom in, wait this long, pop the talk cloud in.")]
        [SerializeField, Min(0f)] private float m_ZoomEffectShowCloudDelay = 0.5f;
        [Tooltip("How long the talk cloud stays up before the camera starts zooming back out.")]
        [SerializeField, Min(0f)] private float m_ZoomEffectHoldDuration = 1f;
        [Tooltip("How long after the camera is back out the talk cloud pops away. The UI and the " +
            "circuit come back at the same moment if U had hidden them.")]
        [SerializeField, Min(0f)] private float m_ZoomEffectHideCloudDelay = 0.5f;

        [Header("CPI Talk Cloud")]
        [Tooltip("T toggles this, and it can be ticked by hand while playing. Whatever it is set to " +
            "when the scene starts is how the cloud starts, without animating.")]
        [SerializeField] private bool m_ShowTalkCloud;
        [Tooltip("Pops in by scaling up from nothing and out by scaling back down, around its own " +
            "pivot. Keep the pivot on the tip of the tail so the cloud grows out of it.")]
        [SerializeField] private Transform m_TalkCloud;
        [SerializeField, Min(0f)] private float m_TalkCloudShowDuration = 0.35f;
        [SerializeField] private Ease m_TalkCloudShowEase = Ease.OutBack;
        [SerializeField, Min(0f)] private float m_TalkCloudHideDuration = 0.25f;
        [SerializeField] private Ease m_TalkCloudHideEase = Ease.InBack;

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
        private bool m_IsDrainingPlanetHealth;
        private float m_PreOverrideTimeScale = 1f;

        private Camera m_Camera;
        private Vector3 m_ZoomOutCameraPosition;
        private float m_ZoomOutOrthographicSize;
        private int m_CameraCullingMask;
        private bool m_IsZoomedIn;
        private float m_ZoomProgress;
        private Tween m_ZoomTween;
        private Coroutine m_ZoomEffectCO;

        private readonly List<Canvas> m_HiddenCanvases = new List<Canvas>();
        private bool m_IsUIAndCircuitHidden;

        private Vector3 m_TalkCloudScale;
        private bool m_IsTalkCloudVisible;
        private Tween m_TalkCloudTween;

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
            else if (shortcutEvent.Key == KeyCode.Z)
                SetZoomedIn(!m_IsZoomedIn);
            else if (shortcutEvent.Key == KeyCode.T)
                SetTalkCloudVisible(!m_ShowTalkCloud);
            else if (shortcutEvent.Key == KeyCode.U)
                SetUIAndCircuitVisible(m_IsUIAndCircuitHidden);
            else if (shortcutEvent.Key == KeyCode.L && m_ZoomEffectCO == null)
                m_ZoomEffectCO = StartCoroutine(ZoomEffectCO());
        }

        #region Zoom

        private void InitializeZoom()
        {
            m_Camera = Camera.main;
            if (m_Camera == null)
                return;

            m_ZoomOutCameraPosition = m_Camera.transform.position;
            m_ZoomOutOrthographicSize = m_Camera.orthographicSize;
            m_CameraCullingMask = m_Camera.cullingMask;
        }

        private void SetZoomedIn(bool zoomedIn)
        {
            if (m_Camera == null || zoomedIn == m_IsZoomedIn)
                return;

            m_IsZoomedIn = zoomedIn;

            // Reversing part way through only has the distance already covered to go back over.
            float targetProgress = m_IsZoomedIn ? 1f : 0f;
            float duration = m_ZoomDuration * Mathf.Abs(targetProgress - m_ZoomProgress);

            m_ZoomTween?.Kill();
            m_ZoomTween = DOTween.To(() => m_ZoomProgress, SetZoomProgress, targetProgress, duration)
                .SetEase(m_ZoomEase)
                .SetLink(gameObject);
        }

        /// <summary>
        /// Places the camera between where it started (0) and the close up (1). The close up is worked
        /// out again on every call instead of once when the zoom starts, so the target, offset and size
        /// can be tuned while zoomed in.
        /// </summary>
        private void SetZoomProgress(float progress)
        {
            m_ZoomProgress = progress;

            Vector3 zoomInPosition = m_ZoomOutCameraPosition;
            if (m_ZoomInTarget != null)
            {
                zoomInPosition.x = m_ZoomInTarget.position.x;
                zoomInPosition.y = m_ZoomInTarget.position.y;
            }
            zoomInPosition += (Vector3)m_ZoomInOffset;

            // Unclamped so an ease that overshoots is not flattened at either end.
            m_Camera.transform.position = Vector3.LerpUnclamped(m_ZoomOutCameraPosition, zoomInPosition, progress);
            m_Camera.orthographicSize = Mathf.LerpUnclamped(m_ZoomOutOrthographicSize, m_ZoomInOrthographicSize, progress);
        }

        /// <summary>
        /// The whole close up as one take: zoom in, show the talk cloud, hold, zoom out, hide the talk
        /// cloud and bring the UI and circuit back if U had hidden them. Each step sets the state it
        /// wants rather than toggling, so starting it already zoomed in or with the cloud up just skips
        /// that step.
        /// </summary>
        private IEnumerator ZoomEffectCO()
        {
            SetZoomedIn(true);
            yield return WaitForZoomCO();
            yield return new WaitForSeconds(m_ZoomEffectShowCloudDelay);

            SetTalkCloudVisible(true);
            yield return new WaitForSeconds(m_ZoomEffectHoldDuration);

            SetZoomedIn(false);
            yield return WaitForZoomCO();
            yield return new WaitForSeconds(m_ZoomEffectHideCloudDelay);

            SetTalkCloudVisible(false);
            SetUIAndCircuitVisible(true);

            m_ZoomEffectCO = null;
        }

        private IEnumerator WaitForZoomCO()
        {
            if (m_ZoomTween != null && m_ZoomTween.IsActive())
                yield return m_ZoomTween.WaitForCompletion();
        }

        #endregion

        #region UI and circuit

        /// <summary>
        /// Leaves only the planet on screen, or brings the rest back. Nothing is deactivated either way,
        /// so it all keeps running while hidden. The circuit, reward lines and ships go by the camera
        /// skipping their layers. The interface goes by switching its canvases off rather than their
        /// GameObjects, so other code that activates or deactivates UI objects in the meantime (the hand
        /// pointer on a wave change, for one) is not fought over, and only the canvases this turned off
        /// are turned back on.
        /// </summary>
        private void SetUIAndCircuitVisible(bool visible)
        {
            if (visible != m_IsUIAndCircuitHidden)
                return;

            m_IsUIAndCircuitHidden = !visible;

            if (m_Camera != null)
                m_Camera.cullingMask = visible ? m_CameraCullingMask : m_CameraCullingMask & ~m_HiddenLayers;

            if (visible)
            {
                foreach (var canvas in m_HiddenCanvases)
                {
                    if (canvas != null)
                        canvas.enabled = true;
                }

                m_HiddenCanvases.Clear();
                return;
            }

            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                // World space canvases are part of the scene (the talk cloud text, floating money),
                // not the interface.
                if (!canvas.enabled || canvas.renderMode == RenderMode.WorldSpace)
                    continue;

                canvas.enabled = false;
                m_HiddenCanvases.Add(canvas);
            }
        }

        #endregion

        #region Talk cloud

        private void InitializeTalkCloud()
        {
            if (m_TalkCloud == null)
                return;

            m_TalkCloudScale = m_TalkCloud.localScale;
            m_IsTalkCloudVisible = m_ShowTalkCloud;
            m_TalkCloud.gameObject.SetActive(m_ShowTalkCloud);
        }

        private void SetTalkCloudVisible(bool visible)
        {
            m_ShowTalkCloud = visible;

            if (m_TalkCloud == null || visible == m_IsTalkCloudVisible)
                return;

            m_IsTalkCloudVisible = visible;
            m_TalkCloudTween?.Kill();

            GameObject cloudObject = m_TalkCloud.gameObject;

            if (visible)
            {
                // Only from nothing when it was fully hidden: shown again mid pop out, it grows back
                // from the size it had shrunk to instead of blinking out first.
                if (!cloudObject.activeSelf)
                {
                    m_TalkCloud.localScale = Vector3.zero;
                    cloudObject.SetActive(true);
                }

                m_TalkCloudTween = m_TalkCloud.DOScale(m_TalkCloudScale, m_TalkCloudShowDuration)
                    .SetEase(m_TalkCloudShowEase)
                    .SetLink(cloudObject);
            }
            else
            {
                m_TalkCloudTween = m_TalkCloud.DOScale(Vector3.zero, m_TalkCloudHideDuration)
                    .SetEase(m_TalkCloudHideEase)
                    .SetLink(cloudObject)
                    .OnComplete(() => cloudObject.SetActive(false));
            }
        }

        #endregion

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
            if (m_HasFailed || m_IsDrainingPlanetHealth)
                return;

            bool wasAtZeroHealth = PlanetHealth <= 0f;

            if (m_OneHitDrainsPlanetHealth)
            {
                m_IsDrainingPlanetHealth = true;
                StartCoroutine(DrainPlanetHealthCO());
            }
            else
            {
                ChangePlanetHealth(-m_HealthLossPercent);
            }

            m_HealthRefillUnlockTime = Time.time + m_HealthRefillCooldown;

            if (m_StopShootingDuringCooldown)
                m_BulletSpawnManager.PauseFiring(m_HealthRefillCooldown);

            OnPlanetHit?.Invoke();

            // A drain fails the planet itself once it reaches zero.
            if (wasAtZeroHealth && !m_IsDrainingPlanetHealth)
                TriggerFailSequence();
        }

        private IEnumerator DrainPlanetHealthCO()
        {
            float startHealth = PlanetHealth;
            float elapsed = 0f;

            while (PlanetHealth > 0f)
            {
                yield return null;

                elapsed += Time.deltaTime;
                SetPlanetHealth(m_OneHitDrainDuration > 0f
                    ? Mathf.Lerp(startHealth, 0f, elapsed / m_OneHitDrainDuration)
                    : 0f);
            }

            TriggerFailSequence();
        }

        private void OnCollectPointCollected(CollectPointCollectedEvent evt)
        {
            if (m_HasFailed || m_IsDrainingPlanetHealth || Time.time < m_HealthRefillUnlockTime)
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
            SetPlanetHealth(PlanetHealth + delta);

            if (delta > 0f)
                UIParticleActions.PlayHpGained(delta);
            else if (delta < 0f)
                UIParticleActions.PlayHpLost(-delta);
        }

        private void SetPlanetHealth(float health)
        {
            float previousHealth = PlanetHealth;

            PlanetHealth = Mathf.Clamp(health, 0f, 100f);
            ApplyPlanetHealthVisual(false);
            KillDinosaursBelowHealth(previousHealth);
        }

        /// <summary>
        /// Kills every dinosaur whose die health was crossed by the change that just happened.
        /// Comparing against the health from before the change, rather than only the current value, is
        /// what keeps a dinosaur on the planet when the scene starts at or below its threshold: it only
        /// dies once health has been above it and comes back down.
        /// </summary>
        private void KillDinosaursBelowHealth(float previousHealth)
        {
            if (m_Dinosaurs == null)
                return;

            foreach (var dinosaur in m_Dinosaurs)
            {
                if (dinosaur.Object == null || dinosaur.IsDead)
                    continue;

                if (previousHealth <= dinosaur.DieHealthPercent || PlanetHealth > dinosaur.DieHealthPercent)
                    continue;

                dinosaur.IsDead = true;
                dinosaur.IdleTween?.Kill();

                // Stopped before the death effect takes the transform over, or it would keep being put back.
                if (dinosaur.Object.TryGetComponent(out DinosaurWalker walker))
                    walker.enabled = false;

                if (dinosaur.DeathEffect == DinosaurDeathEffect.Explode)
                    ExplodeDinosaur(dinosaur.Object);
                else
                    FlyAwayDinosaur(dinosaur.Object);
            }
        }

        private void StartDinosaurIdleAnimations()
        {
            if (!m_DinosaurIdleAnimation || m_Dinosaurs == null)
                return;

            foreach (var dinosaur in m_Dinosaurs)
            {
                // A DinosaurWalker plays this animation itself, blended in with its walk. Two things
                // writing the same scale and position would undo each other.
                if (dinosaur.Object != null && !dinosaur.Object.TryGetComponent(out DinosaurWalker _))
                    dinosaur.IdleTween = CreateDinosaurIdleTween(dinosaur.Object);
            }
        }

        private Tween CreateDinosaurIdleTween(GameObject dinosaurObject)
        {
            float cycleDuration = (m_DinosaurIdleStretchDuration + m_DinosaurIdleSquashDuration + m_DinosaurIdleReturnDuration) * 2f;
            if (cycleDuration <= 0f)
                return null;

            Transform dinosaurTransform = dinosaurObject.transform;
            Vector3 baseScale = dinosaurTransform.localScale;
            Vector3 basePosition = dinosaurTransform.localPosition;

            // The sprites are pivoted at their centre, so scaling alone would push the feet down into
            // the planet as much as it lifts the head. The position is shifted with every scale change
            // to keep this point, the middle of the sprite's bottom edge, exactly where it started.
            Vector3 anchor = Vector3.zero;
            var spriteRenderer = dinosaurObject.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                Bounds spriteBounds = spriteRenderer.sprite.bounds;
                anchor = new Vector3(spriteBounds.center.x, spriteBounds.min.y, 0f);
            }

            Vector2 stretch = Vector2.one;

            void ApplyStretch(Vector2 value)
            {
                stretch = value;

                Vector3 scale = new Vector3(baseScale.x * value.x, baseScale.y * value.y, baseScale.z);
                dinosaurTransform.localScale = scale;
                dinosaurTransform.localPosition = basePosition + dinosaurTransform.localRotation * Vector3.Scale(baseScale - scale, anchor);
            }

            Tween Step(Vector2 target, float duration)
            {
                return DOTween.To(() => stretch, ApplyStretch, target, duration).SetEase(Ease.InOutSine);
            }

            float stretched = 1f + m_DinosaurIdleStretchPercent / 100f;
            float squashed = 1f - m_DinosaurIdleSquashPercent / 100f;

            Sequence idle = DOTween.Sequence()
                .Append(Step(new Vector2(1f, stretched), m_DinosaurIdleStretchDuration))
                .Append(Step(new Vector2(1f, squashed), m_DinosaurIdleSquashDuration))
                .Append(Step(Vector2.one, m_DinosaurIdleReturnDuration))
                .Append(Step(new Vector2(stretched, 1f), m_DinosaurIdleStretchDuration))
                .Append(Step(new Vector2(squashed, 1f), m_DinosaurIdleSquashDuration))
                .Append(Step(Vector2.one, m_DinosaurIdleReturnDuration))
                .SetLoops(-1)
                .SetLink(dinosaurObject);

            // Started part way through, somewhere different for each, so they do not all pulse in step.
            idle.Goto(UnityEngine.Random.Range(0f, cycleDuration), true);

            return idle;
        }

        private void ExplodeDinosaur(GameObject dinosaurObject)
        {
            if (m_DinosaurExplosionPrefab != null)
            {
                // Centred on what is drawn rather than on the pivot, which need not be mid sprite.
                var dinosaurRenderer = dinosaurObject.GetComponentInChildren<Renderer>();
                Vector3 position = dinosaurRenderer != null ? dinosaurRenderer.bounds.center : dinosaurObject.transform.position;

                // Left unparented so the planet's scale pulse does not stretch it while it plays.
                GameObject explosion = Instantiate(m_DinosaurExplosionPrefab, position, Quaternion.identity);
                explosion.transform.localScale *= m_DinosaurExplosionScale;

                // Effect prefabs come on the Default sorting layer, which draws behind the planet. Moved
                // to just above the dinosaur, keeping whatever order the effect has between its own parts.
                if (dinosaurRenderer != null)
                {
                    foreach (var renderer in explosion.GetComponentsInChildren<Renderer>(true))
                    {
                        renderer.sortingLayerID = dinosaurRenderer.sortingLayerID;
                        renderer.sortingOrder += dinosaurRenderer.sortingOrder + 1;
                    }
                }

                Destroy(explosion, m_DinosaurExplosionLifetime);
            }

            dinosaurObject.SetActive(false);
        }

        private void FlyAwayDinosaur(GameObject dinosaurObject)
        {
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
            StartDinosaurIdleAnimations();
            InitializeZoom();
            InitializeTalkCloud();

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
        // Applies the glow, zoom and talk cloud values when they are edited in the inspector during
        // play. Deferred because OnValidate is not allowed to create, destroy or activate objects, and
        // unsubscribed first so dragging a slider queues one pass rather than one per change.
        private void OnValidate()
        {
            if (!Application.isPlaying || m_CurrentCircuit == null)
                return;

            UnityEditor.EditorApplication.delayCall -= ApplyPlayModeEdits;
            UnityEditor.EditorApplication.delayCall += ApplyPlayModeEdits;
        }

        private void ApplyPlayModeEdits()
        {
            // A deferred editor call can outlive this manager.
            if (this == null)
                return;

            ApplyCircuitGlow();
            SetTalkCloudVisible(m_ShowTalkCloud);

            // Left alone mid move, where the running zoom is about to write these anyway.
            if (m_Camera != null && (m_ZoomTween == null || !m_ZoomTween.IsActive()))
                SetZoomProgress(m_ZoomProgress);
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
