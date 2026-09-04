using Oxtail.Utils;
using System;
using System.Collections;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    public struct ShowOfflineRewardsEvent
    {
        public int Coins { get; set; }

        public ShowOfflineRewardsEvent(int coins)
        {
            Coins = coins;
        }
    }

    public class OfflineReardsController : MonoBehaviour
    {
        private int m_ExtraOfflineRewards = 0;

        private const int m_CoinsPerMinute = 1;
        private const int m_MaxOfflineSeconds = 3600 * 6; //6 hours

        private void Start()
        {
            StartCoroutine(CalculateRewardsDelay());
        }

        private IEnumerator CalculateRewardsDelay()
        {
            yield return new WaitForSeconds(1f);

            CalculateExtraRewardsTime();
            CalculateOfflineRewards();
        }

        private void CalculateOfflineRewards()
        {
            string lastTimePlayed = SaveLoadManager.Instance.GetLastTimePlayed();
            if (string.IsNullOrEmpty(lastTimePlayed))
                return;

            DateTime lastTime = DateTime.Parse(lastTimePlayed);
            TimeSpan offlineTime = DateTime.UtcNow - lastTime;

            int maxSeconds = Mathf.Min((int)offlineTime.TotalSeconds, m_MaxOfflineSeconds);
            int minutes = maxSeconds / 60;

            //1 minute offline minimum
            if (minutes > 1)
                GiveOfflineRewards(minutes);
        }

        private void GiveOfflineRewards(int minutes)
        {
            int coins = (m_CoinsPerMinute + m_ExtraOfflineRewards) * minutes;
            EventManager<ShowOfflineRewardsEvent>.TriggerEvent(new ShowOfflineRewardsEvent(coins));
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause)
                SaveOfflineTime();
        }

        private void OnApplicationQuit()
        {
            SaveOfflineTime();
        }

        private void SaveOfflineTime()
        {
            SaveLoadManager.Instance.SaveLastTimePlayed();
        }

        private void CalculateExtraRewardsTime()
        {
            var powerup = GamePowerUpsSO.Instance.GetPowerUpByType(PowerUpType.OfflineRewards);
            int level = SaveLoadManager.Instance.GetPowerUpLevel(powerup.PowerUpID);
            if (level > 0)
                m_ExtraOfflineRewards = (int)powerup.GetUpgradeByLevel(level).Value;
            else
                m_ExtraOfflineRewards = 0;
        }
    }
}
