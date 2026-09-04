using DG.Tweening;
using Oxtail.Utils;

namespace Oxtail.SpaceshipIncremental
{
    public class AddSpaceshipButton : UpgradeButton
    {
        private float m_AddSpaceshipDiscount;

        protected override void Awake()
        {
            base.Awake();

            CheckAddSpaceshipDiscount();

            SaveLoadManager.Instance.OnPowerUpUpdated += OnPowerUpUpdated;
        }

        private void OnDestroy()
        {
            SaveLoadManager.Instance.OnPowerUpUpdated -= OnPowerUpUpdated;
        }

        private void OnPowerUpUpdated(string powerUpID)
        {
            CheckAddSpaceshipDiscount();
        }

        private void CheckAddSpaceshipDiscount()
        {
            var powerup = GamePowerUpsSO.Instance.GetPowerUpByType(PowerUpType.CreationDiscount);
            int level = SaveLoadManager.Instance.GetPowerUpLevel(powerup.PowerUpID);
            if (level > 0)
                m_AddSpaceshipDiscount = powerup.GetUpgradeByLevel(level).Value;
            else
                m_AddSpaceshipDiscount = 0f;
        }

        protected override BigNumber GetUpgradeCost()
        {
            if (LevelManager.Instance.CanSpawnSpaceship.Value)
            {
                BigNumber cost = GameUpgradesCostSO.Instance.GetAddSpaceshipUpgradeCost();
                cost *= 1f - (m_AddSpaceshipDiscount / 100f);
                return cost;
            }
            else
            {
                HideRVFreeUpgrade();
                return -1;
            }
        }

        protected override void UpgradePressed()
        {
            BigNumber cost = GameUpgradesCostSO.Instance.GetAddSpaceshipUpgradeCost();
            LevelManager.Instance.RemoveMoney(cost);

            LevelManager.Instance.AddDefaultSpaceship();
            LevelManager.Instance.IncreaseAddSpaceshipLevel();

            base.UpgradePressed();
        }

        protected override void SetButtonState()
        {
            BigNumber cost = GetUpgradeCost();
            BigNumber money = LevelManager.Instance.Money.Value;
            bool canSpawn = LevelManager.Instance.CanSpawnSpaceship.Value;
            m_Button.interactable = cost < 0 ? false : cost == 0 ? true : money >= cost && canSpawn;

            if (LevelManager.Instance is StandardLevelManager &&
                LevelManager.Instance.LevelIndex == 0 &&
                LevelManager.Instance.LevelProgressionStepIndex == 1)
            {
                m_Button.interactable = SaveLoadManager.Instance.GetMergeLevel() >= 1 && money >= cost;
            }

            m_ProgressSlider.DOKill();
            m_ProgressSlider.DOValue(BigNumber.Ratio(money, cost), 0.5f);
            m_FullSliderBackground.SetActive(m_Button.interactable);
            m_FillDisabledSliderImage.SetActive(!m_Button.interactable && money >= cost);
            m_FillSliderImage.SetActive(!m_Button.interactable && money < cost);

            if (Debugger.FakeRewardedVideos && Debugger.ShowFreeUpgradeOnPercentage > 0 &&
                money >= cost * (Debugger.ShowFreeUpgradeOnPercentage / 100f) &&
                money < cost ||
                Debugger.FreeUpgradeSpaceshipPercentage > 0 &&
                money >= cost * (Debugger.FreeUpgradeSpaceshipPercentage / 100f) &&
                money < cost)
            {
                if (Debugger.ShowSpaceshipUpgradeAfterLevel > 0 &&
                    SaveLoadManager.Instance.GetAddSpaceshipLevel() + 1 >= Debugger.ShowSpaceshipUpgradeAfterLevel &&
                    canSpawn)
                    ShowFreeUpgrade();
            }
            else
                HideRVFreeUpgrade();
        }
    }
}