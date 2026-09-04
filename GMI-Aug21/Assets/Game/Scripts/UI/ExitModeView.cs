using System;
using UnityEngine;
using UnityEngine.UI;

namespace Oxtail.SpaceshipIncremental
{
    public class ExitModeView : MonoBehaviour
    {
        [SerializeField] private GameObject m_Panel;
        [SerializeField] private Button m_ExitModeButton;
        [SerializeField] private Button m_CloseButton;
        [SerializeField] private Button m_ExitButton;

        private void Awake()
        {
            m_ExitModeButton.onClick.AddListener(()=> ShowPanel());
            m_CloseButton.onClick.AddListener(()=> ClosePanel());
            m_ExitButton.onClick.AddListener(()=> GoToStandarLevel());
        }

        private void ShowPanel()
        {
            m_Panel.SetActive(true);
        }

        private void ClosePanel()
        {
            m_Panel.SetActive(false);
        }

        private void GoToStandarLevel()
        {
            LevelGenerator.Instance.CreateNextLevel();
        }
    }
}
