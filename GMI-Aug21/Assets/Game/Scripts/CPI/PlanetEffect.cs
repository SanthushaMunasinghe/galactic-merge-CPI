using DG.Tweening;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    /// <summary>
    /// Plays a scale pulse on the assigned target whenever CPIManager reports a planet hit, an
    /// upgrade, or a planet health gain: target scale eases to Initial Scale * First Scale Multiplier,
    /// then to Initial Scale * Second Scale Multiplier, then back to Initial Scale. A multiplier below
    /// 1 shrinks, above 1 grows. A trigger received while the pulse is already playing is ignored.
    /// </summary>
    public class PlanetEffect : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform m_TargetObject;

        [Header("Scale Effect")]
        [SerializeField] private float m_FirstScaleMultiplier = 0.9f;
        [SerializeField] private float m_SecondScaleMultiplier = 1.05f;
        [SerializeField, Min(0f)] private float m_FirstStageDuration = 0.1f;
        [SerializeField, Min(0f)] private float m_SecondStageDuration = 0.1f;
        [SerializeField, Min(0f)] private float m_ReturnDuration = 0.1f;
        [SerializeField] private Ease m_Ease = Ease.OutQuad;

        private Vector3 m_InitialScale;
        private bool m_IsPlaying;

        private void Awake()
        {
            if (m_TargetObject == null)
                m_TargetObject = transform;

            m_InitialScale = m_TargetObject.localScale;
        }

        private void OnEnable()
        {
            if (CPIManager.Instance != null)
            {
                CPIManager.Instance.OnPlanetHit += PlayEffect;
                CPIManager.Instance.OnUpgradePerformed += PlayEffect;
                CPIManager.Instance.OnPlanetHealthGained += PlayEffect;
            }
        }

        private void OnDisable()
        {
            if (CPIManager.Instance != null)
            {
                CPIManager.Instance.OnPlanetHit -= PlayEffect;
                CPIManager.Instance.OnUpgradePerformed -= PlayEffect;
                CPIManager.Instance.OnPlanetHealthGained -= PlayEffect;
            }
        }

        private void PlayEffect()
        {
            if (m_IsPlaying)
                return;

            m_IsPlaying = true;

            m_TargetObject.DOKill();
            DOTween.Sequence()
                .Append(m_TargetObject.DOScale(m_InitialScale * m_FirstScaleMultiplier, m_FirstStageDuration).SetEase(m_Ease))
                .Append(m_TargetObject.DOScale(m_InitialScale * m_SecondScaleMultiplier, m_SecondStageDuration).SetEase(m_Ease))
                .Append(m_TargetObject.DOScale(m_InitialScale, m_ReturnDuration).SetEase(m_Ease))
                .OnComplete(() => m_IsPlaying = false);
        }
    }
}
