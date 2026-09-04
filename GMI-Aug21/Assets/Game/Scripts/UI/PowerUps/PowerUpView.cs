using Oxtail.Utils;
using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Oxtail.SpaceshipIncremental
{
    public class PowerUpView : MonoBehaviour
    {
        [SerializeField] private Image m_Icon;
        [SerializeField] private TMP_Text m_Description;
        [SerializeField] private TMP_Text m_LevelText;

        [Header("Locked")]
        [SerializeField] private GameObject m_LockedView;
        [SerializeField] private Image m_LockedIcon;

        [Header("Upgrade Button")]
        [SerializeField] private Button m_UpgradeButton;
        [SerializeField] private TMP_Text m_UpgradeAmountText;

        [Header("Materials")]
        [SerializeField] private Material m_PowerUpMaterial;
        [SerializeField] private Material m_PowerUpUnlockMaterial;

        [Header("Effects")]
        [SerializeField] private GameObject m_UpgradeEffect;

        private PowerUpSO m_PowerUpData;
        private int m_CurrentLevel;

        public bool CanUpgrade => m_UpgradeButton.interactable;
        public Button UpgradeButton => m_UpgradeButton;

        private const float m_DefaultLockValue = 0.1f;
        private const float m_FullUnlockValue = -3.6f;

        private void Awake()
        {
            m_UpgradeButton.onClick.AddListener(OnUpgradeButtonClicked);

            SaveLoadManager.Instance.OnGemsUpdated += OnGemsUpdated;
        }

        private void OnDestroy()
        {
            SaveLoadManager.Instance.OnGemsUpdated -= OnGemsUpdated;
        }

        private void OnEnable()
        {
            SetButtonInfo();
            UpdateFrame();
        }

        private void OnGemsUpdated()
        {
            SetButtonInfo();
            UpdateFrame();
        }

        public void SetData(PowerUpSO powerUp)
        {
            m_PowerUpData = powerUp;
            m_Icon.sprite = powerUp.PowerUpIcon;
            m_Icon.material = new Material(m_PowerUpMaterial);
            m_LockedIcon.sprite = powerUp.PowerUpIcon;
            m_LockedIcon.material = new Material(m_PowerUpUnlockMaterial);
            m_CurrentLevel = SaveLoadManager.Instance.GetPowerUpLevel(m_PowerUpData.PowerUpID);
            int percentage = SaveLoadManager.Instance.GetPowerUpUnlockPercentage(m_PowerUpData.PowerUpID);
            m_LockedIcon.gameObject.SetActive(percentage < 100f);
            m_LockedView.SetActive(percentage == 0 && m_CurrentLevel == 0);
            m_LevelText.gameObject.SetActive(m_CurrentLevel > 0);
            if (m_CurrentLevel > 0)
                m_LevelText.text = $"Lv {m_CurrentLevel}";

            UpdateDescription();
            UpdateFill();
            SetButtonInfo();
        }

        public void UpdateData()
        {
            m_Icon.sprite = m_PowerUpData.PowerUpIcon;
            m_CurrentLevel = SaveLoadManager.Instance.GetPowerUpLevel(m_PowerUpData.PowerUpID);
            int percentage = SaveLoadManager.Instance.GetPowerUpUnlockPercentage(m_PowerUpData.PowerUpID);
            m_LockedIcon.gameObject.SetActive(percentage < 100f);
            m_LockedView.SetActive(percentage == 0 && m_CurrentLevel == 0);

            UpdateDescription();
            UpdateFill();
            SetButtonInfo();
        }

        private void OnUpgradeButtonClicked()
        {
            m_CurrentLevel++;
            SaveLoadManager.Instance.SavePowerUpLevel(m_PowerUpData.PowerUpID, m_CurrentLevel);
            int cost = m_PowerUpData.GetUpgradeByLevel(m_CurrentLevel).Cost;
            SaveLoadManager.Instance.AddGems(-cost);
            SetButtonInfo();
            UpdateDescription();
            m_LevelText.gameObject.SetActive(true);
            m_LevelText.text = $"Lv {m_CurrentLevel}";

            Color textColor = m_LevelText.color;
            m_LevelText.color = Color.green;
            m_LevelText.transform.DOPunchScale(Vector3.one * 0.1f, 0.1f)
                .OnComplete(() => m_LevelText.color = textColor);
            m_Icon.transform.DOPunchScale(Vector3.one * 0.1f, 0.1f);

            m_UpgradeEffect.SetActive(false);
            m_UpgradeEffect.SetActive(true);

            BigNumber maxPowerUpAchievementLevel = SaveLoadManager.Instance.GetAchievementProgression(AchievementType.PowerUpLevel);
            if (m_CurrentLevel > maxPowerUpAchievementLevel)
                SaveLoadManager.Instance.SaveAchievementProgression(AchievementType.PowerUpLevel, m_CurrentLevel);
        }

        private void SetButtonInfo()
        {
            if (m_PowerUpData.IsMaxLevel(m_CurrentLevel))
            {
                m_UpgradeAmountText.text = "MAX";
                m_UpgradeButton.interactable = false;
            }
            else
            {
                PowerUpUpgrade nextUpgrade = m_PowerUpData.GetUpgradeByLevel(m_CurrentLevel + 1);
                m_UpgradeAmountText.text = NumberFormatter.FormatValue(nextUpgrade.Cost);

                BigNumber gems = SaveLoadManager.Instance.GetGems();
                m_UpgradeButton.interactable = m_CurrentLevel > 0 && gems >= nextUpgrade.Cost;
            }
        }

        private void UpdateDescription()
        {
            m_Description.text = m_PowerUpData.PowerUpDescription;

            if (m_CurrentLevel > 0)
            {
                PowerUpUpgrade upgradeInfo = m_PowerUpData.GetUpgradeByLevel(m_CurrentLevel);

                m_Description.text += $": {upgradeInfo.Value}";
                m_Description.text += m_PowerUpData.PowerUpType switch
                {
                    PowerUpType.Velocity => "%",
                    PowerUpType.Coins => "%",
                    PowerUpType.CreationDiscount => "%",
                    PowerUpType.MergeDiscount => "%",
                    PowerUpType.CriticalCreation => "%",
                    PowerUpType.CriticalMerge => "%",
                    PowerUpType.SpeedBoost => "s",
                    PowerUpType.RewardChance => "%",
                    PowerUpType.PassiveIncome => "",
                    PowerUpType.OfflineRewards => ""
                };
            }
        }

        private void UpdateFrame()
        {
            if (m_CurrentLevel > 0)
            {
                m_Icon.material.SetColor("_OuterOutlineColor", SpaceshipTiers.Instance.GetTier(m_CurrentLevel).SpaceShipColor);
                m_Icon.material.SetFloat("_OuterOutlineFade", 1f);
            }
        }

        private void UpdateFill()
        {
            int percentage = SaveLoadManager.Instance.GetPowerUpUnlockPercentage(m_PowerUpData.PowerUpID);
            float value = Mathf.Lerp(m_DefaultLockValue, m_FullUnlockValue, percentage / 100f);
            m_LockedIcon.material.SetFloat("_DirectionalGlowFadeFade", value);
        }
    }
}