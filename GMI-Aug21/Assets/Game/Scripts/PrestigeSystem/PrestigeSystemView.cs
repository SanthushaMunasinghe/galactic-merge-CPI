using DG.Tweening;
using Oxtail.Utils;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Oxtail.SpaceshipIncremental
{
    public class PrestigeSystemView : MonoBehaviour
    {
        [SerializeField] private GameObject m_PrestigePanel;
        [SerializeField] private Button m_PrestigeButton;
        [SerializeField] private GameObject m_PrestigeButtonNotify;

        [Header("Prestige Info")]
        [SerializeField] private TMP_Text m_CurrentPrestigeLevelText;
        [SerializeField] private TMP_Text m_NextPrestigeLevelText;
        [SerializeField] private TMP_Text m_CurrentPrestigeBonusText;
        [SerializeField] private TMP_Text m_NextPrestigeBonusText;
        [SerializeField] private TMP_Text m_CurrentPrestigeGoalsText;
        [SerializeField] private Slider m_PrestigeProgressSlider;
        [SerializeField] private Button m_CloseButton;
        [SerializeField] private Button m_WatchRVButton;
        [SerializeField] private Button m_SpendGemsButton;
        [SerializeField] private Button m_UpgradePrestigeButton;

        [Header("Tutorial")]
        [SerializeField] private GameObject m_PrestigeTutorialPanel;
        [SerializeField] private GameObject m_PrestigeTutorialFinger;
        [SerializeField] private Button m_PrestigeTutorialButton;

        [Header("Panels")]
        [SerializeField] private CanvasGroup m_UICanvasGroup;
        [SerializeField] private GameObject m_OfflineRewardsPanel;
        [SerializeField] private GameObject m_DailyRewardsPanel;

        [Header("Transition")]
        [SerializeField] private Image m_TransitionImage;

        private void Awake()
        {
            if (LevelManager.Instance is not StandardLevelManager || !Prestige.GetConfig().PrestigeSystemActive)
            {
                m_PrestigeButton.gameObject.SetActive(false);
                return;
            }

            m_PrestigeButton.onClick.AddListener(()=> ShowPanel());
            m_PrestigeTutorialButton.onClick.AddListener(()=> FinishTutorial());
            m_CloseButton.onClick.AddListener(()=> ClosePanel());
            m_WatchRVButton.onClick.AddListener(()=> ShowRV());
            m_SpendGemsButton.onClick.AddListener(()=> SpendGems());
            m_UpgradePrestigeButton.onClick.AddListener(()=> UpgradePrestige());

            if (!SaveLoadManager.Instance.GetPrestigeUnlocked())
            {
                m_PrestigeButton.gameObject.SetActive(false);

                if (SaveLoadManager.Instance.GetMaxLevelUnlockedIndex() + 1 >= Prestige.GetConfig().MinimumMapToShow)
                {
                    if (!SaveLoadManager.Instance.GetPrestigeTutorialDone())
                        StartCoroutine(DoPrestigeTutorial());
                }
            }
        }

        private void OnEnable()
        {
            CheckNotify();
        }

        private void CheckNotify()
        {
            var prestigeConfig = Prestige.GetConfig();
            int prestigeLevel = SaveLoadManager.Instance.GetPrestigeLevel();
            int prestigeGoals = SaveLoadManager.Instance.GetCurrentPrestigeGoals();
            int nextPrestigeGoals = Mathf.CeilToInt(prestigeConfig.InitialGoalsAmount * Mathf.Pow(prestigeLevel, prestigeConfig.GoalIncreaseExponential));
            m_PrestigeButtonNotify.gameObject.SetActive(prestigeGoals >= nextPrestigeGoals);
        }

        private IEnumerator DoPrestigeTutorial()
        {
            yield return new WaitForSeconds(0.25f);

            yield return new WaitUntil(() => m_UICanvasGroup.alpha == 1);

            yield return new WaitUntil(() => !m_DailyRewardsPanel.activeInHierarchy &&
            !m_OfflineRewardsPanel.activeInHierarchy);

            Time.timeScale = 0;

            yield return new WaitForSecondsRealtime(0.25f);

            m_PrestigeTutorialPanel.SetActive(true);

            yield return new WaitForSecondsRealtime(0.25f);

            m_PrestigeTutorialFinger.SetActive(true);

            SaveLoadManager.Instance.SetPrestigeTutorialDone();
            SaveLoadManager.Instance.SavePrestigeUnlocked();
        }

        private void FinishTutorial()
        {
            ShowPanel();
            m_PrestigeTutorialPanel.SetActive(false);
            m_PrestigeTutorialFinger.SetActive(false);
            m_PrestigeButton.gameObject.SetActive(true);
            Time.timeScale = 1;
        }

        private void ShowPanel()
        {
            FillPrestigeData();
            m_PrestigePanel.SetActive(true);
        }

        private void FillPrestigeData()
        {
            var prestigeConfig = Prestige.GetConfig();
            int prestigeLevel = SaveLoadManager.Instance.GetPrestigeLevel();

            m_CurrentPrestigeLevelText.text = prestigeLevel.ToString();
            m_NextPrestigeLevelText.text = (prestigeLevel + 1).ToString();
            m_CurrentPrestigeBonusText.text = $"x{1 + (prestigeLevel * prestigeConfig.CoinsBonusMultiplier)}";
            m_NextPrestigeBonusText.text = $"x{1 + ((prestigeLevel + 1) * prestigeConfig.CoinsBonusMultiplier)}";

            UpdateProgressBar();
        }

        private void UpdateProgressBar()
        {
            var prestigeConfig = Prestige.GetConfig();
            int prestigeLevel = SaveLoadManager.Instance.GetPrestigeLevel();
            int prestigeGoals = SaveLoadManager.Instance.GetCurrentPrestigeGoals();
            int nextPrestigeGoals = Mathf.CeilToInt(prestigeConfig.InitialGoalsAmount * Mathf.Pow(prestigeLevel + 1, prestigeConfig.GoalIncreaseExponential));
            prestigeGoals = Mathf.Min(prestigeGoals, nextPrestigeGoals);
            m_CurrentPrestigeGoalsText.text = $"Goals:{prestigeGoals}/{nextPrestigeGoals}";
            m_PrestigeProgressSlider.minValue = 0;
            m_PrestigeProgressSlider.maxValue = nextPrestigeGoals;
            m_PrestigeProgressSlider.value = prestigeGoals;

            m_WatchRVButton.interactable = prestigeGoals < nextPrestigeGoals;
            m_SpendGemsButton.interactable = prestigeGoals < nextPrestigeGoals &&
                SaveLoadManager.Instance.GetGems() >= prestigeConfig.FreeGoalGems;
            m_UpgradePrestigeButton.interactable = prestigeGoals >= nextPrestigeGoals;
        }

        private void ShowRV()
        {
            IncreaseGoalAmount();
        }

        private void SpendGems()
        {
            SaveLoadManager.Instance.AddGems(-Prestige.GetConfig().FreeGoalGems);
            IncreaseGoalAmount();
        }

        private void IncreaseGoalAmount()
        {
            SaveLoadManager.Instance.IncreaseCurrentPrestigeGoals();
            UpdateProgressBar();
        }

        private void UpgradePrestige()
        {
            SaveLoadManager.Instance.AddPrestigeLevel();
            SaveLoadManager.Instance.SaveCurrentPrestigeGoals(0);
            SaveLoadManager.Instance.ResetLevelData();
            SaveLoadManager.Instance.SaveLevelIndex(0);
            SaveLoadManager.Instance.SaveMaxLevelUnlockedIndex(0);
            ShowTransition();
        }

        private void ShowTransition()
        {
            m_TransitionImage.material.SetFloat("_HalftoneFade", 0.1f);
            m_TransitionImage.gameObject.SetActive(true);
            m_TransitionImage.material.DOFloat(0f, "_HalftoneFade", 2f)
            .OnComplete(() =>
            {
                LevelGenerator.Instance.CreateNextLevel();
                m_TransitionImage.material.DOFloat(0.1f, "_HalftoneFade", 1f);
            });
        }

        private void ClosePanel()
        {
            m_PrestigePanel.SetActive(false);
        }
    }
}
