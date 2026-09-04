using DG.Tweening;
using Oxtail.Utils;

namespace Oxtail.SpaceshipIncremental
{
    public class UpgradeCircuitButton : UpgradeButton
    {
        protected override BigNumber GetUpgradeCost()
        {
            if (LevelManager.Instance.HasMoreCircuits)
                return GameUpgradesCostSO.Instance.GetCircuitUpgradeCost();
            else
            {
                HideRVFreeUpgrade();
                return -1;
            }
        }

        protected override void UpgradePressed()
        {
            LevelManager.Instance.ChangeCircuit();
            LevelManager.Instance.IncreaseCircuitLevel();

            base.UpgradePressed();
        }

        protected override void SetButtonState()
        {
            BigNumber cost = GetUpgradeCost();
            BigNumber money = LevelManager.Instance.Money.Value;
            bool canUpgrade = LevelManager.Instance.CanChangeCircuit.Value;
            m_Button.interactable = cost < 0 ? false : cost == 0 ? true : money >= cost && canUpgrade;
            m_ProgressSlider.DOKill();
            m_ProgressSlider.DOValue(BigNumber.Ratio(money, cost), 0.5f);
            m_FullSliderBackground.SetActive(m_Button.interactable);
            m_FillDisabledSliderImage.SetActive(!m_Button.interactable && money >= cost);
            m_FillSliderImage.SetActive(!m_Button.interactable && money < cost);

            if (Debugger.FakeRewardedVideos && Debugger.ShowFreeUpgradeOnPercentage > 0 && 
                money >= cost * (Debugger.ShowFreeUpgradeOnPercentage / 100f) &&
                money < cost ||
                Debugger.FreeUpgradeCircuitPercentage > 0 &&
                money >= cost * (Debugger.FreeUpgradeCircuitPercentage / 100f) &&
                money < cost)
            {
                if (Debugger.ShowCircuitUpgradeAfterLevel > 0 &&
                    SaveLoadManager.Instance.GetCircuitLevel() + 1 >= Debugger.ShowCircuitUpgradeAfterLevel &&
                    canUpgrade)
                    ShowFreeUpgrade();
            }
            else
                HideRVFreeUpgrade();
        }
    }
}
