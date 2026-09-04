using Oxtail.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    public class EndlessModeLevelManager : LevelManager
    {
        public static BigNumber InitialGoal = new(10000);

        private const int m_GoalMultiplier = 100;

        public static new EndlessModeLevelManager Instance => LevelManager.Instance as EndlessModeLevelManager;

        public void InitLevel(EndlessModeSO endlessMode)
        {
            SelectStepsOption();

            SetCircuit(SaveLoadManager.Instance.GetEndlessModeCircuitIndex());
            SetCircuitPath();
            CreateInitialRewardLines();

            AudioManager.Instance.PlayMusic(m_BackgroundMusic);

            bool levelInitialized = SaveLoadManager.Instance.GetEndlessModeInitialized();
            if (!levelInitialized)
            {
                SetInitialSpaceships(endlessMode);
                SaveLoadManager.Instance.SaveEndlessModeInitialized(true);
            }

            StartCoroutine(CreateInitialSpaceships());

            Money.Value = SaveLoadManager.Instance.GetEndlessModeMoney();
            StepAccumulatedObjective.Value = SaveLoadManager.Instance.GetEndlessModeStepAccumulatedObjective();
            CanSpawnSpaceship.Value = m_CurrentCircuit.FreeSpaceShipParents;

            UpdateProgressionStep();

            m_RewardSpawnCoroutine = StartCoroutine(RewardSpawnCO());
            m_PassiveIncomeCoroutine = StartCoroutine(PassiveIncomeCO());

            SaveLoadManager.Instance.SaveAchievementProgression(AchievementType.EndlessModeTimes, 1);
        }

        private void SetInitialSpaceships(EndlessModeSO endlessMode)
        {
            List<int> spawnedTiers = new();
            int maxTier = 1;

            foreach (var tier in endlessMode.InitialShips)
            {
                for (int i = 0; i < tier.InitialShipsAmount; i++)
                {
                    spawnedTiers.Add(tier.InitialShipTier);
                }

                if (tier.InitialShipTier > maxTier)
                    maxTier = tier.InitialShipTier;
            }

            m_MaxSpaceShipTierCreated = maxTier;
            SaveLoadManager.Instance.SaveEndlessModeSpawnedSpaceshipTiers(spawnedTiers);
        }

        protected override void CreateInitialRewardLines()
        {
            RewardLinesCount = 0;
            int amount = SaveLoadManager.Instance.GetEndlessModeSpawnedRewardLines();

            for (var i = 0; i < amount; i++)
            {
                CreateRewardLine();
            }
        }

        protected override void UpdateProgressionStep()
        {
            m_ProgressionInfo = SaveLoadManager.Instance.GetEndlessModeCurrentGoal();

            EventManager<UpdateLevelProgression>.TriggerEvent(new UpdateLevelProgression(m_ProgressionInfo));
        }

        protected override List<int> GetSpawnedSpaceships()
        {
            return SaveLoadManager.Instance.GetEndlessModeSpawnedSpaceshipTiers();
        }

        protected override int GetSpawnSpaceshipTier()
        {
            return SaveLoadManager.Instance.GetEndlessModeSpawnSpaceshipTier();
        }

        protected override void StepCompleted()
        {
            IncreaseProgressionStep();
            UpdateProgressionStep();
        }

        protected override void IncreaseProgressionStep()
        {
            BigNumber currentGoal = m_ProgressionInfo.StepGoal;

            m_ProgressionInfo = new ProgressionStepInfo
            {
                StepType = ProgressionObjectiveType.CollectCoins,
                StepGoal = currentGoal * m_GoalMultiplier
            };

            SaveLoadManager.Instance.SaveEndlessModeCurrentGoal(m_ProgressionInfo);

            m_CoinsEarnedPerGoal = 0;
            m_CoinsSpendPerGoal = 0;
            m_MergeCountPerGoal = 0;
            m_AddSpaceshipCountPerGoal = 0;
            m_AddRewardLineCountPerGoal = 0;
            m_UpgradeCircuitCountPerGoal = 0;
            m_SpeedUpCountPerGoal = 0;
        }

        protected override void SaveMoney()
        {
            SaveLoadManager.Instance.SaveEndlessModeMoney(Money.Value);
        }

        protected override void SaveSpawnedSpaceshipsTiers(List<int> spaceships)
        {
            SaveLoadManager.Instance.SaveEndlessModeSpawnedSpaceshipTiers(spaceships);
        }

        protected override void SetNewFloorTier()
        {
            int tierNumber = SaveLoadManager.Instance.GetEndlessModeSpawnSpaceshipTier();
            int newFloorTier = Mathf.FloorToInt((m_MaxSpaceShipTierCreated / 4f) + 1);
            if (tierNumber < newFloorTier)
                SaveLoadManager.Instance.SaveEndlessModeSpawnSpaceshipTier(newFloorTier);
        }

        protected override void UpdateMaxTierAchievement(int newTier)
        {

        }

        public override void AddRewardLine()
        {
            CreateRewardLine();
        }

        protected override void CreateRewardLine()
        {
            m_CurrentCircuit.AddRewardLine();
            RewardLinesCount++;
            SaveLoadManager.Instance.SaveEndlessModeSpawnedRewardLines(RewardLinesCount);
        }

        public override void IncreaseAddSpaceshipLevel()
        {
            int level = SaveLoadManager.Instance.GetEndlessModeAddSpaceshipLevel();
            SaveLoadManager.Instance.SaveEndlessModeAddSpaceshipLevel(level + 1);

            UpdateProgressionStep(ProgressionObjectiveType.SpawnSpaceships, 1);
        }

        public override void SaveIncreaseMergeSpaceshipLevel()
        {
            int level = SaveLoadManager.Instance.GetEndlessModeMergeLevel();
            SaveLoadManager.Instance.SaveEndlessModeMergeLevel(level + 1);
        }

        protected override void SaveIncreaseRewardLineLevel()
        {
            int level = SaveLoadManager.Instance.GetEndlessModeRewardLineLevel();
            SaveLoadManager.Instance.SaveEndlessModeRewardLineLevel(level + 1);
        }

        public override void IncreaseCircuitLevel()
        {
            base.IncreaseCircuitLevel();

            int level = SaveLoadManager.Instance.GetEndlessModeCircuitLevel();
            SaveLoadManager.Instance.SaveEndlessModeCircuitLevel(level + 1);
            SaveLoadManager.Instance.SaveEndlessModeCircuitIndex(m_CircuitIndex);

            UpdateProgressionStep(ProgressionObjectiveType.UpgradeCircuit, 1);
        }

        protected override void SaveStepAccumulatedObjective()
        {
            SaveLoadManager.Instance.SaveEndlessModeStepAccumulatedObjective(StepAccumulatedObjective.Value);
        }

        protected override void SaveLevelCompletionData()
        {
            
        }

        public BigNumber GetGoalByIndex(int index)
        {
            return InitialGoal * BigNumber.Pow(m_GoalMultiplier, index);
        }
    }
}
