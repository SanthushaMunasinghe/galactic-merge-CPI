using DG.Tweening;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    /// <summary>
    /// Attach to the camera. Shakes this transform once per planet CPIManager reports as destroyed —
    /// including the last one, which also ends the game, so there is no separate "game over" shake
    /// beyond the per-planet one.
    /// </summary>
    public class CameraShake : MonoBehaviour
    {
        [Header("CPI Fail Shake")]
        [SerializeField, Min(0f)] private float m_Duration = 0.3f;
        [SerializeField, Min(0f)] private float m_Strength = 0.5f;
        [SerializeField, Min(0)] private int m_Vibrato = 10;
        [SerializeField, Range(0f, 90f)] private float m_Randomness = 90f;

        private void OnEnable()
        {
            if (CPIManager.Instance != null)
                CPIManager.Instance.OnPlanetDestroyed += Shake;
        }

        private void OnDisable()
        {
            if (CPIManager.Instance != null)
                CPIManager.Instance.OnPlanetDestroyed -= Shake;
        }

        private void Shake()
        {
            transform.DOShakePosition(m_Duration, m_Strength, m_Vibrato, m_Randomness);
        }
    }
}
