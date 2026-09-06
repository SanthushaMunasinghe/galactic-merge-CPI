using DG.Tweening;
using System;
using TMPro;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    /// <summary>
    /// Pooled UI particle that shows a short text popup, floats up and fades out.
    /// Mirrors the FloatingText prefab: callers set text/color while inactive, then
    /// SetActive(true) triggers the animation from OnEnable, and Disabled fires when
    /// it's done so the pool can reclaim it.
    /// </summary>
    public class TextUIParticle : MonoBehaviour
    {
        [SerializeField] private TMP_Text m_Text;
        [SerializeField] private float m_MoveDistance = 150f;
        [SerializeField] private float m_Duration = 1f;

        public event Action<TextUIParticle> Disabled;

        public void SetText(string text)
        {
            m_Text.text = text;
        }

        public void SetColor(Color color)
        {
            m_Text.color = color;
        }

        private void OnEnable()
        {
            m_Text.DOFade(1f, 0f);
            transform.DOMoveY(transform.position.y + m_MoveDistance, m_Duration).SetEase(Ease.OutQuad);
            m_Text.DOFade(0f, m_Duration).SetEase(Ease.InQuad).OnComplete(() => Disabled?.Invoke(this));
        }

        private void OnDisable()
        {
            m_Text.DOKill();
            transform.DOKill();
        }
    }
}
