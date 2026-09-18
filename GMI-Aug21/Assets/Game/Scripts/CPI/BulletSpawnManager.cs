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

        /// <summary>Unlocks the next Shot Point in the array, if any remain locked. Exposed as a public
        /// method for future upgrade/UI code to call — nothing in the scene wires this up yet.</summary>
        public void UnlockNextShotPoint()
        {
            if (m_ShotPoints == null || m_UnlockedShotPointCount >= m_ShotPoints.Length)
                return;

            m_UnlockedShotPointCount++;
        }

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

            BulletProjectile bullet = Instantiate(m_BulletPrefab, shotPoint.position, Quaternion.identity, transform);
            bullet.Initialize(shotPoint.up, m_Speed);
            return true;
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
