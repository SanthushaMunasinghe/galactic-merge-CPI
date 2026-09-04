using Oxtail.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    [System.Serializable]
    public struct SpaceshipTier
    {
        public int TierNumber;
        public int Coins;
        public Color SpaceShipColor;
    }

    [CreateAssetMenu(fileName = "SO_SpaceshipTiers", menuName = "Incremental/Spaceship Tiers")]
    public class SpaceshipTiers : ScriptableObjectSingleton<SpaceshipTiers>
    {
        [SerializeField] private SpaceshipTier[] m_SpaceshipTier;

        public int MaxTier => m_SpaceshipTier.Last().TierNumber;

        public SpaceshipTier GetTier(int tierNumber)
        {
            tierNumber = Mathf.Clamp(tierNumber, 1, MaxTier);
            return m_SpaceshipTier.FirstOrDefault(tier => tier.TierNumber == tierNumber);
        }
    }
}
