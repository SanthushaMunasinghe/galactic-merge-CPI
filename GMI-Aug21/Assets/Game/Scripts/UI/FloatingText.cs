using DG.Tweening;
using Oxtail.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    public struct DestroyFloatingTextEvent { }

    public class FloatingText : MonoBehaviour
    {
        [SerializeField] private TMP_Text m_AmountText;

        private Material m_FontMaterial;

        public event Action<FloatingText> Disabled;

        private void Awake()
        {
            EventManager<ShowLevelCompleteEvent>.AddListener(OnLevelComplete);
            EventManager<DestroyFloatingTextEvent>.AddListener(OnLevelDestroyed);

            m_FontMaterial = m_AmountText.fontMaterial;
        }

        private void OnEnable()
        {
            m_AmountText.DOFade(1f, 0f);
            //transform.RectTransform().DOAnchorPosY(transform.RectTransform().anchoredPosition.y + 1, 1f);
            transform.DOPunchScale(Vector3.one * 0.1f, 0.25f);
            Vector3 jumpPos = transform.position + new Vector3(UnityEngine.Random.Range(-1f, 1f), 0.5f, 0f);
            transform.DOJump(jumpPos, 0.1f, 1, 1f);
            m_AmountText.DOFade(0f, 1f).SetEase(Ease.InExpo).OnComplete(() => Disabled?.Invoke(this));
        }

        private void OnDisable()
        {
            m_AmountText.DOKill();
            transform.DOKill();
            transform.RectTransform().DOKill();
        }

        private void OnDestroy()
        {
            m_AmountText.DOKill();
            transform.DOKill();
            transform.RectTransform().DOKill();
            Destroy(m_FontMaterial);

            EventManager<ShowLevelCompleteEvent>.RemoveListener(OnLevelComplete);
            EventManager<DestroyFloatingTextEvent>.RemoveListener(OnLevelDestroyed);
        }

        private void OnLevelComplete(ShowLevelCompleteEvent evt)
        {
            gameObject.SetActive(false);
        }

        private void OnLevelDestroyed(DestroyFloatingTextEvent evt)
        {
            gameObject.SetActive(false);
        }

        public void SetText(string text)
        {
            m_AmountText.text = text;
        }

        public void SetColor(Color color)
        {
            m_AmountText.color = color;
            m_FontMaterial.SetColor("_GlowColor", color);
        }
    }
}
