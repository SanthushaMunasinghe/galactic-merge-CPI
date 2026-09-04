using System;
using System.Collections;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    public class DailyChallengeCompleteView : LevelCompleteView
    {
        protected override void FillData(int index)
        {
            var challenge = GameDailyChallengesSO.Instance.GetDailyChallengeByIndex(index);
            m_FinishedDescription = challenge.LevelFinishedDescription;
            m_NextLevelDescription = string.Empty;
            m_GemsOnCompletion = challenge.GemsOnCompletion;
        }

        protected override IEnumerator ShowGems()
        {
            yield return base.ShowGems();

            StartCoroutine(ShowLevelReady());
        }
    }
}
