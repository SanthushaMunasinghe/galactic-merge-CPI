using Oxtail.Utils;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using Unity.Serialization.Json;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    public class SaveLoadManager : Singleton<SaveLoadManager>
    {
        private const string m_SoundsvolumeKey = "SoundsVolume";
        private const string m_MusicVolumeKey = "MusicVolume";
        private const string m_GemsKey = "Gems";
        private const string m_MoneyKey = "Money";
        private const string m_LevelKey = "Level";
        private const string m_BigNumberGemsKey = "BigNumberGems";
        private const string m_BigNumberMoneyKey = "BigNumberMoney";
        private const string m_CelestiumKey = "Celestium";
        private const string m_MaxLevelUnlockedIndex = "MaxLevelUnlockedIndex";
        private const string m_CircuitIndexKey = "CircuitIndex";
        private const string m_MergeUpgradeLevelKey = "MergeUpgradeLevel";
        private const string m_AddSpaceshipLevelKey = "AddSpaceshipLevel";
        private const string m_MoneyLineLevelKey = "MoneyLineLevel";
        private const string m_CircuitLevelKey = "CircuitLevel";
        private const string m_SpaceshipSpawnTierKey = "SpaceshipSpawnTier";
        private const string m_SpawnedSpaceshipsTiersKey = "SpawnedSpaceshipsTiers";
        private const string m_SpawnedRewardLinesKey = "SpawnedRewardLines";
        private const string m_StepAccumulatedObjectiveKey = "StepAccumulatedObjective";
        private const string m_StepAccumulatedGoalKey = "StepAccumulatedGoal";
        private const string m_LevelObjectiveStepKey = "LevelObjectiveStep";
        private const string m_LevelInitializedKey = "LevelInitialized";

        private const string m_PowerUpLevelKey = "PowerUpLevel_";
        private const string m_PowerUpUnlockKey = "PowerUpUnlock_";

        private const string m_PlanetHealKey = "PlanetHeal";
        private const string m_BigNumberPlanetHealKey = "BigNumberPlanetHeal";
        private const string m_TargetHealthKey = "TargetHealth";
        private const string m_BigNumberTargetHealthKey = "BigNumberTargetHealth";

        private const string m_FirstTimePlayed = "FirstTimePlayed";
        private const string m_LastTimePlayed = "LastTimePlayed";

        private const string m_SurveySentKey = "SurveySent";

        private const string m_CurrentLoginKey = "CurrentLogin";
        private const string m_LoginDaysKey = "LoginDays";
        private const string m_RewardsWeekKey = "RewardsWeek";
        private const string m_ShowLoginRewardKey = "ShowLoginReward";

        private const string m_SpaceshipShapeKey = "SpaceshipShape";
        private const string m_SpaceshipEffectKey = "SpaceshipEffect";
        private const string m_SpaceshipTrailKey = "SpaceshipTrail";
        private const string m_SpaceshipCustomizationUnlockedKey = "SpaceshipCustomizationUnlockedKey_";

        private const string m_AchievementGoalIndexKey = "AchievementGoalIndex_";
        private const string m_AchievementProgressionKey = "AchievementProgression_";

        private const string m_LoginStreakDaysKey = "LoginStreakDays";

        private const string m_DailyChallengeAvailableKey = "DailyChallengeAvailable";
        private const string m_DailyChallengeIndexKey = "DailyChallengeIndex";
        private const string m_DailyChallengeCircuitIndexKey = "DailyChallengeCircuitIndex";
        private const string m_DailyChallengeSpawnedSpaceshipsTiersKey = "DailyChallengeSpawnedSpaceshipsTiers";
        private const string m_DailyChallengeObjectiveStepKey = "DailyChallengeObjectiveStep";
        private const string m_DailyChallengeStepAccumulatedGoalKey = "DailyChallengeStepAccumulatedGoal";
        private const string m_DailyChallengeMoneyKey = "DailyChallengeMoney";
        private const string m_DailyChallengeSpaceshipSpawnTierKey = "DailyChallengeSpaceshipSpawnTier";
        private const string m_DailyChallengeSpawnedRewardLinesKey = "DailyChallengeSpawnedRewardLines";
        private const string m_DailyChallengeMergeUpgradeLevelKey = "DailyChallengeMergeUpgradeLevel";
        private const string m_DailyChallengeAddSpaceshipLevelKey = "DailyChallengeAddSpaceshipLevel";
        private const string m_DailyChallengeMoneyLineLevelKey = "DailyChallengeMoneyLineLevel";
        private const string m_DailyChallengeCircuitLevelKey = "DailyChallengeCircuitLevel";
        private const string m_DailyChallengeInitializedKey = "DailyChallengeLevelInitialized";
        private const string m_ShowDailyChallengeNotify = "ShowDailyChallengeNotify";

        private const string m_EndlessModeCircuitIndexKey = "EndlessModeCircuitIndex";
        private const string m_EndlessModeSpawnedSpaceshipsTiersKey = "EndlessModeSpawnedSpaceshipsTiers";
        private const string m_EndlessModeStepAccumulatedGoalKey = "EndlessModeStepAccumulatedGoal";
        private const string m_EndlessModeMoneyKey = "EndlessModeMoney";
        private const string m_EndlessModeSpaceshipSpawnTierKey = "EndlessModeSpaceshipSpawnTier";
        private const string m_EndlessModeSpawnedRewardLinesKey = "EndlessModeSpawnedRewardLines";
        private const string m_EndlessModeMergeUpgradeLevelKey = "EndlessModeMergeUpgradeLevel";
        private const string m_EndlessModeAddSpaceshipLevelKey = "EndlessModeAddSpaceshipLevel";
        private const string m_EndlessModeMoneyLineLevelKey = "EndlessModeMoneyLineLevel";
        private const string m_EndlessModeCircuitLevelKey = "EndlessModeCircuitLevel";
        private const string m_EndlessModeInitializedKey = "EndlessModeLevelInitialized";
        private const string m_EndlessModeCurrentGoalKey = "EndlessModeCurrentGoal";
        private const string m_EndlessModeClaimedRewardsIndexesKey = "m_EndlessModeClaimedRewardsIndexes";
        private const string m_ShowEndlessModeNotify = "ShowEndlessModeNotify";

        private const string m_TotalNumberOfStepsCompletedKey = "TotalNumberOfStepsCompleted";
        private const string m_TotalNumberOfMapsCompletedKey = "TotalNumberOfMapsCompleted";

        private const string m_NoAdsKey = "NoAds";

        private const string m_PowerUpsTutorialFinished = "PowerUpsTutorialFinished";
        private const string m_RestoredPurchases = "RestoredPurchases";

        private const string m_PrestigeLevelKey = "PrestigeLevel";
        private const string m_CurrentPrestigeGoalsKey = "CurrentPrestigeGoals";
        private const string m_PrestigeTutorialDoneKey = "PrestigeTutorialDoneKey";
        private const string m_PrestigeUnlockedKey = "PrestigeUnlockedKey";

        private const string m_InitialTutorial1DoneKey = "InitialTutorial1Done";
        private const string m_InitialTutorial2DoneKey = "InitialTutorial2Done";

        public event Action OnGemsUpdated;
        public event Action OnCelestiumUpdated;
        public event Action<string> OnPowerUpUpdated;
        public event Action OnAchievementUpdated;

        public float GetSoundsVolume()
        {
            return PlayerPrefs.GetFloat(m_SoundsvolumeKey, 100);
        }

        public void SaveSoundsVolume(float volume)
        {
            PlayerPrefs.SetFloat(m_SoundsvolumeKey, volume);
            Save();
        }

        public float GetMusicVolume()
        {
            return PlayerPrefs.GetFloat(m_MusicVolumeKey, 100);
        }

        public void SaveMusicVolume(float volume)
        {
            PlayerPrefs.SetFloat(m_MusicVolumeKey, volume);
            Save();
        }

        public void SaveGems(BigNumber gems)
        {
            PlayerPrefs.SetString(m_BigNumberGemsKey, gems.Serialize());
            OnGemsUpdated?.Invoke();
            Save();
        }

        public void AddGems(BigNumber gems)
        {
            BigNumber total = GetGems() + gems;
            SaveGems(total);

            if (gems < 0)
                AddAchievementProgression(AchievementType.SpendGems, BigNumber.Abs(gems));
        }

        public BigNumber GetGems()
        {
            if (PlayerPrefs.HasKey(m_GemsKey))
            {
                int gems = PlayerPrefs.GetInt(m_GemsKey);
                BigNumber bigNumberGems = new BigNumber(gems);
                PlayerPrefs.DeleteKey(m_GemsKey);
                return bigNumberGems;
            }

            return BigNumber.Deserialize(PlayerPrefs.GetString(m_BigNumberGemsKey, new BigNumber(0).Serialize()));
        }

        public void SaveMoney(BigNumber money)
        {
            PlayerPrefs.SetString(m_BigNumberMoneyKey, money.Serialize());
            Save();
        }

        public BigNumber GetMoney()
        {
            if (PlayerPrefs.HasKey(m_MoneyKey))
            {
                int money = PlayerPrefs.GetInt(m_MoneyKey);
                BigNumber bigNumberGems = new BigNumber(money);
                PlayerPrefs.DeleteKey(m_MoneyKey);
                return bigNumberGems;
            }

            return BigNumber.Deserialize(PlayerPrefs.GetString(m_BigNumberMoneyKey, BigNumber.Zero.Serialize()));
        }

        public void SaveCelestium(BigNumber celestium)
        {
            PlayerPrefs.SetString(m_CelestiumKey, celestium.Serialize());
            OnCelestiumUpdated?.Invoke();
            Save();
        }

        public void AddCelestium(BigNumber celestium)
        {
            BigNumber total = GetCelestium() + celestium;
            SaveCelestium(total);
        }

        public BigNumber GetCelestium()
        {
            return BigNumber.Deserialize(PlayerPrefs.GetString(m_CelestiumKey, new BigNumber(0).Serialize()));
        }

        public void SaveLevelIndex(int level)
        {
            if (level >= GameLevelsSO.Instance.LevelsCount)
                level = 0;

            PlayerPrefs.SetInt(m_LevelKey, level);
            Save();
        }

        public int GetLevelIndex()
        {
            return PlayerPrefs.GetInt(m_LevelKey, 0);
        }

        public void SaveMaxLevelUnlockedIndex(int index)
        {
            PlayerPrefs.SetInt(m_MaxLevelUnlockedIndex, index);
            Save();
        }

        public int GetMaxLevelUnlockedIndex()
        {
            return PlayerPrefs.GetInt(m_MaxLevelUnlockedIndex, GetLevelIndex());
        }

        public void SaveCircuitIndex(int index)
        {
            PlayerPrefs.SetInt(m_CircuitIndexKey, index);
            Save();
        }

        public int GetCircuitIndex()
        {
            return PlayerPrefs.GetInt(m_CircuitIndexKey, 0);
        }

        public void SaveMergeLevel(int level)
        {
            PlayerPrefs.SetInt(m_MergeUpgradeLevelKey, level);
            Save();
        }

        public int GetMergeLevel()
        {
            return PlayerPrefs.GetInt(m_MergeUpgradeLevelKey, 0);
        }

        public void SaveAddSpaceshipLevel(int level)
        {
            PlayerPrefs.SetInt(m_AddSpaceshipLevelKey, level);
            Save();
        }

        public int GetAddSpaceshipLevel()
        {
            return PlayerPrefs.GetInt(m_AddSpaceshipLevelKey, 0);
        }

        public void SaveRewardLineLevel(int level)
        {
            PlayerPrefs.SetInt(m_MoneyLineLevelKey, level);
            Save();
        }

        public int GetRewardLineLevel()
        {
            return PlayerPrefs.GetInt(m_MoneyLineLevelKey, 0);
        }

        public void SaveCircuitLevel(int level)
        {
            PlayerPrefs.SetInt(m_CircuitLevelKey, level);
            Save();
        }

        public int GetCircuitLevel()
        {
            return PlayerPrefs.GetInt(m_CircuitLevelKey, 0);
        }

        public void SaveSpawnSpaceshipTier(int tierNumber)
        {
            PlayerPrefs.SetInt(m_SpaceshipSpawnTierKey, tierNumber);
            Save();
        }

        public int GetSpawnSpaceshipTier()
        {
            return PlayerPrefs.GetInt(m_SpaceshipSpawnTierKey, 1);
        }

        public void SaveSpawnedSpaceshipTiers(List<int> tiers)
        {
            string serializedList = JsonSerialization.ToJson(tiers);
            PlayerPrefs.SetString(m_SpawnedSpaceshipsTiersKey, serializedList);
            Save();
        }

        public List<int> GetSpawnedSpaceshipTiers()
        {
            string serializedList = PlayerPrefs.GetString(m_SpawnedSpaceshipsTiersKey);
            if (string.IsNullOrEmpty(serializedList))
                return new List<int>() { 1 };

            return JsonSerialization.FromJson<List<int>>(serializedList);
        }

        public void SaveSpawnedRewardLines(int lines)
        {
            PlayerPrefs.SetInt(m_SpawnedRewardLinesKey, lines);
            Save();
        }

        public int GetSpawnedRewardLines()
        {
            return PlayerPrefs.GetInt(m_SpawnedRewardLinesKey, 1);
        }

        public void SaveStepAccumulatedObjective(BigNumber objectiveValue)
        {
            PlayerPrefs.SetString(m_StepAccumulatedGoalKey, objectiveValue.Serialize());
            Save();
        }

        public BigNumber GetStepAccumulatedObjective()
        {
            if (PlayerPrefs.HasKey(m_StepAccumulatedObjectiveKey))
            {
                int objective = PlayerPrefs.GetInt(m_StepAccumulatedObjectiveKey);
                BigNumber bigNumberObjective = new BigNumber(objective);
                PlayerPrefs.DeleteKey(m_StepAccumulatedObjectiveKey);
                return bigNumberObjective;
            }

            return BigNumber.Deserialize(PlayerPrefs.GetString(m_StepAccumulatedGoalKey, BigNumber.Zero.Serialize()));
        }

        public void SaveLevelProgressionStep(int step)
        {
            PlayerPrefs.SetInt(m_LevelObjectiveStepKey, step);
            Save();
        }

        public int GetLevelProgressionStep()
        {
            return PlayerPrefs.GetInt(m_LevelObjectiveStepKey, 0);
        }

        public void SaveLevelInitialized(bool initialized)
        {
            PlayerPrefs.SetInt(m_LevelInitializedKey, initialized ? 1 : 0);
            Save();
        }

        public bool GetLevelInitialized()
        {
            return PlayerPrefs.GetInt(m_LevelInitializedKey, 0) == 1;
        }

        public void SavePlanetHeal(int planetIndex, BigNumber heal)
        {
            PlayerPrefs.SetString(m_BigNumberPlanetHealKey + planetIndex, heal.Serialize());
            Save();
        }

        public BigNumber GetPlanetHeal(int planetIndex)
        {
            if (PlayerPrefs.HasKey(m_PlanetHealKey + planetIndex))
            {
                float heal = PlayerPrefs.GetFloat(m_PlanetHealKey + planetIndex);
                BigNumber bigNumberHeal = new BigNumber(heal);
                PlayerPrefs.DeleteKey(m_PlanetHealKey + planetIndex);
                return bigNumberHeal;
            }

            return BigNumber.Deserialize(PlayerPrefs.GetString(m_BigNumberPlanetHealKey + planetIndex,
                BigNumber.Zero.Serialize()));
        }

        public void SaveTargetHealth(int targetIndex, BigNumber health)
        {
            PlayerPrefs.SetString(m_BigNumberTargetHealthKey + targetIndex, health.Serialize());
            Save();
        }

        public BigNumber GetTargetHealth(int targetIndex)
        {
            if (PlayerPrefs.HasKey(m_TargetHealthKey + targetIndex))
            {
                int health = PlayerPrefs.GetInt(m_TargetHealthKey + targetIndex);
                BigNumber bigNumberHealth = new BigNumber(health);
                PlayerPrefs.DeleteKey(m_TargetHealthKey + targetIndex);
                return bigNumberHealth;
            }

            return BigNumber.Deserialize(PlayerPrefs.GetString(m_BigNumberTargetHealthKey + targetIndex,
                new BigNumber(-1).Serialize()));
        }

        public void SavePowerUpLevel(string powerUpID, int level)
        {
            PlayerPrefs.SetInt(m_PowerUpLevelKey + powerUpID, level);
            OnPowerUpUpdated?.Invoke(powerUpID);
            Save();
        }

        public int GetPowerUpLevel(string powerUpID)
        {
            return PlayerPrefs.GetInt(m_PowerUpLevelKey + powerUpID, 0);
        }

        public void SavePowerUpUnlockPercentage(string powerUpID, int percentage)
        {
            PlayerPrefs.SetInt(m_PowerUpUnlockKey + powerUpID, percentage);
            Save();
        }

        public int GetPowerUpUnlockPercentage(string powerUpID)
        {
            return PlayerPrefs.GetInt(m_PowerUpUnlockKey + powerUpID, 0);
        }

        public void SaveFirstTimePlayed()
        {
            PlayerPrefs.SetString(m_FirstTimePlayed, DateTime.UtcNow.ToString());
            Save();
        }

        public string GetFirstTimePlayed()
        {
            return PlayerPrefs.GetString(m_FirstTimePlayed, string.Empty);
        }

        public void SaveLastTimePlayed()
        {
            PlayerPrefs.SetString(m_LastTimePlayed, DateTime.UtcNow.ToString());
            Save();
        }

        public string GetLastTimePlayed()
        {
            return PlayerPrefs.GetString(m_LastTimePlayed, string.Empty);
        }

        public void SaveSurveySent()
        {
            PlayerPrefs.SetInt(m_SurveySentKey, 1);
            Save();
        }

        public bool GetSurveySent()
        {
            return PlayerPrefs.GetInt(m_SurveySentKey, 0) == 1;
        }

        public void RefreshDailyState()
        {
            SaveShowDailyChallengeNotify(true);
            SaveShowEndlessModeNotify(true);

            string currentLogin = GetCurrentLoginValue();
            if (!string.IsNullOrEmpty(currentLogin))
            {
                if (GetAchievementProgression(AchievementType.LoginDays) == 0)
                    SaveAchievementProgression(AchievementType.LoginDays, 1);

                DateTime currentDate = DateTime.Parse(currentLogin);
                DateTime nowDate = DateTime.Now.Date;
                if (currentDate < nowDate)
                {
                    ResetDailyEvents();

                    int daysDifference = (nowDate - currentDate.Date).Days;
                    if (daysDifference == 1)
                    {
                        AddLoginStreak();
                        if (GetAchievementProgression(AchievementType.LoginDays) >= 1)
                            AddAchievementProgression(AchievementType.LoginDays, 1);
                    }
                    else if (daysDifference > 1)
                    {
                        ResetLoginStreakDays();
                        SaveAchievementProgression(AchievementType.LoginDays, 1);
                    }
                }
            }
            else
            {
                SaveAchievementProgression(AchievementType.LoginDays, 1);
            }

            SaveCurrentLoginValue(DateTime.Now.Date.ToString());
        }

        public void SaveCurrentLoginValue(string value)
        {
            PlayerPrefs.SetString(m_CurrentLoginKey, value);
            Save();
        }

        public string GetCurrentLoginValue()
        {
            return PlayerPrefs.GetString(m_CurrentLoginKey, string.Empty);
        }

        public void SaveLoginDay(int day)
        {
            if (day > 7)
            {
                int week = GetRewardsWeekIndex();
                SaveRewardsWeekIndex(week + 1);
                day = 1;
            }

            day = Mathf.Clamp(day, 1, 7);

            PlayerPrefs.SetInt(m_LoginDaysKey, day);
            Save();
        }

        public int GetLoginDay()
        {
            return PlayerPrefs.GetInt(m_LoginDaysKey, 1);
        }

        public void SaveRewardsWeekIndex(int weekIndex)
        {
            weekIndex = Mathf.Clamp(weekIndex, 0, GameDailyRewardsSO.Instance.MaxWeekIndex);
            PlayerPrefs.SetInt(m_RewardsWeekKey, weekIndex);
            Save();
        }

        public int GetRewardsWeekIndex()
        {
            return PlayerPrefs.GetInt(m_RewardsWeekKey, 0);
        }

        public void SaveShowLoginReward(bool show)
        {
            PlayerPrefs.SetInt(m_ShowLoginRewardKey, show ? 1 : 0);
            Save();
        }

        public bool GetShowLoginReward()
        {
            return PlayerPrefs.GetInt(m_ShowLoginRewardKey, 1) == 1;
        }

        public void SaveSpaceshipShapeID(string id)
        {
            PlayerPrefs.SetString(m_SpaceshipShapeKey, id);
            Save();
        }

        public string GetSpaceshipShapeID()
        {
            return PlayerPrefs.GetString(m_SpaceshipShapeKey,
                SpaceshipCustomizationSO.Instance.DefaultShape.CustomizationID);
        }

        public void SaveSpaceshipEffectID(string id)
        {
            PlayerPrefs.SetString(m_SpaceshipEffectKey, id);
            Save();
        }

        public string GetSpaceshipEffectID()
        {
            return PlayerPrefs.GetString(m_SpaceshipEffectKey,
                SpaceshipCustomizationSO.Instance.DefaultEffect.CustomizationID);
        }

        public void SaveSpaceshipTrailID(string id)
        {
            PlayerPrefs.SetString(m_SpaceshipTrailKey, id);
            Save();
        }

        public string GetSpaceshipTrailID()
        {
            return PlayerPrefs.GetString(m_SpaceshipTrailKey,
                SpaceshipCustomizationSO.Instance.DefaultTrail.CustomizationID);
        }

        public void SaveSpaceshipCustomizationUnlocked(string customizationID)
        {
            PlayerPrefs.SetInt(m_SpaceshipCustomizationUnlockedKey + customizationID, 1);
            Save();
        }

        public bool GetSpaceshipCustomizationUnlocked(string customizationID)
        {
            return PlayerPrefs.GetInt(m_SpaceshipCustomizationUnlockedKey + customizationID, 0) == 1;
        }

        public void SaveAchievementGoalIndex(string achievementID, int index)
        {
            PlayerPrefs.SetInt(m_AchievementGoalIndexKey + achievementID, index);
            Save();
        }

        public int GetAchievementGoalIndex(string achievementID)
        {
            return PlayerPrefs.GetInt(m_AchievementGoalIndexKey + achievementID, 0);
        }

        public void AddAchievementProgression(AchievementType achievementType, BigNumber progression)
        {
            var achievement = GameAchievementsSO.Instance.GetAchievementByType(achievementType);
            BigNumber totalProgression = GetAchievementProgression(achievementType) + progression;
            PlayerPrefs.SetString(m_AchievementProgressionKey + achievement.AchievementID,
                totalProgression.Serialize());
            Save();

            OnAchievementUpdated?.Invoke();
        }

        public void SaveAchievementProgression(AchievementType achievementType, BigNumber progression)
        {
            var achievement = GameAchievementsSO.Instance.GetAchievementByType(achievementType);
            PlayerPrefs.SetString(m_AchievementProgressionKey + achievement.AchievementID, progression.Serialize());
            Save();

            OnAchievementUpdated?.Invoke();
        }

        public BigNumber GetAchievementProgression(AchievementType achievementType)
        {
            string achievementID = GameAchievementsSO.Instance.GetAchievementByType(achievementType).AchievementID;
            return BigNumber.Deserialize(PlayerPrefs.GetString(m_AchievementProgressionKey + achievementID,
                BigNumber.Zero.Serialize()));
        }

        public void AddLoginStreak()
        {
            int total = GetLoginStreakDays() + 1;
            PlayerPrefs.SetInt(m_LoginStreakDaysKey, total);
            Save();
        }

        public void ResetLoginStreakDays()
        {
            PlayerPrefs.DeleteKey(m_LoginStreakDaysKey);
        }

        public int GetLoginStreakDays()
        {
            return PlayerPrefs.GetInt(m_LoginStreakDaysKey, 1);
        }

        //-------------- Daily Challenge -------------

        #region Daily Challenge

        public void SaveDailyChallengeAvailable(bool available)
        {
            PlayerPrefs.SetInt(m_DailyChallengeAvailableKey, available ? 1 : 0);
            Save();
        }

        public bool GetDailyChallengeAvailable()
        {
            return PlayerPrefs.GetInt(m_DailyChallengeAvailableKey, 1) == 1;
        }

        public void SaveDailyChallengeIndex(int index)
        {
            PlayerPrefs.SetInt(m_DailyChallengeIndexKey, index);
            Save();
        }

        public int GetDailyChallengeIndex()
        {
            return PlayerPrefs.GetInt(m_DailyChallengeIndexKey, 0);
        }

        public void SaveDailyChallengeCircuitIndex(int index)
        {
            PlayerPrefs.SetInt(m_DailyChallengeCircuitIndexKey, index);
            Save();
        }

        public int GetDailyChallengeCircuitIndex()
        {
            return PlayerPrefs.GetInt(m_DailyChallengeCircuitIndexKey, 0);
        }

        public void SaveDailyChallengeSpawnedSpaceshipTiers(List<int> tiers)
        {
            string serializedList = JsonSerialization.ToJson(tiers);
            PlayerPrefs.SetString(m_DailyChallengeSpawnedSpaceshipsTiersKey, serializedList);
            Save();
        }

        public List<int> GetDailyChallengeSpawnedSpaceshipTiers()
        {
            string serializedList = PlayerPrefs.GetString(m_DailyChallengeSpawnedSpaceshipsTiersKey);
            if (string.IsNullOrEmpty(serializedList))
                return new List<int>() { 1 };

            return JsonSerialization.FromJson<List<int>>(serializedList);
        }

        public void SaveDailyChallengeProgressionStep(int step)
        {
            PlayerPrefs.SetInt(m_DailyChallengeObjectiveStepKey, step);
            Save();
        }

        public int GetDailyChallengeProgressionStep()
        {
            return PlayerPrefs.GetInt(m_DailyChallengeObjectiveStepKey, 0);
        }

        public void SaveDailyChallengeStepAccumulatedObjective(BigNumber objectiveValue)
        {
            PlayerPrefs.SetString(m_DailyChallengeStepAccumulatedGoalKey, objectiveValue.Serialize());
            Save();
        }

        public BigNumber GetDailyChallengeStepAccumulatedObjective()
        {
            return BigNumber.Deserialize(PlayerPrefs.GetString(m_DailyChallengeStepAccumulatedGoalKey,
                BigNumber.Zero.Serialize()));
        }

        public void SaveDailyChallengeMoney(BigNumber money)
        {
            PlayerPrefs.SetString(m_DailyChallengeMoneyKey, money.Serialize());
            Save();
        }

        public BigNumber GetDailyChallengeMoney()
        {
            return BigNumber.Deserialize(PlayerPrefs.GetString(m_DailyChallengeMoneyKey, BigNumber.Zero.Serialize()));
        }

        public void SaveDailyChallengeSpawnSpaceshipTier(int tierNumber)
        {
            PlayerPrefs.SetInt(m_DailyChallengeSpaceshipSpawnTierKey, tierNumber);
            Save();
        }

        public int GetDailyChallengeSpawnSpaceshipTier()
        {
            return PlayerPrefs.GetInt(m_DailyChallengeSpaceshipSpawnTierKey, 1);
        }

        public void SaveDailyChallengeSpawnedRewardLines(int lines)
        {
            PlayerPrefs.SetInt(m_DailyChallengeSpawnedRewardLinesKey, lines);
            Save();
        }

        public int GetDailyChallengeSpawnedRewardLines()
        {
            return PlayerPrefs.GetInt(m_DailyChallengeSpawnedRewardLinesKey, 1);
        }

        public void SaveDailyChallengeAddSpaceshipLevel(int level)
        {
            PlayerPrefs.SetInt(m_DailyChallengeAddSpaceshipLevelKey, level);
            Save();
        }

        public int GetDailyChallengeAddSpaceshipLevel()
        {
            return PlayerPrefs.GetInt(m_DailyChallengeAddSpaceshipLevelKey, 0);
        }

        public void SaveDailyChallengeMergeLevel(int level)
        {
            PlayerPrefs.SetInt(m_DailyChallengeMergeUpgradeLevelKey, level);
            Save();
        }

        public int GetDailyChallengeMergeLevel()
        {
            return PlayerPrefs.GetInt(m_DailyChallengeMergeUpgradeLevelKey, 0);
        }

        public void SaveDailyChallengeRewardLineLevel(int level)
        {
            PlayerPrefs.SetInt(m_DailyChallengeMoneyLineLevelKey, level);
            Save();
        }

        public int GetDailyChallengeRewardLineLevel()
        {
            return PlayerPrefs.GetInt(m_DailyChallengeMoneyLineLevelKey, 0);
        }

        public void SaveDailyChallengeCircuitLevel(int level)
        {
            PlayerPrefs.SetInt(m_DailyChallengeCircuitLevelKey, level);
            Save();
        }

        public int GetDailyChallengeCircuitLevel()
        {
            return PlayerPrefs.GetInt(m_DailyChallengeCircuitLevelKey, 0);
        }

        public void SaveDailyChallengeInitialized(bool initialized)
        {
            PlayerPrefs.SetInt(m_DailyChallengeInitializedKey, initialized ? 1 : 0);
            Save();
        }

        public bool GetDailyChallengeInitialized()
        {
            return PlayerPrefs.GetInt(m_DailyChallengeInitializedKey, 0) == 1;
        }

        public void SaveShowDailyChallengeNotify(bool show)
        {
            PlayerPrefs.SetInt(m_ShowDailyChallengeNotify, show ? 1 : 0);
            Save();
        }

        public bool GetShowDailyChallengeNotify()
        {
            return PlayerPrefs.GetInt(m_ShowDailyChallengeNotify, 1) == 1;
        }

        #endregion

        //-------------- Endless Mode -------------

        #region Endless Mode

        public void SaveEndlessModeCircuitIndex(int index)
        {
            PlayerPrefs.SetInt(m_EndlessModeCircuitIndexKey, index);
            Save();
        }

        public int GetEndlessModeCircuitIndex()
        {
            return PlayerPrefs.GetInt(m_EndlessModeCircuitIndexKey, 0);
        }

        public void SaveEndlessModeSpawnedSpaceshipTiers(List<int> tiers)
        {
            string serializedList = JsonSerialization.ToJson(tiers);
            PlayerPrefs.SetString(m_EndlessModeSpawnedSpaceshipsTiersKey, serializedList);
            Save();
        }

        public List<int> GetEndlessModeSpawnedSpaceshipTiers()
        {
            string serializedList = PlayerPrefs.GetString(m_EndlessModeSpawnedSpaceshipsTiersKey);
            if (string.IsNullOrEmpty(serializedList))
                return new List<int>() { 1 };

            return JsonSerialization.FromJson<List<int>>(serializedList);
        }

        public void SaveEndlessModeStepAccumulatedObjective(BigNumber objectiveValue)
        {
            PlayerPrefs.SetString(m_EndlessModeStepAccumulatedGoalKey, objectiveValue.Serialize());
            Save();
        }

        public BigNumber GetEndlessModeStepAccumulatedObjective()
        {
            return BigNumber.Deserialize(PlayerPrefs.GetString(m_EndlessModeStepAccumulatedGoalKey,
                BigNumber.Zero.Serialize()));
        }

        public void SaveEndlessModeMoney(BigNumber money)
        {
            PlayerPrefs.SetString(m_EndlessModeMoneyKey, money.Serialize());
            Save();
        }

        public BigNumber GetEndlessModeMoney()
        {
            return BigNumber.Deserialize(PlayerPrefs.GetString(m_EndlessModeMoneyKey, BigNumber.Zero.Serialize()));
        }

        public void SaveEndlessModeSpawnSpaceshipTier(int tierNumber)
        {
            PlayerPrefs.SetInt(m_EndlessModeSpaceshipSpawnTierKey, tierNumber);
            Save();
        }

        public int GetEndlessModeSpawnSpaceshipTier()
        {
            return PlayerPrefs.GetInt(m_EndlessModeSpaceshipSpawnTierKey, 1);
        }

        public void SaveEndlessModeSpawnedRewardLines(int lines)
        {
            PlayerPrefs.SetInt(m_EndlessModeSpawnedRewardLinesKey, lines);
            Save();
        }

        public int GetEndlessModeSpawnedRewardLines()
        {
            return PlayerPrefs.GetInt(m_EndlessModeSpawnedRewardLinesKey, 1);
        }

        public void SaveEndlessModeAddSpaceshipLevel(int level)
        {
            PlayerPrefs.SetInt(m_EndlessModeAddSpaceshipLevelKey, level);
            Save();
        }

        public int GetEndlessModeAddSpaceshipLevel()
        {
            return PlayerPrefs.GetInt(m_EndlessModeAddSpaceshipLevelKey, 0);
        }

        public void SaveEndlessModeMergeLevel(int level)
        {
            PlayerPrefs.SetInt(m_EndlessModeMergeUpgradeLevelKey, level);
            Save();
        }

        public int GetEndlessModeMergeLevel()
        {
            return PlayerPrefs.GetInt(m_EndlessModeMergeUpgradeLevelKey, 0);
        }

        public void SaveEndlessModeRewardLineLevel(int level)
        {
            PlayerPrefs.SetInt(m_EndlessModeMoneyLineLevelKey, level);
            Save();
        }

        public int GetEndlessModeRewardLineLevel()
        {
            return PlayerPrefs.GetInt(m_EndlessModeMoneyLineLevelKey, 0);
        }

        public void SaveEndlessModeCircuitLevel(int level)
        {
            PlayerPrefs.SetInt(m_EndlessModeCircuitLevelKey, level);
            Save();
        }

        public int GetEndlessModeCircuitLevel()
        {
            return PlayerPrefs.GetInt(m_EndlessModeCircuitLevelKey, 0);
        }

        public void SaveEndlessModeInitialized(bool initialized)
        {
            PlayerPrefs.SetInt(m_EndlessModeInitializedKey, initialized ? 1 : 0);
            Save();
        }

        public bool GetEndlessModeInitialized()
        {
            return PlayerPrefs.GetInt(m_EndlessModeInitializedKey, 0) == 1;
        }

        public void SaveEndlessModeCurrentGoal(ProgressionStepInfo goal)
        {
            string serializedGoal = JsonSerialization.ToJson(goal);
            PlayerPrefs.SetString(m_EndlessModeCurrentGoalKey, serializedGoal);
            Save();
        }

        public ProgressionStepInfo GetEndlessModeCurrentGoal()
        {
            string serializedGoal = PlayerPrefs.GetString(m_EndlessModeCurrentGoalKey);
            if (string.IsNullOrEmpty(serializedGoal))
                return new ProgressionStepInfo
                {
                    StepType = ProgressionObjectiveType.CollectCoins,
                    StepGoal = EndlessModeLevelManager.InitialGoal
                };

            return JsonSerialization.FromJson<ProgressionStepInfo>(serializedGoal);
        }

        public void SaveEndlessModeClaimedRewardsIndex(int index)
        {
            var list = GetEndlessModeClaimedRewardIndex();
            if (list.Contains(index))
                return;

            list.Add(index);

            string serializedIndexes = JsonSerialization.ToJson(list);
            PlayerPrefs.SetString(m_EndlessModeClaimedRewardsIndexesKey, serializedIndexes);
            Save();
        }

        public List<int> GetEndlessModeClaimedRewardIndex()
        {
            string serializedIndexes = PlayerPrefs.GetString(m_EndlessModeClaimedRewardsIndexesKey);
            if (string.IsNullOrEmpty(serializedIndexes))
                return new();

            return JsonSerialization.FromJson<List<int>>(serializedIndexes);
        }

        public void SaveShowEndlessModeNotify(bool show)
        {
            PlayerPrefs.SetInt(m_ShowEndlessModeNotify, show ? 1 : 0);
            Save();
        }

        public bool GetShowEndlessModeNotify()
        {
            return PlayerPrefs.GetInt(m_ShowEndlessModeNotify, 1) == 1;
        }

        #endregion

        #region Remote Config

        public void AddStepCompleted()
        {
            int total = GetTotalNumberOfStepsCompleted() + 1;
            PlayerPrefs.SetInt(m_TotalNumberOfStepsCompletedKey, total);
            Save();
        }

        public int GetTotalNumberOfStepsCompleted()
        {
            return PlayerPrefs.GetInt(m_TotalNumberOfStepsCompletedKey, 0);
        }

        public void AddMapCompleted()
        {
            int total = GetTotalNumberOfMapsCompleted() + 1;
            PlayerPrefs.SetInt(m_TotalNumberOfMapsCompletedKey, total);
            Save();
        }

        public int GetTotalNumberOfMapsCompleted()
        {
            return PlayerPrefs.GetInt(m_TotalNumberOfMapsCompletedKey, 0);
        }

        #endregion

        public void SaveNoAdsStatus(bool noAds)
        {
            PlayerPrefs.SetInt(m_NoAdsKey, noAds ? 1 : 0);
            Save();
        }

        public bool GetNoAdsStatus()
        {
            return PlayerPrefs.GetInt(m_NoAdsKey, 0) == 1;
        }

        public void SetPowerUpTutorialFinished()
        {
            PlayerPrefs.SetInt(m_PowerUpsTutorialFinished, 1);
            Save();
        }

        public bool GetPowerUpTutorialFinished()
        {
            return PlayerPrefs.GetInt(m_PowerUpsTutorialFinished, 0) == 1;
        }

        public void SaveRestoredPurchases()
        {
            PlayerPrefs.SetInt(m_RestoredPurchases, 1);
            Save();
        }

        public bool GetRestoredPurchases()
        {
            return PlayerPrefs.GetInt(m_RestoredPurchases, 0) == 1;
        }

        public void AddPrestigeLevel()
        {
            int level = GetPrestigeLevel() + 1;
            PlayerPrefs.SetInt(m_PrestigeLevelKey, level);
            Save();
        }

        public int GetPrestigeLevel()
        {
            return PlayerPrefs.GetInt(m_PrestigeLevelKey, 0);
        }

        public void SaveCurrentPrestigeGoals(int goals)
        {
            PlayerPrefs.SetInt(m_CurrentPrestigeGoalsKey, goals);
            Save();
        }

        public void IncreaseCurrentPrestigeGoals()
        {
            int total = GetCurrentPrestigeGoals() + 1;
            PlayerPrefs.SetInt(m_CurrentPrestigeGoalsKey, total);
            Save();
        }

        public int GetCurrentPrestigeGoals()
        {
            return PlayerPrefs.GetInt(m_CurrentPrestigeGoalsKey, 0);
        }

        public void SetPrestigeTutorialDone()
        {
            PlayerPrefs.SetInt(m_PrestigeTutorialDoneKey, 1);
            Save();
        }

        public bool GetPrestigeTutorialDone()
        {
            return PlayerPrefs.GetInt(m_PrestigeTutorialDoneKey, 0) == 1;
        }

        public void SavePrestigeUnlocked()
        {
            PlayerPrefs.SetInt(m_PrestigeUnlockedKey, 1);
            Save();
        }

        public bool GetPrestigeUnlocked()
        {
            return PlayerPrefs.GetInt(m_PrestigeUnlockedKey, 0) == 1;
        }

        public void SaveInitialTutorial1Done()
        {
            PlayerPrefs.SetInt(m_InitialTutorial1DoneKey, 1);
            Save();
        }

        public bool GetInitialTutorial1Done()
        {
            return PlayerPrefs.GetInt(m_InitialTutorial1DoneKey, 0) == 1;
        }

        public void SaveInitialTutorial2Done()
        {
            PlayerPrefs.SetInt(m_InitialTutorial2DoneKey, 1);
            Save();
        }

        public bool GetInitialTutorial2Done()
        {
            return PlayerPrefs.GetInt(m_InitialTutorial2DoneKey, 0) == 1;
        }

        public void ResetLevelData()
        {
            SaveMoney(0);
            SaveCircuitIndex(0);
            SaveCircuitLevel(0);
            SaveRewardLineLevel(0);
            SaveSpawnedRewardLines(1);
            SaveMergeLevel(0);
            SaveSpawnedSpaceshipTiers(new() { 1 });
            SaveLevelProgressionStep(0);
            SaveSpawnSpaceshipTier(1);
            SaveStepAccumulatedObjective(0);
            SaveAddSpaceshipLevel(0);
            SaveLevelInitialized(false);

            int index = 0;
            while (PlayerPrefs.HasKey(m_BigNumberPlanetHealKey + index))
            {
                SavePlanetHeal(index, BigNumber.Zero);
                index++;
            }

            index = 0;
            while (PlayerPrefs.HasKey(m_BigNumberTargetHealthKey + index))
            {
                SaveTargetHealth(index, BigNumber.Zero);
                index++;
            }
        }

        public void ResetDailyEvents()
        {
            SaveShowLoginReward(true);

            ResetDailyChallenge();
        }

        private void ResetDailyChallenge()
        {
            SaveDailyChallengeAvailable(true);

            var index = GetDailyChallengeIndex();
            index++;
            if (index > GameDailyChallengesSO.Instance.GetLastIndex())
                index = 0;
            SaveDailyChallengeIndex(index);

            PlayerPrefs.DeleteKey(m_DailyChallengeCircuitIndexKey);
            PlayerPrefs.DeleteKey(m_DailyChallengeSpawnedSpaceshipsTiersKey);
            PlayerPrefs.DeleteKey(m_DailyChallengeObjectiveStepKey);
            PlayerPrefs.DeleteKey(m_DailyChallengeStepAccumulatedGoalKey);
            PlayerPrefs.DeleteKey(m_DailyChallengeMoneyKey);
            PlayerPrefs.DeleteKey(m_DailyChallengeSpaceshipSpawnTierKey);
            PlayerPrefs.DeleteKey(m_DailyChallengeSpawnedRewardLinesKey);
            PlayerPrefs.DeleteKey(m_DailyChallengeMergeUpgradeLevelKey);
            PlayerPrefs.DeleteKey(m_DailyChallengeAddSpaceshipLevelKey);
            PlayerPrefs.DeleteKey(m_DailyChallengeMoneyLineLevelKey);
            PlayerPrefs.DeleteKey(m_DailyChallengeCircuitLevelKey);
            PlayerPrefs.DeleteKey(m_DailyChallengeInitializedKey);
        }

        private void Save()
        {
            PlayerPrefs.Save();
        }
    }
}