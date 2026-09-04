using Oxtail.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Oxtail.SpaceshipIncremental
{
    public struct ShowGamePauseViewEvent { }
    public struct ResumeGamePauseEvent { }

    public class GamePauseView : MonoBehaviour
    {
        [SerializeField] private GameObject m_PausePanel;
        [SerializeField] private Button m_ResumeButton;

        private float m_TimeScaleBeforePause;

        private void Awake()
        {
            m_ResumeButton.onClick.AddListener(ResumeGame);

            EventManager<ShowGamePauseViewEvent>.AddListener(OnShowGamePauseView);
            EventManager<ResumeGamePauseEvent>.AddListener(OnResumeGamePause);
        }

        private void OnDestroy()
        {
            EventManager<ShowGamePauseViewEvent>.RemoveListener(OnShowGamePauseView);
            EventManager<ResumeGamePauseEvent>.RemoveListener(OnResumeGamePause);
        }

        private void OnShowGamePauseView(ShowGamePauseViewEvent evt)
        {
            PauseGame();
        }

        private void OnResumeGamePause(ResumeGamePauseEvent evt)
        {
            ResumeGame();
        }

        private void PauseGame()
        {
            m_TimeScaleBeforePause = Time.timeScale;
            Time.timeScale = 0f;

            m_PausePanel.SetActive(true);
        }

        private void ResumeGame()
        {             
            Time.timeScale = m_TimeScaleBeforePause;
            m_PausePanel.SetActive(false);
        }
    }
}
