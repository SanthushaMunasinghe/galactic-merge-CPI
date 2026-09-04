using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Oxtail.SpaceshipIncremental
{
    public class FleetPopupView : MonoBehaviour
    {
        [SerializeField] private GameObject m_Panel;

        [Header("Visuals")] [SerializeField] private Button m_PlayAdButton;
        [SerializeField] private Button m_CancelButton;

        [Header("Panels")] [SerializeField] private CanvasGroup m_Canvas;
        [SerializeField] private GameObject m_OfflinePanel;
        [SerializeField] private GameObject m_DailyPanel;
        [SerializeField] private GameObject m_PowerUpTutorialPanel;
        [SerializeField] private GameObject m_PowerUpPanel;
        [SerializeField] private GameObject m_PrestigeTutorialPanel;

        private void Awake()
        {
            m_PlayAdButton.onClick.AddListener(() => PlayAd());
            m_CancelButton.onClick.AddListener(() => CancelButton());
        }

        private void Start()
        {
            if (LevelManager.Instance is StandardLevelManager)
            {
                if (!Debugger.FakeRewardedVideos) return;

                StartCoroutine(ShowPanel());
            }
        }

        private IEnumerator ShowPanel()
        {
            if (Debugger.ShowFleetPopupOnStart)
            {
                yield return new WaitUntil(() => m_Canvas.alpha >= 1 &&
                                                 !m_OfflinePanel.activeInHierarchy &&
                                                 !m_DailyPanel.activeInHierarchy);

                if (LevelManager.Instance.LevelIndex == 0)
                {
                    yield return new WaitUntil(() =>
                        SaveLoadManager.Instance.GetInitialTutorial1Done() &&
                        SaveLoadManager.Instance.GetInitialTutorial2Done());
                }
                else if (LevelManager.Instance.LevelIndex == 1)
                {
                    yield return new WaitUntil(() => SaveLoadManager.Instance.GetPowerUpTutorialFinished());
                    if (m_PowerUpTutorialPanel != null)
                        yield return new WaitUntil(() => !m_PowerUpTutorialPanel.activeInHierarchy);
                    if (m_PowerUpPanel != null)
                        yield return new WaitUntil(() => !m_PowerUpPanel.activeInHierarchy);
                }

                var prestigeConfig = Prestige.GetConfig();
                if (prestigeConfig.PrestigeSystemActive &&
                    LevelManager.Instance.LevelIndex + 1 == prestigeConfig.MinimumMapToShow)
                {
                    yield return new WaitUntil(() => SaveLoadManager.Instance.GetPrestigeTutorialDone());
                    if (m_PrestigeTutorialPanel != null)
                        yield return new WaitUntil(() => !m_PrestigeTutorialPanel.activeInHierarchy);
                }

                float fleetPercentage = Debugger.ShowFleetPercentage;

                if (fleetPercentage > 0 && LevelManager.Instance.SpaceshipFreePercentage >= fleetPercentage)
                {
                    m_Panel.SetActive(true);
                }
            }
        }

        private void PlayAd()
        {
            m_Panel.SetActive(false);

            while (LevelManager.Instance.CanSpawnSpaceship.Value)
            {
                LevelManager.Instance.AddDefaultSpaceship();
            }
        }

        private void CancelButton()
        {
            m_Panel.SetActive(false);
        }
    }
}