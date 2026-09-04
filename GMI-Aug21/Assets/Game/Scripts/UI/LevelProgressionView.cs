using DG.Tweening;
using Oxtail.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Oxtail.SpaceshipIncremental
{
    public struct UpdateGoalValue
    {
        public BigNumber NewValue { get; private set; }

        public UpdateGoalValue(BigNumber newValue)
        {
            NewValue = newValue;
        }
    }

    public class LevelProgressionView : MonoBehaviour
    {
        [SerializeField] protected TMP_Text m_ProgressionText;
        [SerializeField] protected Slider m_ProgressSlider;
        [SerializeField] private RectTransform m_CompletionEffectParent;

        protected BigNumber m_CurrentObjective;
        private Sequence m_ProgressSliderSeq;
        private CanvasGroup m_CanvasGroup;
        private bool m_SeqRunning;

        private void Awake()
        {
            m_CanvasGroup = GetComponentInParent<CanvasGroup>();
            m_ProgressSlider.minValue = 0;
            LevelManager.Instance.StepAccumulatedObjective.OnPropertyChanged += UpdateSlider;

            EventManager<UpdateLevelProgression>.AddListener(UpdateProgressionInfo);
            EventManager<UpdateGoalValue>.AddListener(UpdateGoalValueInfo);
        }

        private void OnDestroy()
        {
            if (LevelManager.Instance != null)
                LevelManager.Instance.StepAccumulatedObjective.OnPropertyChanged -= UpdateSlider;

            EventManager<UpdateLevelProgression>.RemoveListener(UpdateProgressionInfo);
            EventManager<UpdateGoalValue>.RemoveListener(UpdateGoalValueInfo);
        }

        protected virtual void UpdateProgressionInfo(UpdateLevelProgression eventData)
        {
            string objectiveFormatted = eventData.Step.StepGoal.ToString();
            int currentStep = LevelManager.Instance.LevelProgressionStepIndex + 1;
            int maxStep = LevelManager.Instance.LevelProgressionSteps;

            m_ProgressionText.text = $"Goal ({currentStep} / {maxStep}): " + eventData.Step.StepType switch
            {
                ProgressionObjectiveType.CollectCoins => $"OBTAIN <color=#FF8400>{objectiveFormatted}</color> COINS",
                ProgressionObjectiveType.SpawnSpaceships => $"ADD <color=#FF8400>{objectiveFormatted}</color> SPACESHIPS",
                ProgressionObjectiveType.MergeSpaceShips => $"MERGE SPACESHIPS <color=#FF8400>{objectiveFormatted}</color> TIMES",
                ProgressionObjectiveType.AddRewardLines => $"ADD <color=#FF8400>{objectiveFormatted}</color> REWARD LINES",
                ProgressionObjectiveType.UpgradeCircuit => $"UPGRADE CIRCUIT <color=#FF8400>{objectiveFormatted}</color> TIMES",
                ProgressionObjectiveType.UseSpeedBoost => $"USE SPEED BOOST <color=#FF8400>{objectiveFormatted}</color> TIMES",
                ProgressionObjectiveType.CreateTierSpaceship => $"CREATE A TIER <color=#FF8400>{objectiveFormatted}</color> SPACESHIP",
                ProgressionObjectiveType.RevivePlanet => "<color=#FF8400>REVIVE</color> PLANETS",
                ProgressionObjectiveType.DestroyTargets => "<color=#FF8400>DESTROY</color> IT!",
                ProgressionObjectiveType.CrossRewardLines => $"CROSS REWARD LINES <color=#FF8400>{objectiveFormatted}</color> TIMES",
                ProgressionObjectiveType.CollectShinyGifts => $"COLLECT <color=#FF8400>{objectiveFormatted}</color> SHINY GIFTS",
                ProgressionObjectiveType.MaxCircuitSpaceships => "REACH MAX SPACESHIPS AMOUNT",
                ProgressionObjectiveType.CompletesInTime => $"LEVEL COMPLETE IN <color=#FF8400>{objectiveFormatted}</color>",
                ProgressionObjectiveType.MaxGemsInTime => "GET <color=#FF8400>GEMS</color> BEFORE TIME RUNS OUT!"
            };

            if (m_ProgressSliderSeq != null)
                StartCoroutine(WaitForSliderFinish(eventData.Step.StepGoal));
            else
            {
                m_CurrentObjective = eventData.Step.StepGoal;
                m_ProgressSlider.value = BigNumber.Ratio(LevelManager.Instance.StepAccumulatedObjective.Value, m_CurrentObjective);
            }
        }


        private IEnumerator WaitForSliderFinish(BigNumber goal)
        {
            yield return new WaitUntil(()=> !m_SeqRunning);

            m_CurrentObjective = goal;
            m_ProgressSlider.value = BigNumber.Ratio(LevelManager.Instance.StepAccumulatedObjective.Value, m_CurrentObjective);
        }

        private void UpdateGoalValueInfo(UpdateGoalValue evt)
        {
            m_CurrentObjective = evt.NewValue;
        }

        private void UpdateSlider(BigNumber value)
        {
            if (m_CanvasGroup == null)
                m_CanvasGroup = GetComponentInParent<CanvasGroup>();

            StartCoroutine(UpdateSliderInternal(value));
        }

        private IEnumerator UpdateSliderInternal(BigNumber value)
        {
            yield return new WaitUntil(()=> m_CanvasGroup.alpha >= 1f);
            yield return new WaitUntil(() => !m_SeqRunning);

            float ratio = BigNumber.Ratio(value, m_CurrentObjective);
            m_ProgressSlider.DOKill();
            m_ProgressSliderSeq = DOTween.Sequence();
            m_ProgressSliderSeq.Append(m_ProgressSlider.DOValue(ratio, 0.1f)
            .SetEase(Ease.InExpo));

            if (ratio >= 1f)
                m_ProgressSliderSeq.Join(m_ProgressSlider.RectTransform().DOPunchScale(Vector3.one * 0.2f, 0.2f, 1));

            m_ProgressSliderSeq.OnComplete(()=>
            {
                m_SeqRunning = false;
                if (ratio >= 1f)
                {
                    m_CompletionEffectParent.gameObject.SetActive(false);
                    m_CompletionEffectParent.gameObject.SetActive(true);
                }
            });

            m_SeqRunning = true;

            m_ProgressSliderSeq.Play();
        }
    }
}
