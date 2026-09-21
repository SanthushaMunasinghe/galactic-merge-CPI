using DG.Tweening;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    /// <summary>
    /// Attach to the camera next to its CameraFollow. Shakes the camera once CPIManager's fail sequence
    /// has cleared the remaining asteroids, right before its fail delay starts. The shake is applied
    /// through CameraFollow.ShakeOffset instead of moving the transform directly, because CameraFollow
    /// rewrites the camera's position every frame and would otherwise swallow it.
    /// </summary>
    [RequireComponent(typeof(CameraFollow))]
    public class CameraShake : MonoBehaviour
    {
        [Header("CPI Fail Shake")]
        [SerializeField, Min(0f)] private float m_Duration = 0.3f;
        [SerializeField, Min(0f)] private float m_Strength = 0.5f;
        [SerializeField, Min(0)] private int m_Vibrato = 10;
        [SerializeField, Range(0f, 90f)] private float m_Randomness = 90f;

        private CameraFollow m_Follow;
        private Tween m_ShakeTween;

        private void Awake()
        {
            m_Follow = GetComponent<CameraFollow>();
        }

        private void OnEnable()
        {
            if (CPIManager.Instance != null)
                CPIManager.Instance.OnFailShake += Shake;
        }

        private void OnDisable()
        {
            if (CPIManager.Instance != null)
                CPIManager.Instance.OnFailShake -= Shake;

            m_ShakeTween?.Kill();
            m_Follow.ShakeOffset = Vector3.zero;
        }

        private void Shake()
        {
            m_ShakeTween?.Kill();
            m_Follow.ShakeOffset = Vector3.zero;

            m_ShakeTween = DOTween.Shake(
                () => Vector3.zero,
                offset => m_Follow.ShakeOffset = offset,
                m_Duration, m_Strength, m_Vibrato, m_Randomness);
        }
    }
}
