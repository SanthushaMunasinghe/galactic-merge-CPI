using Oxtail.Utils;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    public enum ProgressionObjectiveType
    {
        CollectCoins,
        SpawnSpaceships,
        MergeSpaceShips,
        AddRewardLines,
        UpgradeCircuit,
        UseSpeedBoost,
        CreateTierSpaceship,
        RevivePlanet,
        DestroyTargets,
        CrossRewardLines,
        CollectShinyGifts,
        MaxCircuitSpaceships,
        CompletesInTime,
        MaxGemsInTime
    }

    [System.Serializable]
    public struct ProgressionStepInfo
    {
        public ProgressionObjectiveType StepType;
        [ShowIf(nameof(ShowObjective))]
        public BigNumber StepGoal;

        private bool ShowObjective => StepType != ProgressionObjectiveType.MaxCircuitSpaceships;
    }

    [CreateAssetMenu(fileName = "SO_LevelProgress", menuName = "Incremental/Level/Level Progression")]
    public class LevelProgressionSO : ScriptableObject
    {
        [SerializeField] private ProgressionStepInfo[] m_Steps;

        public ProgressionStepInfo[] Steps => m_Steps;
        public int MaxStepIndex => m_Steps.Length - 1;

        public ProgressionStepInfo GetStep(int step)
        {
            step = Mathf.Clamp(step, 0, MaxStepIndex);
            return m_Steps[step];
        }
    }
}
