using Coffee.UIExtensions;
using DG.Tweening;
using Oxtail.Utils;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Oxtail.SpaceshipIncremental
{
    public abstract class UpgradeButton : MonoBehaviour
    {
        [SerializeField] protected Button m_Button;

        [Header("Cost")]
        [SerializeField] private TMP_Text m_CostText;
        [SerializeField] private Image m_MoneyIcon;

        [Header("Slider")]
        [SerializeField] protected Slider m_ProgressSlider;
        [SerializeField] protected GameObject m_FullSliderBackground;
        [SerializeField] protected GameObject m_FillSliderImage;
        [SerializeField] protected GameObject m_FillDisabledSliderImage;

        [Header("VR Free Upgrade")]
        [SerializeField] protected Button m_FreeUpgradeButton;

        [Header("Audio")]
        [SerializeField] private AudioClip m_ClickSound;

        [Header("Upgrade effect")]
        [SerializeField] protected Transform m_UpgradeEffectsParent;
        [SerializeField] protected UIParticle m_UpgradeEffect;

        protected bool m_IsVRFreeUpgrade;
        private List<UIParticle> m_UpgradePressedEffectPool = new();

        protected abstract BigNumber GetUpgradeCost();


        protected virtual void Awake()
        {
            m_Button.onClick.AddListener(()=> UpgradePressed());
            m_FreeUpgradeButton.onClick.AddListener(()=> ShowRV());
            LevelManager.Instance.Money.OnPropertyChanged += CheckButtonInteraction;
        }

        private void OnDestroy()
        {
            if (LevelManager.Instance != null)
                LevelManager.Instance.Money.OnPropertyChanged -= CheckButtonInteraction;

            HideRVFreeUpgrade();
        }

        private void Start()
        {
            CheckButtonState();
        }

        private void CheckButtonInteraction(BigNumber value)
        {
            CheckButtonState();
        }

        protected virtual void UpgradePressed()
        {
            m_ProgressSlider.value = 0f;

            AudioManager.Instance.PlaySound(m_ClickSound);

            var pool = m_UpgradePressedEffectPool.Where(x => !x.gameObject.activeInHierarchy);
            UIParticle effect = null;
            if (pool.Count() == 0)
            {
                effect = Instantiate(m_UpgradeEffect, transform.position, Quaternion.identity, m_UpgradeEffectsParent);
                m_UpgradePressedEffectPool.Add(effect);
            }
            else
                effect = pool.First();

            if (effect != null)
            {
                effect.transform.position = m_CostText.transform.position;
                effect.gameObject.SetActive(false);
                effect.gameObject.SetActive(true);
            }

            CheckButtonState();
        }

        protected void CheckButtonState()
        {
            SetCostText();
            SetImageState();
            SetButtonState();
        }

        private void SetImageState()
        {
            BigNumber cost = GetUpgradeCost();
            m_MoneyIcon.gameObject.SetActive(cost >= 0);
        }

        private void SetCostText()
        {
            BigNumber cost = GetUpgradeCost();
            m_CostText.text = cost < 0 ? "MAX" : cost == 0 ? "FREE" : cost.ToString();
        }

        protected virtual void SetButtonState()
        {
            if (m_IsVRFreeUpgrade)
                ShowFreeUpgrade();

            BigNumber cost = GetUpgradeCost();
            BigNumber money = LevelManager.Instance.Money.Value;
            m_Button.interactable = cost < 0 ? false : cost == 0 ? true : money >= cost;
            m_ProgressSlider.DOKill();
            m_ProgressSlider.DOValue(BigNumber.Ratio(money, cost), 0.5f);
            m_FullSliderBackground.SetActive(m_Button.interactable);
            m_FillDisabledSliderImage.SetActive(!m_Button.interactable && money >= cost);
            m_FillSliderImage.SetActive(!m_Button.interactable && money < cost);
        }

        protected virtual void ShowFreeUpgrade()
        {
            if (!Debugger.FakeRewardedVideos) return;

            if (m_FreeUpgradeButton.gameObject.activeInHierarchy)
                return;

            m_FreeUpgradeButton.gameObject.SetActive(true);
            m_IsVRFreeUpgrade = true;
            m_FreeUpgradeButton.transform.DOPunchScale(Vector3.one * 0.1f, 1f, 1)
                .SetLoops(-1);
        }

        protected void ShowRV()
        {
            m_IsVRFreeUpgrade = false;
            LevelManager.Instance.SetIsRVFreeUpgrade();
            UpgradePressed();
        }

        protected virtual void HideRVFreeUpgrade()
        {
            if (!m_FreeUpgradeButton.gameObject.activeInHierarchy)
                return;

            m_FreeUpgradeButton.transform.DOKill();
            m_FreeUpgradeButton.transform.localScale = Vector3.one;
            m_FreeUpgradeButton.gameObject.SetActive(false);
            m_IsVRFreeUpgrade = false;
        }
    }
}
