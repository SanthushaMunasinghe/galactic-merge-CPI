using Oxtail.Utils;
using UnityEngine;
using System.Linq;

namespace Oxtail.SpaceshipIncremental
{
    [CreateAssetMenu(fileName = "GameLevels", menuName = "Incremental/Game/Game Levels")]
    public class GameLevelsSO : ScriptableObjectSingleton<GameLevelsSO>
    {
        [SerializeField] private LevelSO[] m_Levels;

        public int LevelsCount => m_Levels.Length;

        public LevelSO GetLevelByIndex(int index)
        {
            if (index >= m_Levels.Length)
                index = 0;

            return m_Levels.FirstOrDefault(level => level.LevelIndex == index);
        }
    }
}
