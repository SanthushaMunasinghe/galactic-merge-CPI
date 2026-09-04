using Oxtail.Utils;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    public enum AchievementType
    {
        LoginDays,
        AddSpaceships,
        MergeSpaceships,
        AddRewardLines,
        UpgradeCircuits,
        SpeedBoostUses,
        GiftRewards,
        HealPlanet,
        TotalHazardDamage,
        EndlessModeTimes,
        DailyChallengeTimes,
        SpaceshipTier,
        PowerUpLevel,
        SpendMoney,
        SpendGems
    }

    [System.Serializable]
    public struct AchievementGoal
    {
        public BigNumber Goal;
        public GameRewardInfo GameRewardInfo;
    }

    [CreateAssetMenu(fileName = "Achievement", menuName = "Incremental/Achievement")]
    public class AchievementSO : ScriptableObject
    {
        [SerializeField] private string m_AchievementID = "Achievement_";
        [SerializeField] private string m_AchievementDescription;
        [SerializeField] private Sprite m_AchievementIcon;
        [SerializeField] private AchievementType m_Type;
        [SerializeField] private AchievementGoal[] m_Goals;

        public string AchievementID => m_AchievementID;
        public string AchievementDescription => m_AchievementDescription;
        public Sprite AchievementIcon => m_AchievementIcon;
        public AchievementType Type => m_Type;
        public AchievementGoal[] Goals => m_Goals;

        public AchievementGoal GetAchievementGoalByIndex(int index)
        {
            return m_Goals[index];
        }
    }
}
