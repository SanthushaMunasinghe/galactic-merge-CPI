using DG.Tweening;
using Oxtail.Utils;
using System;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    public struct HideLevelHUDEvent { }

    public class LevelCanvas : MonoBehaviour
    {
        [SerializeField] private GameObject m_LevelHUD;
        [SerializeField] private LevelCompleteView m_LevelCompletedPanel;

        private void Awake()
        {
            EventManager<ShowLevelCompleteEvent>.AddListener(OnShowLevelComplete);
            EventManager<ShowLevelHUDEvent>.AddListener(OnShowLevelHUD);
            EventManager<HideLevelHUDEvent>.AddListener(OnHideLevelHUD);
        }

        private void OnDestroy()
        {
            EventManager<ShowLevelCompleteEvent>.RemoveListener(OnShowLevelComplete);
            EventManager<ShowLevelHUDEvent>.RemoveListener(OnShowLevelHUD);
            EventManager<HideLevelHUDEvent>.RemoveListener(OnHideLevelHUD);
        }

        private void OnShowLevelHUD(ShowLevelHUDEvent evt)
        {
            m_LevelCompletedPanel.gameObject.SetActive(false);
            m_LevelHUD.SetActive(true);
        }

        private void OnHideLevelHUD(HideLevelHUDEvent evt)
        {
            m_LevelHUD.SetActive(false);
        }

        public void OnShowLevelComplete(ShowLevelCompleteEvent eventData)
        {
            Time.timeScale = 1;
            m_LevelCompletedPanel.ShowLevelComplete(eventData);
            m_LevelHUD.SetActive(false);
        }
    }
}
