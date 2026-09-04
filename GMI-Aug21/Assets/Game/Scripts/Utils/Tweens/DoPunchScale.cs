using DG.Tweening;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Oxtail.Utils
{
    public class DoPunchScale : MonoBehaviour
    {
        [SerializeField] private Vector3 m_Punch;
        [SerializeField] private float m_Duration;
        [SerializeField] private int m_Vibrato;
        [SerializeField] private Ease m_Ease;
        [SerializeField] private int m_Loops;
        [SerializeField, ShowIf(nameof(m_HasLoops))]
        private LoopType m_LoopType; 

        private bool m_HasLoops => m_Loops != 0;

        private Tween m_PunchScaleTween;

        private void OnEnable()
        {
            if (m_PunchScaleTween != null)
                return;

            transform.localScale = Vector3.one;

            m_PunchScaleTween = transform.DOPunchScale(m_Punch, m_Duration, m_Vibrato).SetEase(m_Ease).SetLoops(m_Loops, m_LoopType);
        }

        private void OnDisable()
        {
            m_PunchScaleTween.Kill();
            m_PunchScaleTween = null;
        }
    }
}
