using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Oxtail.SpaceshipIncremental
{
    public class ShopPurchaseButtonWithInfo : MonoBehaviour
    {
        [SerializeField] private Image m_Icon;
        [SerializeField] private TMP_Text m_Description;
        [SerializeField] private TMP_Text m_PriceText;
        [SerializeField] private ParticleSystem m_ParticlesEffect;

        [Header("Best Value Badge")]
        [SerializeField] private GameObject m_BestValue;
        [SerializeField] private GameObject m_BestValueBadge;
        [SerializeField] private TMP_Text m_BestValueBadgeText;

        private Button m_Button;

        public Action OnPressed;

        private void Awake()
        {
            m_Button = GetComponent<Button>();
            m_Button.onClick.AddListener(() => OnPressed?.Invoke());
        }

        private void OnDestroy()
        {
            OnPressed = null;
        }

        public void SetIcon(Sprite icon)
        {
            m_Icon.sprite = icon;
        }

        public void SetIconMaterial(Material mat)
        {
            m_Icon.material = mat;
        }

        public void SetDescription(string description)
        {
            m_Description.text = description;
        }

        public void SetPrice(string price)
        {
            m_PriceText.text = price;
        }

        public void SetParticlesColor(Color color)
        {
            var effect = m_ParticlesEffect.main;
            effect.startColor = color;
        }

        public void ActivateParticles()
        {
            m_ParticlesEffect.gameObject.SetActive(true);
        }

        public void ShowBestValueBadge()
        {
            m_BestValueBadge.transform.DOLocalRotate(new Vector3(0, 0, -360), 10f, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart);

            m_BestValueBadge.SetActive(true);
            m_BestValue.SetActive(true);
        }
    }
}
