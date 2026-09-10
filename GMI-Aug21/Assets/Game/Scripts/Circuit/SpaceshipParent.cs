using DG.Tweening;
using Oxtail.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    public class SpaceshipParent : MonoBehaviour
    {
        [SerializeField] private Transform m_InitialWaypoint;
        [SerializeField, Min(0.01f)] private float m_RotationSmoothTime = 0.15f;

        private float m_Velocity = 1.25f;
        private Vector3 m_NextWaypoint;
        private Vector3 m_DefaultPos;

        private Tween m_PathTween;
        private float m_SpeedMultiplier = 1f;

        private bool m_IsInSpeedBoost;

        private bool m_AlignWithPathLocal;
        private float m_RotationOffsetDegrees;
        private float m_RotationVelocity;

        public bool IsFree => Spaceship == null;
        public Spaceship Spaceship { get; private set; }
        public Transform InitialWaypoint => m_InitialWaypoint;

        public float Velocity => m_PathTween != null ? m_PathTween.timeScale * m_Velocity : m_Velocity;

        private void Awake()
        {
            m_DefaultPos = transform.position;
        }

        private void OnEnable()
        {
            SaveLoadManager.Instance.OnPowerUpUpdated += OnPowerUpUpdated;
        }

        private void OnDisable()
        {
            DisablePath();
            SaveLoadManager.Instance.OnPowerUpUpdated -= OnPowerUpUpdated;
        }

        public void SetPath(Vector3[] path, PathType pathType, bool alignWithPathLocal = false, float rotationOffsetDegrees = 0f, float rotationSmoothTimeOverride = -1f)
        {
            m_NextWaypoint = path[0];
            m_AlignWithPathLocal = alignWithPathLocal;
            m_RotationOffsetDegrees = rotationOffsetDegrees;

            if (rotationSmoothTimeOverride >= 0f)
                m_RotationSmoothTime = rotationSmoothTimeOverride;

            m_RotationVelocity = 0f;

            SetInitialRotation();

            m_PathTween = transform.DOPath(path, m_Velocity, pathType, PathMode.TopDown2D)
                .SetOptions(true)
                .OnWaypointChange((index) =>
                {
                    int nextIndex = index + 1;
                    if (nextIndex >= path.Length)
                        nextIndex = 0;

                    m_NextWaypoint = path[nextIndex];
                }).SetSpeedBased(true)
            .OnUpdate(() =>
            {
                if (m_AlignWithPathLocal)
                    transform.localEulerAngles = new Vector3(0, 0, CheckNextWaypointRotationLocal());
                else
                    transform.eulerAngles = new Vector3(0, 0, CheckNextWaypointRotation(m_NextWaypoint));
            })
            .SetEase(Ease.Linear)
            .SetLoops(-1);

            CalculateSpeedMultiplier();
        }

        public void StopPath()
        {
            m_PathTween.TogglePause();
        }

        public void ResumePath()
        {
            m_PathTween.TogglePause();
        }

        private void DisablePath()
        {
            Spaceship = null;
            m_PathTween.Kill();
            transform.position = m_DefaultPos;
        }

        private void SetInitialRotation()
        {
            if (m_AlignWithPathLocal)
            {
                transform.localEulerAngles = new Vector3(0, 0, TargetLocalZAngle());
                return;
            }

            Vector3 dir = (m_NextWaypoint - transform.position).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.eulerAngles = new Vector3(0, 0, angle - 90f);
        }

        private float CheckNextWaypointRotation(Vector2 waypoint)
        {
            Vector3 dir = (m_NextWaypoint - transform.position).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            float finalAngle = Mathf.SmoothDampAngle(transform.eulerAngles.z, angle - 90f, ref m_RotationVelocity, m_RotationSmoothTime);
            return finalAngle;
        }

        /// <summary>
        /// alignWithPathLocal rotation target, in degrees around local Z: the ship's forward is its
        /// local Y axis, and rotating around Z sweeps that forward vector to face the travel
        /// direction, using the same XY-plane formula as the default (world-space) rotation above.
        /// Computed relative to the parent's current world Z so every SpaceshipParent converges on
        /// the same absolute world-facing direction, regardless of whatever baseline rotation that
        /// particular slot (or its parent) was authored with around the track. Without this, writing
        /// an absolute local Z discards each slot's own baseline and only lines up by coincidence
        /// for whichever slot happens to have a zero-rotation parent. Local X and Y stay 0.
        /// rotationOffsetDegrees is added on top so the result can be corrected by hand if the ship
        /// model's own forward axis does not exactly match this.
        /// </summary>
        private float TargetLocalZAngle()
        {
            Vector3 dir = (m_NextWaypoint - transform.position).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            float desiredWorldZ = angle - 90f + m_RotationOffsetDegrees;

            float parentWorldZ = transform.parent != null ? transform.parent.eulerAngles.z : 0f;
            return desiredWorldZ - parentWorldZ;
        }

        private float CheckNextWaypointRotationLocal()
        {
            return Mathf.SmoothDampAngle(transform.localEulerAngles.z, TargetLocalZAngle(), ref m_RotationVelocity, m_RotationSmoothTime);
        }

        public void AddSpaceship(Spaceship spaceship)
        {
            if (transform.childCount > 1)
                Destroy(transform.GetChild(0).gameObject);

            Spaceship = spaceship;
        }

        public void RemoveSpaceship()
        {
            Spaceship = null;
        }

        public void IncreaseSpeed()
        {
            m_PathTween.timeScale *= 2f;
            m_IsInSpeedBoost = true;
        }

        public void DecreaseSpeed()
        {
            m_PathTween.timeScale = m_SpeedMultiplier;
            m_IsInSpeedBoost = false;
        }

        /// <summary>
        /// Overrides the resting (non-boosted) speed multiplier directly, bypassing the power-up
        /// derived value CalculateSpeedMultiplier() would otherwise compute. Used by CPIManager for
        /// local testing. A temporary speed boost in progress still takes priority, matching how
        /// CalculateSpeedMultiplier already defers to m_IsInSpeedBoost.
        /// </summary>
        public void SetSpeedMultiplier(float multiplier)
        {
            m_SpeedMultiplier = multiplier;
            if (!m_IsInSpeedBoost && m_PathTween != null)
                m_PathTween.timeScale = multiplier;
        }

        private void OnPowerUpUpdated(string powerUpID)
        {
            CalculateSpeedMultiplier();
        }

        private void CalculateSpeedMultiplier()
        {
            var powerup = GamePowerUpsSO.Instance.GetPowerUpByType(PowerUpType.Velocity);
            int level = SaveLoadManager.Instance.GetPowerUpLevel(powerup.PowerUpID);
            if (level > 0)
                m_SpeedMultiplier = 1f * (1f + (powerup.GetUpgradeByLevel(level).Value / 100f));
            else
                m_SpeedMultiplier = 1;

            if (!m_IsInSpeedBoost)
                m_PathTween.timeScale = m_SpeedMultiplier;
        }
    }
}
