using Oxtail.Utils;

namespace Oxtail.SpaceshipIncremental
{
    public class AddRewardLineButton : UpgradeButton
    {
        private UIFlyToParticles m_UIFlyToParticles;

        protected override void Awake()
        {
            base.Awake();
            
            m_UIFlyToParticles = GetComponent<UIFlyToParticles>();
        }

        protected override BigNumber GetUpgradeCost()
        {
            if (LevelManager.Instance.CanSpawnRewardLine)
            {
                BigNumber cost = GameUpgradesCostSO.Instance.GetRewardLineUpgradeCost();
                BigNumber money = LevelManager.Instance.Money.Value;
                if (Debugger.FakeRewardedVideos && Debugger.ShowFreeUpgradeOnPercentage > 0 && 
                    money >= cost * (Debugger.ShowFreeUpgradeOnPercentage / 100f) && 
                    money < cost ||
                    Debugger.FreeUpgradeRewardLinePercentage > 0 &&
                    money >= cost * (Debugger.FreeUpgradeRewardLinePercentage / 100f) &&
                    money < cost)
                {
                    if (Debugger.ShowLineUpgradeAfterLevel > 0 &&
                        SaveLoadManager.Instance.GetRewardLineLevel() + 1 >= Debugger.ShowLineUpgradeAfterLevel)
                        ShowFreeUpgrade();
                }
                else
                    HideRVFreeUpgrade();

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
            LevelManager.Instance.AddRewardLine();
            LevelManager.Instance.IncreaseRewardLineLevel();

            EventManager<RewardLineAddedEvent>.TriggerEvent();

            base.UpgradePressed();
        }
    }
}
