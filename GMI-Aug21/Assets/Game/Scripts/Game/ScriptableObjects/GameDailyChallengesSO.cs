using Oxtail.Utils;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    [CreateAssetMenu(fileName = "GameDailyChallenges", menuName = "Incremental/Game/Game Daily Challenges")]
    public class GameDailyChallengesSO : ScriptableObjectSingleton<GameDailyChallengesSO>
    {
        [SerializeField] private DailyChallengeSO[] m_DailyChallenges;

        public int GetLastIndex()
        {
            int index = 0;

            foreach (var dailyChallenge in m_DailyChallenges)
            {
                if (dailyChallenge.ChallengeIndex > index)
                    index = dailyChallenge.ChallengeIndex;
            }

            return index;
        }

        public DailyChallengeSO GetDailyChallengeByIndex(int index)
        {
            index = Mathf.Clamp(index, 0, m_DailyChallenges.Length - 1);
            return m_DailyChallenges[index];
        }
    }
}
