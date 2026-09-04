using Oxtail.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    [CreateAssetMenu(fileName = "SO_GameUpgradesCost", menuName = "Incremental/Game/Upgrades Cost")]
    public class GameUpgradesCostSO : ScriptableObjectSingleton<GameUpgradesCostSO>
    {
        [SerializeField] private int m_MergeBaseCost;
        [SerializeField] private int m_AddSpaceshipBaseCost;
        [SerializeField] private int m_RewardLineBaseCost;
        [SerializeField] private int m_CircuitBaseCost;

        public BigNumber GetMergeUpgradeCost()
        {
            int level = 0;
            if (LevelManager.Instance is CPIManager cpiManager)
                level = cpiManager.MergeLevel;
            else if (LevelManager.Instance is StandardLevelManager)
                level = SaveLoadManager.Instance.GetMergeLevel();
            else if (LevelManager.Instance is DailyChallengeLevelManager)
                level = SaveLoadManager.Instance.GetDailyChallengeMergeLevel();
            else if (LevelManager.Instance is EndlessModeLevelManager)
                level = SaveLoadManager.Instance.GetEndlessModeMergeLevel();

            if (level == 0)
                return m_MergeBaseCost;

            return Mathf.CeilToInt(m_MergeBaseCost * Mathf.Pow(level, 1.25f));
        }

        public BigNumber GetAddSpaceshipUpgradeCost()
        {
            int level = 0;
            if (LevelManager.Instance is CPIManager cpiManager)
                level = cpiManager.AddSpaceshipLevel;
            else if (LevelManager.Instance is StandardLevelManager)
                level = SaveLoadManager.Instance.GetAddSpaceshipLevel();
            else if (LevelManager.Instance is DailyChallengeLevelManager)
                level = SaveLoadManager.Instance.GetDailyChallengeAddSpaceshipLevel();
            else if (LevelManager.Instance is EndlessModeLevelManager)
                level = SaveLoadManager.Instance.GetEndlessModeAddSpaceshipLevel();

            if (level == 0)
                return m_AddSpaceshipBaseCost;

            return Mathf.CeilToInt(m_AddSpaceshipBaseCost * Mathf.Pow(level, 1.1f));
        }

        public BigNumber GetRewardLineUpgradeCost()
        {
            int level = 0;
            if (LevelManager.Instance is CPIManager cpiManager)
                level = cpiManager.RewardLineLevel;
            else if (LevelManager.Instance is StandardLevelManager)
                level = SaveLoadManager.Instance.GetRewardLineLevel();
            else if (LevelManager.Instance is DailyChallengeLevelManager)
                level = SaveLoadManager.Instance.GetDailyChallengeRewardLineLevel();
            else if (LevelManager.Instance is EndlessModeLevelManager)
                level = SaveLoadManager.Instance.GetEndlessModeRewardLineLevel();

            if (level == 0)
                return m_RewardLineBaseCost;

            return Mathf.CeilToInt(Mathf.Pow(m_RewardLineBaseCost * level, 1.35f));
        }

        public BigNumber GetCircuitUpgradeCost()
        {
            int level = 0;
            if (LevelManager.Instance is CPIManager cpiManager)
                level = cpiManager.CircuitLevel;
            else if (LevelManager.Instance is StandardLevelManager)
                level = SaveLoadManager.Instance.GetCircuitLevel();
            else if (LevelManager.Instance is DailyChallengeLevelManager)
                level = SaveLoadManager.Instance.GetDailyChallengeCircuitLevel();
            else if (LevelManager.Instance is EndlessModeLevelManager)
                level = SaveLoadManager.Instance.GetEndlessModeCircuitLevel();

            if (level == 0)
                return m_CircuitBaseCost;

            return Mathf.CeilToInt(Mathf.Pow(m_CircuitBaseCost * level, 1.35f));
        }
    }
}
