using Oxtail.Utils;
using System.Linq;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    [System.Serializable]
    public struct RankInfo
    {
        public string RankID;
        public string RankName;
        public Sprite RankBadge;
        [Range(0, 100)]
        public int RankPercentage;
    }

    [CreateAssetMenu(fileName = "GameRanks", menuName = "Incremental/Game/Game Ranks")]
    public class GamePlayerRanksSO : ScriptableObjectSingleton<GamePlayerRanksSO>
    {
        [SerializeField] private RankInfo[] m_PlayerRanks;

        public RankInfo GetPlayerRankByIndex(int index)
        {
            index = Mathf.Clamp(index, 0, m_PlayerRanks.Length - 1);
            return m_PlayerRanks[index];
        }

        public RankInfo GetRankByID(string id)
        {
            return m_PlayerRanks.FirstOrDefault(x => x.RankID == id);
        }

        public RankInfo GetRankByPercentage(int percentage)
        {
            return m_PlayerRanks.Reverse().FirstOrDefault(x => x.RankPercentage <= percentage);
        }
    }
}
