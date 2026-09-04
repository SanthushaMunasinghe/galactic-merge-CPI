using System.Collections;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    public class StandardLevelCompleteView : LevelCompleteView
    {
        [Header("Unlock")]
        [SerializeField] private PowerUpUnlockView m_PowerUpUnlockView;

        private LevelSO m_LevelData;

        protected override void FillData(int index)
        {
            m_LevelData = GameLevelsSO.Instance.GetLevelByIndex(index);
            m_FinishedDescription = m_LevelData.LevelFinishedDescription;
            m_NextLevelDescription = GameLevelsSO.Instance.GetLevelByIndex(index + 1).LevelDescription;
            m_GemsOnCompletion = m_LevelData.GemsOnCompletion;
        }

        protected override IEnumerator ShowGems()
        {
            yield return base.ShowGems();

            StartCoroutine(ShowPowerUpUnlock());
        }

        private IEnumerator ShowPowerUpUnlock()
        {
            if (m_LevelData.PowerUpReward != null)
            {
                bool powerUpUnlocked = SaveLoadManager.Instance.GetPowerUpLevel(m_LevelData.PowerUpReward.PowerUpID) > 0;
                if (!powerUpUnlocked)
                {
                    yield return m_PowerUpUnlockView.ShowUnlock(m_LevelData);
                }
            }

            m_PowerUpUnlockView.HidePowerUpInfo();
            StartCoroutine(ShowLevelReady());
        }
    }
}
