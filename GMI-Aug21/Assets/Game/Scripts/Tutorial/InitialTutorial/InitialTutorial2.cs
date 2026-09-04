using DG.Tweening;
using Oxtail.Utils;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Oxtail.SpaceshipIncremental
{
    public class InitialTutorial2 : MonoBehaviour
    {
        [SerializeField] private GameObject m_TutorialPanel;
        [SerializeField] private Button m_MergeButton;
        [SerializeField] private GameObject m_Arrows;
        [SerializeField] private GameObject m_TutorialFinger;
        [SerializeField] private TMP_Text m_ProgressionText;

        [Header("Daily Rewards")]
        [SerializeField] private GameObject m_DailyRewardPanel;
        [SerializeField] private GameObject m_OfflineRewardPanel;
        [SerializeField] private CanvasGroup m_Canvas;

        private float m_TimeScaleBeforePause;
        private Coroutine m_TutorialCoroutine;

        private void Awake()
        {
            if (!Debugger.ShowTutorial) return;

            LevelManager.Instance.Money.OnPropertyChanged += OnMoneyChanged;
            LevelManager.Instance.CanMerge.OnPropertyChanged += OnCanMergeChanged;
            m_MergeButton.onClick.AddListener(() => OnAddSpaceshipButtonClicked());
            EventManager<UpdateLevelProgression>.AddListener(UpdateProgressionInfo);
        }

        private void OnDestroy()
        {
            LevelManager.Instance.Money.OnPropertyChanged -= OnMoneyChanged;
            LevelManager.Instance.CanMerge.OnPropertyChanged -= OnCanMergeChanged;
            EventManager<UpdateLevelProgression>.RemoveListener(UpdateProgressionInfo);
        }

        private void UpdateProgressionInfo(UpdateLevelProgression eventData)
        {
            string objectiveFormatted = eventData.Step.StepGoal.ToString();
            int currentStep = LevelManager.Instance.LevelProgressionStepIndex + 1;
            int maxStep = LevelManager.Instance.LevelProgressionSteps;

            m_ProgressionText.text = $"GOAL ({currentStep} / {maxStep}): " + eventData.Step.StepType switch
            {
                ProgressionObjectiveType.CollectCoins => $"OBTAIN <color=#FF8400>{objectiveFormatted}</color> COINS",
                ProgressionObjectiveType.SpawnSpaceships => $"ADD <color=#FF8400>{objectiveFormatted}</color> SPACESHIPS",
                ProgressionObjectiveType.MergeSpaceShips => $"MERGE SPACESHIPS <color=#FF8400>{objectiveFormatted}</color> TIMES",
                ProgressionObjectiveType.AddRewardLines => $"ADD <color=#FF8400>{objectiveFormatted}</color> REWARD LINES",
                ProgressionObjectiveType.UpgradeCircuit => $"UPGRADE CIRCUIT <color=#FF8400>{objectiveFormatted}</color> TIMES",
                ProgressionObjectiveType.UseSpeedBoost => $"USE SPEED BOOST <color=#FF8400>{objectiveFormatted}</color> TIMES",
                ProgressionObjectiveType.CreateTierSpaceship => $"CREATE A TIER <color=#FF8400>{objectiveFormatted}</color> SPACESHIP",
                ProgressionObjectiveType.RevivePlanet => "<color=#FF8400>REVIVE</color> THE STARS",
                ProgressionObjectiveType.DestroyTargets => "<color=#FF8400>DESTROY</color> IT!",
                ProgressionObjectiveType.CrossRewardLines => $"CROSS REWARD LINES <color=#FF8400>{objectiveFormatted}</color> TIMES",
                ProgressionObjectiveType.CollectShinyGifts => $"COLLECT <color=#FF8400>{objectiveFormatted}</color> SHINY GIFTS",
                ProgressionObjectiveType.MaxCircuitSpaceships => $"REACH MAX SPACESHIPS AMOUNT",
                ProgressionObjectiveType.CompletesInTime => $"LEVEL COMPLETE IN <color=#FF8400>{objectiveFormatted}</color>",
                ProgressionObjectiveType.MaxGemsInTime => $"GET <color=#FF8400>GEMS</color> BEFORE TIME RUNS OUT!"
            };
        }

        private void OnCanMergeChanged(bool canMerge)
        {
            if (!canMerge)
                return;

            OnMoneyChanged(LevelManager.Instance.Money.Value);
        }

        private void OnMoneyChanged(BigNumber number)
        {
            if (SaveLoadManager.Instance.GetInitialTutorial2Done())
                return;

            if (!SaveLoadManager.Instance.GetInitialTutorial1Done())
                return;

            if (SaveLoadManager.Instance.GetLevelProgressionStep() > 1)
            {
                if (!SaveLoadManager.Instance.GetInitialTutorial2Done())
                    SaveLoadManager.Instance.SaveInitialTutorial2Done();
                
                return;
            }

            if (!LevelManager.Instance.CanMerge.Value)
                return;

            if (number >= GameUpgradesCostSO.Instance.GetMergeUpgradeCost())
                m_TutorialCoroutine = StartCoroutine(DoTutorial());
        }

        private IEnumerator DoTutorial()
        {
            m_MergeButton.gameObject.SetActive(false);

            yield return new WaitUntil(() => m_Canvas.alpha >= 1 &&
            !m_DailyRewardPanel.activeInHierarchy &&
            !m_OfflineRewardPanel.activeInHierarchy);

            m_TimeScaleBeforePause = Time.timeScale;
            Time.timeScale = 0f;
            m_TutorialPanel.SetActive(true);
            m_Arrows.transform.RectTransform().DOAnchorPosY(m_Arrows.transform.RectTransform().anchoredPosition.y - 10f, 0.5f)
                .SetUpdate(true)
                .SetLoops(-1, LoopType.Yoyo);
            m_MergeButton.gameObject.SetActive(true);

            yield return new WaitForSecondsRealtime(2f);

            m_TutorialFinger.SetActive(true);
        }

        private void OnAddSpaceshipButtonClicked()
        {
            StopCoroutine(m_TutorialCoroutine);
            m_TutorialCoroutine = null;
            m_TutorialPanel.SetActive(false);
            Time.timeScale = m_TimeScaleBeforePause;
            m_Arrows.transform.RectTransform().DOKill();
            SaveLoadManager.Instance.SaveInitialTutorial2Done();
        }
    }
}