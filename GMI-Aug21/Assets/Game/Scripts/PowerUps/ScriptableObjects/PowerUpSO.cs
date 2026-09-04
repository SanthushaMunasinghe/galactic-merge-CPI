using Oxtail.Utils;
using System.Linq;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    public enum PowerUpType
    {
        Velocity,
        Coins,
        CreationDiscount,
        MergeDiscount,
        CriticalCreation,
        CriticalMerge,
        SpeedBoost,
        RewardChance,
        PassiveIncome,
        OfflineRewards
    }

    [System.Serializable]
    public struct PowerUpUpgrade
    {
        public int Level;
        public float Value;
        public int Cost;
    }

    [CreateAssetMenu(fileName = "PowerUp", menuName = "Incremental/PowerUp")]
    public class PowerUpSO : ScriptableObject
    {
        [Header("General Info")]
        [SerializeField] private string m_PowerUpID = "PowerUp_";
        [SerializeField] private string m_PowerUpName;
        [SerializeField] private string m_PowerUpDescription;
        [SerializeField] private Sprite m_PowerUpIcon;
        [SerializeField] private PowerUpType m_PowerUpType;

        [Header("Upgrades")]
        [SerializeField] private PowerUpUpgrade[] m_Upgrades;

        public bool IsMaxLevel(int currentLevel)
        {
            return currentLevel >= m_Upgrades.Last().Level;
        }

        public string PowerUpID => m_PowerUpID;
        public string PowerUpName => m_PowerUpName;
        public string PowerUpDescription => m_PowerUpDescription;
        public Sprite PowerUpIcon => m_PowerUpIcon;
        public PowerUpType PowerUpType => m_PowerUpType;

        public PowerUpUpgrade GetUpgradeByLevel(int level)
        {
            return m_Upgrades.FirstOrDefault(upgrade => upgrade.Level == level);
        }
    }
}
