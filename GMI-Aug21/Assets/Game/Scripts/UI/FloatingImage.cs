using DG.Tweening;
using Oxtail.Utils;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Oxtail.SpaceshipIncremental
{
    public class FloatingImage : MonoBehaviour
    {
        [SerializeField] private Image m_FloatingImage;

        private void OnEnable()
        {
            transform.RectTransform().DOAnchorPosY(transform.RectTransform().anchoredPosition.y + 1, 1f);
            m_FloatingImage.DOFade(0f, 1f).SetEase(Ease.InExpo).OnComplete(() => Destroy(gameObject));
        }

        public void SetImage(Sprite sprite)
        {
            m_FloatingImage.sprite = sprite;
        }
    }
}
