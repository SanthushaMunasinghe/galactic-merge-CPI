using System;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    [Serializable]
    public class Prestige
    {
        public float CoinsBonusMultiplier = 0.5f;
        public int FreeGoalGems = 50;
        public int MinimumMapToShow = 5;
        public int InitialGoalsAmount = 30;
        public float GoalIncreaseExponential = 1.2f;
        public bool PrestigeSystemActive = false;

        public static Prestige GetConfig() => Debugger.Prestige;
    }
}
