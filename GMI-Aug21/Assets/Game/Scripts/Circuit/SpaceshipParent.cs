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

        private float m_Velocity = 1.25f;
        private Vector3 m_NextWaypoint;
        private Vector3 m_DefaultPos;

        private Tween m_PathTween;
        private float m_SpeedMultiplier = 1f;

        private bool m_IsInSpeedBoost;

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

        public void SetPath(Vector3[] path, PathType pathType)
        {
            m_NextWaypoint = path[0];

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
            Vector3 dir = (m_NextWaypoint - transform.position).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.eulerAngles = new Vector3(0, 0, angle - 90f);
        }

        private float CheckNextWaypointRotation(Vector2 waypoint)
        {
            Vector3 dir = (m_NextWaypoint - transform.position).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            float finalAngle = Mathf.LerpAngle(transform.eulerAngles.z, angle - 90f, 5f * Time.deltaTime);
            return finalAngle;
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
