using Oxtail.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    /// <summary>
    /// Listens for SpaceshipHitRewardLineEvent and, for every currently unlocked Shot Point (a prefix of
    /// Shot Points, starting at Initial Unlocked Shot Point Count and growable at runtime via
    /// UnlockNextShotPoint), fires one bullet per point of the crossing spaceship's TierNumber (an
    /// unmerged tier-1 ship fires 1 from each unlocked point, a ship merged once to tier 2 fires 2 from
    /// each, and so on) — so a single hit produces UnlockedShotPointCount x bulletCount bullets in total,
    /// each shot point contributing its own tier-based volley. Every bullet flies straight along the
    /// firing shot point's own up direction (see BulletProjectile.Initialize), so an angled shot point
    /// fires outward instead of straight up without this manager needing to know anything about angles.
    ///
    /// A shot that can't fire yet (PauseFiring's window hasn't elapsed) is queued rather than dropped,
    /// recorded by the shot point Transform it must refire from so its direction is still correct once
    /// the pause lifts; Update retries every queued shot every frame. PauseFiring blocks every shot — new
    /// and already queued — until the given duration elapses. CPIManager calls this with the same
    /// duration as its post-hit health refill cooldown whenever an asteroid reaches the planet, so
    /// shooting and healing resume together. A shot is only ever abandoned via ClearPendingShots (called
    /// by CPIManager once a wave has fully cleared).
    /// </summary>
    public class BulletSpawnManager : MonoBehaviour
    {
        [Header("Prefab")]
        [SerializeField] private BulletProjectile m_BulletPrefab;

        [Header("Flight Settings")]
        [SerializeField] private float m_Speed = 8f;

        [Header("Shot Points")]
        [Tooltip("Every possible bullet-firing position, in unlock order. Only a prefix of this array is " +
            "unlocked at a time (see Initial Unlocked Shot Point Count / UnlockNextShotPoint) — a reward " +
            "line hit fires from every currently unlocked point, not just the first.")]
        [SerializeField] private Transform[] m_ShotPoints;
        [SerializeField, Min(0)] private int m_InitialUnlockedShotPointCount = 1;

        [Header("Shot Point Generator")]
        [Tooltip("Instantiated for each generated shot point (see GenerateShotPoints). Its child sprite " +
            "renderer shows where the point is and which way it fires.")]
        [SerializeField] private GameObject m_ShotPointPrefab;
        [Tooltip("Generated shot points are placed on a circle around this transform.")]
        [SerializeField] private Transform m_ShotPointCenter;
        [SerializeField, Min(0f)] private float m_ShotPointRadius = 2.6f;
        [Tooltip("Angle in degrees between neighbouring shot points, measured around the center.")]
        [SerializeField, Min(0f)] private float m_ShotPointAngleStep = 10f;
        [SerializeField, Min(1)] private int m_ShotPointCount = 7;

        [Header("Collect Point")]
        [SerializeField] private CollectPoint m_CollectPointPrefab;
        [Tooltip("Units per second a collect point flies toward the planet.")]
        [SerializeField, Min(0f)] private float m_CollectPointSpeed = 12f;
        [Tooltip("What collect points fly toward.")]
        [SerializeField] private Transform m_PlanetCenter;
        [Tooltip("How close a collect point has to get to Planet Center to be collected.")]
        [SerializeField, Min(0f)] private float m_PlanetBoundsRadius = 1.5f;

        private readonly List<Transform> m_PendingShots = new List<Transform>();
        private float m_FireUnlockTime;
        private int m_UnlockedShotPointCount;

        public int UnlockedShotPointCount => m_UnlockedShotPointCount;
        public bool AllShotPointsUnlocked => m_ShotPoints != null && m_UnlockedShotPointCount >= m_ShotPoints.Length;

        private void Awake()
        {
            m_UnlockedShotPointCount = m_ShotPoints != null
                ? Mathf.Clamp(m_InitialUnlockedShotPointCount, 0, m_ShotPoints.Length)
                : 0;

            RefreshShotPointVisuals();
        }

        private void OnEnable()
        {
            EventManager<SpaceshipHitRewardLineEvent>.AddListener(OnSpaceshipHitRewardLine);
        }

        private void OnDisable()
        {
            EventManager<SpaceshipHitRewardLineEvent>.RemoveListener(OnSpaceshipHitRewardLine);
        }

        private void Update()
        {
            // Retries every queued shot every frame so a shot that couldn't fire the instant it was
            // requested (still paused) still fires as soon as the pause lifts, instead of being lost.
            for (int i = m_PendingShots.Count - 1; i >= 0; i--)
            {
                Transform shotPoint = m_PendingShots[i];
                if (shotPoint == null || TryFireBullet(shotPoint))
                    m_PendingShots.RemoveAt(i);
            }
        }

        /// <summary>Unlocks the next Shot Point in the array, if any remain locked, and activates its
        /// sprite renderer so the newly unlocked point becomes visible.</summary>
        public void UnlockNextShotPoint()
        {
            if (m_ShotPoints == null || m_UnlockedShotPointCount >= m_ShotPoints.Length)
                return;

            SetShotPointVisualActive(m_UnlockedShotPointCount, true);
            m_UnlockedShotPointCount++;
        }

        /// <summary>Shows the sprite renderer of every currently unlocked Shot Point and hides it on every
        /// locked one. Called on Awake so a Shot Point Count saved/serialized higher than the initial
        /// unlocked count doesn't show points the player hasn't unlocked yet.</summary>
        private void RefreshShotPointVisuals()
        {
            if (m_ShotPoints == null)
                return;

            for (int i = 0; i < m_ShotPoints.Length; i++)
                SetShotPointVisualActive(i, i < m_UnlockedShotPointCount);
        }

        private void SetShotPointVisualActive(int index, bool active)
        {
            Transform shotPoint = m_ShotPoints[index];
            if (shotPoint == null)
                return;

            SpriteRenderer visual = shotPoint.GetComponentInChildren<SpriteRenderer>(true);
            if (visual != null)
                visual.gameObject.SetActive(active);
        }

#if UNITY_EDITOR
        private const string k_GeneratedContainerName = "GeneratedShotPoints";

        /// <summary>
        /// Edit-time button (see BulletSpawnManagerEditor). Clears everything previously generated and
        /// rebuilds Shot Points: Shot Point Count points on a circle of Shot Point Radius around Shot Point
        /// Center, each rotated so its up direction points straight out from the center. Index 0 is straight
        /// up, then they alternate left, right, left, right..., each pair one Shot Point Angle Step further
        /// out, so unlocking them in order widens the fan symmetrically. Points live under a
        /// "GeneratedShotPoints" child, which is the only thing a regenerate deletes — shot points you made
        /// by hand elsewhere are left alone (though the array itself is replaced).
        /// </summary>
        [ContextMenu("Generate Shot Points")]
        public void GenerateShotPoints()
        {
            if (m_ShotPointCenter == null)
            {
                Debug.LogError($"{nameof(BulletSpawnManager)}: Shot Point Center must be assigned.", this);
                return;
            }

            if (m_ShotPointPrefab == null)
            {
                Debug.LogError($"{nameof(BulletSpawnManager)}: Shot Point Prefab must be assigned.", this);
                return;
            }

            UnityEditor.Undo.RecordObject(this, "Generate Shot Points");

            Transform container = transform.Find(k_GeneratedContainerName);
            if (container == null)
            {
                GameObject containerObject = new GameObject(k_GeneratedContainerName);
                UnityEditor.Undo.RegisterCreatedObjectUndo(containerObject, "Generate Shot Points");
                container = containerObject.transform;
                container.SetParent(transform, false);
            }

            for (int i = container.childCount - 1; i >= 0; i--)
                UnityEditor.Undo.DestroyObjectImmediate(container.GetChild(i).gameObject);

            m_ShotPoints = new Transform[m_ShotPointCount];

            for (int i = 0; i < m_ShotPointCount; i++)
            {
                // 0 -> 0, 1 -> +1 step (left), 2 -> -1 step (right), 3 -> +2 steps, 4 -> -2 steps, ...
                int stepIndex = (i + 1) / 2;
                float angle = (i % 2 == 1 ? 1f : -1f) * stepIndex * m_ShotPointAngleStep;
                Quaternion rotation = m_ShotPointCenter.rotation * Quaternion.Euler(0f, 0f, angle);

                GameObject point = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(m_ShotPointPrefab, container);
                point.name = $"ShotPoint_{i}";
                UnityEditor.Undo.RegisterCreatedObjectUndo(point, "Generate Shot Points");
                point.transform.SetPositionAndRotation(
                    m_ShotPointCenter.position + (rotation * Vector3.up * m_ShotPointRadius),
                    rotation);

                m_ShotPoints[i] = point.transform;
            }

            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        private void OnSpaceshipHitRewardLine(SpaceshipHitRewardLineEvent evt)
        {
            if (m_BulletPrefab == null)
            {
                Debug.LogError($"{nameof(BulletSpawnManager)}: Bullet Prefab must be assigned.", this);
                return;
            }

            if (m_ShotPoints == null || m_UnlockedShotPointCount == 0)
            {
                Debug.LogError($"{nameof(BulletSpawnManager)}: no unlocked Shot Points to fire from.", this);
                return;
            }

            int bulletCount = evt.Spaceship != null ? Mathf.Max(1, evt.Spaceship.TierNumber) : 1;

            for (int s = 0; s < m_UnlockedShotPointCount; s++)
            {
                Transform shotPoint = m_ShotPoints[s];
                if (shotPoint == null)
                    continue;

                for (int i = 0; i < bulletCount; i++)
                {
                    if (!TryFireBullet(shotPoint))
                        m_PendingShots.Add(shotPoint);
                }
            }
        }

        private bool TryFireBullet(Transform shotPoint)
        {
            if (Time.time < m_FireUnlockTime)
                return false;

            // Parented to the scene root rather than to this transform: this manager sits under the Circuits
            // object, which is dragged sideways during a wave and would carry in-flight bullets with it.
            BulletProjectile bullet = Instantiate(m_BulletPrefab, shotPoint.position, Quaternion.identity, transform.root);
            bullet.Initialize(shotPoint.up, m_Speed);
            return true;
        }

        /// <summary>Spawns the pickup a bullet kill leaves behind; it flies straight to Planet Center at Collect
        /// Point Speed and heals on arrival (see CollectPoint). Parented to the scene root, not to anything
        /// that moves, so nothing but its own flight carries it.</summary>
        public void SpawnCollectPoint(Vector3 worldPosition)
        {
            if (m_CollectPointPrefab == null || m_PlanetCenter == null)
            {
                Debug.LogError($"{nameof(BulletSpawnManager)}: Collect Point Prefab and Planet Center must be assigned.", this);
                return;
            }

            CollectPoint collectPoint = Instantiate(m_CollectPointPrefab, worldPosition, Quaternion.identity, transform.root);
            collectPoint.Initialize(m_CollectPointSpeed, m_PlanetCenter, m_PlanetBoundsRadius);
        }

        /// <summary>Abandons every shot still waiting to fire. Call once a wave has fully cleared, so a
        /// stray paused shot from the previous wave doesn't unexpectedly fire once the pause lifts.</summary>
        public void ClearPendingShots()
        {
            m_PendingShots.Clear();
        }

        /// <summary>Blocks every shot — a reward line hit during the pause still queues, it just won't
        /// resolve until this elapses — for the given duration from now. A pause already in progress is
        /// only ever extended, never cut short by a shorter one.</summary>
        public void PauseFiring(float duration)
        {
            m_FireUnlockTime = Mathf.Max(m_FireUnlockTime, Time.time + duration);
        }
    }
}
