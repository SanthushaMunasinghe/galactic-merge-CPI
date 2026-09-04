using DG.Tweening;
using Oxtail.Utils;

namespace Oxtail.SpaceshipIncremental
{
    public class MergeButton : UpgradeButton
    {
        private bool m_IsFreeMergeActive;

        private float m_MergeDiscount;

        protected override void Awake()
        {
            base.Awake();
            
            CheckMergeDiscount();

            LevelManager.Instance.CanMerge.OnPropertyChanged += CheckButtonState;
            SaveLoadManager.Instance.OnPowerUpUpdated += OnPowerUpUpdated;
            EventManager<ShowFreeMerge>.AddListener(OnShowFreeMerge);
        }

        private void OnDestroy()
        {
            if (LevelManager.Instance != null)
                LevelManager.Instance.CanMerge.OnPropertyChanged -= CheckButtonState;

            SaveLoadManager.Instance.OnPowerUpUpdated -= OnPowerUpUpdated;
            EventManager<ShowFreeMerge>.RemoveListener(OnShowFreeMerge);
        }

        private void OnPowerUpUpdated(string powerUpID)
        {
            CheckMergeDiscount();
        }

        private void CheckMergeDiscount()
        {
            var powerup = GamePowerUpsSO.Instance.GetPowerUpByType(PowerUpType.CreationDiscount);
            int level = SaveLoadManager.Instance.GetPowerUpLevel(powerup.PowerUpID);
            if (level > 0)
                m_MergeDiscount = powerup.GetUpgradeByLevel(level).Value;
            else
                m_MergeDiscount = 0f;
        }

        private void OnShowFreeMerge(ShowFreeMerge eventData)
        {
            m_IsFreeMergeActive = true;
            CheckButtonState();

        }

        private void CheckButtonState(bool canMerge)
        {
            if (!LevelManager.Instance.CanMerge.Value)
                HideRVFreeUpgrade();

            SetButtonState();
        }

        protected override void SetButtonState()
        {
            BigNumber cost = GetUpgradeCost();
            BigNumber money = LevelManager.Instance.Money.Value;
            bool canMerge = LevelManager.Instance.CanMerge.Value;
            m_Button.interactable = cost < 0 ? false : cost == 0 ? canMerge : money >= cost && canMerge;
            m_ProgressSlider.DOKill();
            m_ProgressSlider.DOValue(BigNumber.Ratio(money, cost), 0.5f);
            m_FullSliderBackground.SetActive(m_Button.interactable);
            m_FillDisabledSliderImage.SetActive(!m_Button.interactable && money >= cost);
            m_FillSliderImage.SetActive(!m_Button.interactable && money < cost);

            if (Debugger.FakeRewardedVideos && Debugger.ShowFreeUpgradeOnPercentage > 0 &&
                money >= cost * (Debugger.ShowFreeUpgradeOnPercentage / 100f) &&
                money < cost ||
                Debugger.FreeUpgradeMergePercentage > 0 &&
                money >= cost * (Debugger.FreeUpgradeMergePercentage / 100f) &&
                money < cost)
            {
                if (Debugger.ShowMergeUpgradeAfterLevel > 0 &&
                    SaveLoadManager.Instance.GetMergeLevel() + 1 >= Debugger.ShowMergeUpgradeAfterLevel &&
                    canMerge)
                    ShowFreeUpgrade();
            }
            else
                HideRVFreeUpgrade();
        }

        protected override BigNumber GetUpgradeCost()
        {
            BigNumber cost = GameUpgradesCostSO.Instance.GetMergeUpgradeCost();
            cost *= 1f - (m_MergeDiscount / 100f);

            return m_IsFreeMergeActive ? 0 : cost;
        }

        protected override void UpgradePressed()
        {
            LevelManager.Instance.MergeSpaceship();
            LevelManager.Instance.IncreaseMergeSpaceshipLevel();

            if (m_IsFreeMergeActive)
                m_IsFreeMergeActive = false;

            base.UpgradePressed();
        }
    }
}
