using Oxtail.Utils;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    [CreateAssetMenu(fileName = "GameDailyRewards", menuName = "Incremental/Game/Game Daily Rewards")]
    public class GameDailyRewardsSO : ScriptableObjectSingleton<GameDailyRewardsSO>
    {
        [SerializeField] private DailyLoginRewardsSO[] m_LoginRewards;

        public int MaxWeekIndex => m_LoginRewards.Length - 1;

        public DailyLoginRewardsSO GetLoginWeekRewards(int week)
        {
            week = Mathf.Clamp(week, 0, m_LoginRewards.Length - 1);
            return m_LoginRewards[week];
        }
    }
}
