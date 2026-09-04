using Oxtail.Utils;
using System.Linq;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    [CreateAssetMenu(fileName = "GameAchievements", menuName = "Incremental/Game/Game Achievements")]
    public class GameAchievementsSO : ScriptableObjectSingleton<GameAchievementsSO>
    {
        [SerializeField] private AchievementSO[] m_Achievements;

        public AchievementSO[] Achievements => m_Achievements;

        public AchievementSO GetAchievementByID(string id)
        {
            return m_Achievements.FirstOrDefault(x => x.AchievementID == id);
        }

        public AchievementSO GetAchievementByType(AchievementType type)
        {
            return m_Achievements.FirstOrDefault(x => x.Type == type);
        }

        public int CompletedAchievementsPercentage()
        {
            int totalAchievements = 0;
            int completedAchievements = 0;

            foreach (var achievement in m_Achievements)
            {
                totalAchievements += achievement.Goals.Length;
                completedAchievements += SaveLoadManager.Instance.GetAchievementGoalIndex(achievement.AchievementID);
            }

            return Mathf.RoundToInt((float)completedAchievements / totalAchievements * 100f);
        }
    }
}
