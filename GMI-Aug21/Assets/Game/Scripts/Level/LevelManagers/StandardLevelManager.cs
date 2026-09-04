using Oxtail.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    public class StandardLevelManager : LevelManager
    {
        public static new StandardLevelManager Instance => LevelManager.Instance as StandardLevelManager;

        public virtual void InitLevel(LevelSO level)
        {
            m_LevelIndex = level.LevelIndex;

            SelectStepsOption();

            SetCircuit(SaveLoadManager.Instance.GetCircuitIndex());
            SetCircuitPath();
            CreateInitialRewardLines();

            AudioManager.Instance.PlayMusic(m_BackgroundMusic);

            bool levelInitialized = SaveLoadManager.Instance.GetLevelInitialized();
            if (!levelInitialized)
            {
                SetInitialSpaceships(level);
                SaveLoadManager.Instance.SaveLevelInitialized(true);
            }

            StartCoroutine(CreateInitialSpaceships());

            Money.Value = SaveLoadManager.Instance.GetMoney();
            if (level.LevelIndex == 0 && Money.Value == 0)
                Money.Value = 2;

            StepAccumulatedObjective.Value = SaveLoadManager.Instance.GetStepAccumulatedObjective();
            CanSpawnSpaceship.Value = m_CurrentCircuit.FreeSpaceShipParents;

            LevelProgressionStepIndex = SaveLoadManager.Instance.GetLevelProgressionStep();
            UpdateProgressionStep();

            m_GemsOnCompletion = level.GemsOnCompletion;

            CalculateMoneyMultiplier();
            CalculateExtraTierCreationChance();
            CalculateExtraTierMergeChance();
            CalculateRewardTimeDecrease();
            CalculatePassiveIncome();
            CalculateExtraBoostTime();

            m_RewardSpawnCoroutine = StartCoroutine(RewardSpawnCO());
            m_PassiveIncomeCoroutine = StartCoroutine(PassiveIncomeCO());

            if (!levelInitialized)
                UpdateProgressionStep(ProgressionObjectiveType.CreateTierSpaceship, m_MaxSpaceShipTierCreated);
        }

        private void SetInitialSpaceships(LevelSO level)
        {
            List<int> spawnedTiers = new();
            int maxTier = 1;

            foreach (var tier in level.InitialShips)
            {
                for (int i = 0; i < tier.InitialShipsAmount; i++)
                {
                    spawnedTiers.Add(tier.InitialShipTier);
                }

                if (tier.InitialShipTier > maxTier)
                    maxTier = tier.InitialShipTier;
            }

            m_MaxSpaceShipTierCreated = maxTier;
            SaveLoadManager.Instance.SaveSpawnedSpaceshipTiers(spawnedTiers);
        }

        protected override void CreateInitialRewardLines()
        {
            RewardLinesCount = 0;
            int amount = SaveLoadManager.Instance.GetSpawnedRewardLines();

            for (var i = 0; i < amount; i++)
            {
                CreateRewardLine();
            }
        }

        protected override void CompleteLevel()
        {
            if (m_LevelFinished)
                return;

            base.CompleteLevel();
        }

        public void SetPowerUpData()
        {
            LevelSO level = GameLevelsSO.Instance.GetLevelByIndex(m_LevelIndex);
            bool unlocked = SaveLoadManager.Instance.GetPowerUpLevel(level.PowerUpReward.PowerUpID) > 0;
            if (!unlocked && level.PowerUpReward != null)
            {
                if (level.PowerUpUnlockPercentage == 100)
                    SaveLoadManager.Instance.SavePowerUpLevel(level.PowerUpReward.PowerUpID, 1);

                SaveLoadManager.Instance.SavePowerUpUnlockPercentage(level.PowerUpReward.PowerUpID, level.PowerUpUnlockPercentage);
            }
        }

        protected override List<int> GetSpawnedSpaceships()
        {
            return SaveLoadManager.Instance.GetSpawnedSpaceshipTiers();
        }

        protected override void IncreaseProgressionStep()
        {
            LevelProgressionStepIndex++;
            SaveLoadManager.Instance.SaveLevelProgressionStep(LevelProgressionStepIndex);
            StepAccumulatedObjective.Value = 0;
            SaveLoadManager.Instance.SaveStepAccumulatedObjective(StepAccumulatedObjective.Value);

            m_CoinsEarnedPerGoal = 0;
            m_CoinsSpendPerGoal = 0;
            m_MergeCountPerGoal = 0;
            m_AddSpaceshipCountPerGoal = 0;
            m_AddRewardLineCountPerGoal = 0;
            m_UpgradeCircuitCountPerGoal = 0;
            m_SpeedUpCountPerGoal = 0;

            if (m_LevelIndex == 0 && LevelProgressionStepIndex == 1)
                Money.Value += 6;
        }

        protected override void SaveLevelCompletionData()
        {
            SaveLoadManager.Instance.SaveLevelIndex(m_LevelIndex + 1);
            SaveLoadManager.Instance.SaveMaxLevelUnlockedIndex(m_LevelIndex + 1);

            EventManager<ShowLevelCompleteEvent>.TriggerEvent(new ShowLevelCompleteEvent(m_LevelIndex, m_MaxSpaceShipTierCreated));

            SaveLoadManager.Instance.ResetLevelData();
        }

        protected override void SaveMoney()
        {
            SaveLoadManager.Instance.SaveMoney(Money.Value);
        }

        public override void RemoveMoney(BigNumber money)
        {
            base.RemoveMoney(money);
            SaveLoadManager.Instance.AddAchievementProgression(AchievementType.SpendMoney, money);
        }

        protected override int GetSpawnSpaceshipTier()
        {
            return SaveLoadManager.Instance.GetSpawnSpaceshipTier();
        }

        public override void AddDefaultSpaceship()
        {
            base.AddDefaultSpaceship();

            SaveLoadManager.Instance.AddAchievementProgression(AchievementType.AddSpaceships, 1);
        }

        protected override void SetNewFloorTier()
        {
            int tierNumber = SaveLoadManager.Instance.GetSpawnSpaceshipTier();
            int newFloorTier = Mathf.FloorToInt((m_MaxSpaceShipTierCreated / 4f) + 1);
            if (tierNumber < newFloorTier)
                SaveLoadManager.Instance.SaveSpawnSpaceshipTier(newFloorTier);
        }

        protected override void SaveSpawnedSpaceshipsTiers(List<int> spaceships)
        {
            SaveLoadManager.Instance.SaveSpawnedSpaceshipTiers(spaceships);
        }

        protected override void CreateRewardLine()
        {
            m_CurrentCircuit.AddRewardLine();
            RewardLinesCount++;
            SaveLoadManager.Instance.SaveSpawnedRewardLines(RewardLinesCount);
        }

        public override void IncreaseAddSpaceshipLevel()
        {
            int level = SaveLoadManager.Instance.GetAddSpaceshipLevel();
            SaveLoadManager.Instance.SaveAddSpaceshipLevel(level + 1);

            UpdateProgressionStep(ProgressionObjectiveType.SpawnSpaceships, 1);
        }

        public override void SaveIncreaseMergeSpaceshipLevel()
        {
            int level = SaveLoadManager.Instance.GetMergeLevel();
            SaveLoadManager.Instance.SaveMergeLevel(level + 1);
        }

        protected override void SaveIncreaseRewardLineLevel()
        {
            int level = SaveLoadManager.Instance.GetRewardLineLevel();
            SaveLoadManager.Instance.SaveRewardLineLevel(level + 1);
        }

        public override void IncreaseCircuitLevel()
        {
            base.IncreaseCircuitLevel();

            int level = SaveLoadManager.Instance.GetCircuitLevel();
            SaveLoadManager.Instance.SaveCircuitLevel(level + 1);
            SaveLoadManager.Instance.SaveCircuitIndex(m_CircuitIndex);

            UpdateProgressionStep(ProgressionObjectiveType.UpgradeCircuit, 1);
            SaveLoadManager.Instance.AddAchievementProgression(AchievementType.UpgradeCircuits, 1);
        }

        protected override void StartSpeedUp()
        {
            base.StartSpeedUp();
            SaveLoadManager.Instance.AddAchievementProgression(AchievementType.SpeedBoostUses, 1);
        }

        public override void ApplyReward(RewardType type)
        {
            base.ApplyReward(type);
            SaveLoadManager.Instance.AddAchievementProgression(AchievementType.GiftRewards, 1);
        }

        protected override void SaveStepAccumulatedObjective()
        {
            SaveLoadManager.Instance.SaveStepAccumulatedObjective(StepAccumulatedObjective.Value);
        }
    }
}
