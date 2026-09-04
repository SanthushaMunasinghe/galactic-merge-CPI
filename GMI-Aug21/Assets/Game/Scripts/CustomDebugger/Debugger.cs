using System.Linq;
using Oxtail.Utils;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    public static class Debugger
    {
        public const bool FakeNoAds = true;
        public static bool FakeShop;
        public static bool IsPremium;
        
        #region RVs

        public static bool FakeRewardedVideos;
        public static int TriggerAfterNumOfCoinsSpend = 0;
        public static int TriggerAfterNumOfUpgrades = 0;
        public static int SideButtonCooldown = 0;
        public static int ShowFreeUpgradeOnPercentage = 0;
        public static int ShowSpaceshipUpgradeAfterLevel = 0;
        public static int ShowMergeUpgradeAfterLevel = 0;
        public static int ShowLineUpgradeAfterLevel = 0;
        public static int ShowCircuitUpgradeAfterLevel = 0;
        public static int FreeUpgradeMergePercentage = 0;
        public static int FreeUpgradeSpaceshipPercentage = 0;
        public static int FreeUpgradeRewardLinePercentage = 0;
        public static int FreeUpgradeCircuitPercentage = 0;
        public static float ShowFleetPercentage = 0;
        public static bool ShowFleetPopupOnStart = false;
        public static float RewardLineRVCountdown = 0;
        public static float RewardLineRVLiveTime = 20;
        public static float RewardLineRVLiveDuration = 60;
        public static int FloatingGiftRVShowPercentage = 35;
        public static int FloatingGiftRVRangeMinTime = 60;
        public static int FloatingGiftRVRangeMaxTime = 180;

        #endregion
        
        public static readonly Prestige Prestige = new();
        public static int ShipMoneyGenerationPercentage;
        public static bool ShowTutorial = false;
        public static int[] ShowSecondaryStepsOnLevels = new int[0];


        public static void ShowGoToLevel(int level)
        {
            int index = Mathf.Clamp(level, 0, GameLevelsSO.Instance.LevelsCount - 1);
            SaveLoadManager.Instance.ResetLevelData();
            SaveLoadManager.Instance.SaveLevelIndex(index);
            GameSceneManager.LoadScene(GameScenesSO.Instance.GameScene);
        }

        public static void ShowInstaFinishLevel()
        {
            EventManager<ResumeGamePauseEvent>.TriggerEvent();
            LevelManager.Instance.FinishLevelInstantly();
        }

        public static void ChangeGameSpeed(float speed)
        {
            speed = Mathf.Max(speed, 0f);
            Time.timeScale = speed;
        }

        public static void ToggleUI(bool enabled)
        {
            if (enabled)
                EventManager<HideLevelHUDEvent>.TriggerEvent();
            else
                EventManager<ShowLevelHUDEvent>.TriggerEvent();
        }

        public static void UnlockPowerUp(PowerUpSO powerUp, int level)
        {
            int powerUpLevel = Mathf.Max(level, 0);
            SaveLoadManager.Instance.SavePowerUpLevel(powerUp.PowerUpID, powerUpLevel);
            SaveLoadManager.Instance.SavePowerUpUnlockPercentage(powerUp.PowerUpID, powerUpLevel > 0 ? 100 : 0);
        }

        public static void UnlockAchievement(AchievementSO achievement)
        {
            SaveLoadManager.Instance.SaveAchievementGoalIndex(achievement.AchievementID, achievement.Goals.Length - 1);
            SaveLoadManager.Instance.SaveAchievementProgression(achievement.Type, achievement.Goals.Last().Goal);
        }

        public static void AddCoins(BigNumber coins)
        {
            LevelManager.Instance.AddMoney(coins);
        }

        public static void AddGems(BigNumber gems)
        {
            SaveLoadManager.Instance.AddGems(gems);
        }

        public static void AddCelestium(BigNumber celestium)
        {
            SaveLoadManager.Instance.AddCelestium(celestium);
        }
    }
}