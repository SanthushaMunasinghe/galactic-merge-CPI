using DG.Tweening;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    /// <summary>
    /// Attach to the camera. Shakes this transform once CPIManager's fail sequence has cleared the
    /// remaining asteroids, right before its fail delay starts.
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
                CPIManager.Instance.OnFailShake += Shake;
        }

        private void OnDisable()
        {
            if (CPIManager.Instance != null)
                CPIManager.Instance.OnFailShake -= Shake;
        }

        private void Shake()
        {
            transform.DOShakePosition(m_Duration, m_Strength, m_Vibrato, m_Randomness);
        }
    }
}
