using Sirenix.OdinInspector;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    public enum GameRewardType
    {
        Gems,
        PowerUpUnlock,
        Celestium
    }

    [System.Serializable]
    public struct GameRewardInfo
    {
        public GameRewardType Type;
        [HideIf(nameof(IsPowerUp))]
        public int Quantity;
        [ShowIf(nameof(IsPowerUp))]
        public PowerUpSO PowerUp;

        private bool IsPowerUp => Type == GameRewardType.PowerUpUnlock;
    }


    [CreateAssetMenu(fileName = "DailyLoginRewards", menuName = "Incremental/Daily Login Rewards")]
    public class DailyLoginRewardsSO : ScriptableObject
    {
        [SerializeField] private GameRewardInfo Day1;
        [SerializeField] private GameRewardInfo Day2;
        [SerializeField] private GameRewardInfo Day3;
        [SerializeField] private GameRewardInfo Day4;
        [SerializeField] private GameRewardInfo Day5;
        [SerializeField] private GameRewardInfo Day6;
        [SerializeField] private GameRewardInfo Day7;

        public GameRewardInfo GetDayReward(int day)
        {
            return day switch
            {
                1 => Day1,
                2 => Day2,
                3 => Day3,
                4 => Day4,
                5 => Day5,
                6 => Day6,
                7 => Day7
            };
        }
    }
}
