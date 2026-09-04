using Oxtail.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    public class DailyChallengeLevelManager : LevelManager
    {
        public void InitLevel(DailyChallengeSO challenge)
        {
            m_LevelIndex = challenge.ChallengeIndex;

            SelectStepsOption();

            SetCircuit(SaveLoadManager.Instance.GetDailyChallengeCircuitIndex());
            SetCircuitPath();
            CreateInitialRewardLines();

            AudioManager.Instance.PlayMusic(m_BackgroundMusic);

            bool levelInitialized = SaveLoadManager.Instance.GetDailyChallengeInitialized();
            if (!levelInitialized)
            {
                SetInitialSpaceships(challenge);
                SaveLoadManager.Instance.SaveDailyChallengeInitialized(true);
            }

            StartCoroutine(CreateInitialSpaceships());

            Money.Value = SaveLoadManager.Instance.GetDailyChallengeMoney();
            StepAccumulatedObjective.Value = SaveLoadManager.Instance.GetDailyChallengeStepAccumulatedObjective();
            CanSpawnSpaceship.Value = m_CurrentCircuit.FreeSpaceShipParents;

            LevelProgressionStepIndex = SaveLoadManager.Instance.GetDailyChallengeProgressionStep();
            UpdateProgressionStep();

            m_GemsOnCompletion = challenge.GemsOnCompletion;

            m_RewardSpawnCoroutine = StartCoroutine(RewardSpawnCO());
            m_PassiveIncomeCoroutine = StartCoroutine(PassiveIncomeCO());

            if (!levelInitialized)
                UpdateProgressionStep(ProgressionObjectiveType.CreateTierSpaceship, m_MaxSpaceShipTierCreated);
        }

        private void SetInitialSpaceships(DailyChallengeSO challenge)
        {
            List<int> spawnedTiers = new();
            int maxTier = 1;

            foreach (var tier in challenge.InitialShips)
            {
                for (int i = 0; i < tier.InitialShipsAmount; i++)
                {
                    spawnedTiers.Add(tier.InitialShipTier);
                }

                if (tier.InitialShipTier > maxTier)
                    maxTier = tier.InitialShipTier;
            }

            m_MaxSpaceShipTierCreated = maxTier;
            SaveLoadManager.Instance.SaveDailyChallengeSpawnedSpaceshipTiers(spawnedTiers);
        }

        protected override void CreateInitialRewardLines()
        {
            RewardLinesCount = 0;
            int amount = SaveLoadManager.Instance.GetDailyChallengeSpawnedRewardLines();

            for (var i = 0; i < amount; i++)
            {
                CreateRewardLine();
            }
        }

        protected override List<int> GetSpawnedSpaceships()
        {
            return SaveLoadManager.Instance.GetDailyChallengeSpawnedSpaceshipTiers();
        }

        protected override int GetSpawnSpaceshipTier()
        {
            return SaveLoadManager.Instance.GetDailyChallengeSpawnSpaceshipTier();
        }

        protected override void IncreaseProgressionStep()
        {
            LevelProgressionStepIndex++;
            SaveLoadManager.Instance.SaveDailyChallengeProgressionStep(LevelProgressionStepIndex);
            StepAccumulatedObjective.Value = 0;
            SaveLoadManager.Instance.SaveDailyChallengeStepAccumulatedObjective(StepAccumulatedObjective.Value);

            m_CoinsEarnedPerGoal = 0;
            m_CoinsSpendPerGoal = 0;
            m_MergeCountPerGoal = 0;
            m_AddSpaceshipCountPerGoal = 0;
            m_AddRewardLineCountPerGoal = 0;
            m_UpgradeCircuitCountPerGoal = 0;
            m_SpeedUpCountPerGoal = 0;
        }

        protected override void CompleteLevel()
        {
            if (m_LevelFinished)
                return;

            base.CompleteLevel();

            SaveLoadManager.Instance.AddAchievementProgression(AchievementType.DailyChallengeTimes, 1);
        }

        protected override void SaveLevelCompletionData()
        {
            SaveLoadManager.Instance.SaveDailyChallengeAvailable(false);
            EventManager<ShowLevelCompleteEvent>.TriggerEvent(new ShowLevelCompleteEvent(m_LevelIndex, m_MaxSpaceShipTierCreated));
        }

        protected override void SaveMoney()
        {
            SaveLoadManager.Instance.SaveDailyChallengeMoney(Money.Value);
        }

        protected override void SaveSpawnedSpaceshipsTiers(List<int> spaceships)
        {
            SaveLoadManager.Instance.SaveDailyChallengeSpawnedSpaceshipTiers(spaceships);
        }

        protected override void SetNewFloorTier()
        {
            int tierNumber = SaveLoadManager.Instance.GetDailyChallengeSpawnSpaceshipTier();
            int newFloorTier = Mathf.FloorToInt((m_MaxSpaceShipTierCreated / 4f) + 1);
            if (tierNumber < newFloorTier)
                SaveLoadManager.Instance.SaveDailyChallengeSpawnSpaceshipTier(newFloorTier);
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
            SaveLoadManager.Instance.SaveDailyChallengeSpawnedRewardLines(RewardLinesCount);
        }

        public override void IncreaseAddSpaceshipLevel()
        {
            int level = SaveLoadManager.Instance.GetDailyChallengeAddSpaceshipLevel();
            SaveLoadManager.Instance.SaveDailyChallengeAddSpaceshipLevel(level + 1);

            UpdateProgressionStep(ProgressionObjectiveType.SpawnSpaceships, 1);
        }

        public override void SaveIncreaseMergeSpaceshipLevel()
        {
            int level = SaveLoadManager.Instance.GetDailyChallengeMergeLevel();
            SaveLoadManager.Instance.SaveDailyChallengeMergeLevel(level + 1);
        }

        protected override void SaveIncreaseRewardLineLevel()
        {
            int level = SaveLoadManager.Instance.GetDailyChallengeRewardLineLevel();
            SaveLoadManager.Instance.SaveDailyChallengeRewardLineLevel(level + 1);
        }

        public override void IncreaseCircuitLevel()
        {
            base.IncreaseCircuitLevel();

            int level = SaveLoadManager.Instance.GetDailyChallengeCircuitLevel();
            SaveLoadManager.Instance.SaveDailyChallengeCircuitLevel(level + 1);
            SaveLoadManager.Instance.SaveDailyChallengeCircuitIndex(m_CircuitIndex);

            UpdateProgressionStep(ProgressionObjectiveType.UpgradeCircuit, 1);
        }

        protected override void SaveStepAccumulatedObjective()
        {
            SaveLoadManager.Instance.SaveDailyChallengeStepAccumulatedObjective(StepAccumulatedObjective.Value);
        }
    }
}
