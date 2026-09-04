using Oxtail.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Oxtail.SpaceshipIncremental
{
    public class EndlessModeProgressionView : LevelProgressionView
    {
        protected override void UpdateProgressionInfo(UpdateLevelProgression eventData)
        {
            string objectiveFormatted = eventData.Step.StepGoal.ToString();

            m_ProgressionText.text = $"OBTAIN <color=#FF8400>{objectiveFormatted}</color> COINS";
            m_CurrentObjective = eventData.Step.StepGoal;
            m_ProgressSlider.value = BigNumber.Ratio(LevelManager.Instance.StepAccumulatedObjective.Value, m_CurrentObjective);
        }
    }
}
