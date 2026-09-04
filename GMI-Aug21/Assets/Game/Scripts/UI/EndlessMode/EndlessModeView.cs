using System;
using UnityEngine;
using UnityEngine.UI;

namespace Oxtail.SpaceshipIncremental
{
    public class EndlessModeView : MonoBehaviour
    {
        [SerializeField] private GameObject m_Panel;
        [SerializeField] private GameObject m_NotifyIcon;
        [SerializeField] private Button m_EndlessModeButton;
        [SerializeField] private Button m_GoButton;
        [SerializeField] private Button m_CloseButton;

        private void Awake()
        {
            m_EndlessModeButton.onClick.AddListener(()=> ShowPanel());
            m_GoButton.onClick.AddListener(()=> GoToEndlessMode());
            m_CloseButton.onClick.AddListener(()=> HidePanel());

            m_NotifyIcon.SetActive(SaveLoadManager.Instance.GetShowEndlessModeNotify());
        }

        private void ShowPanel()
        {
            m_Panel.SetActive(true);
            m_NotifyIcon.SetActive(false);
            SaveLoadManager.Instance.SaveShowEndlessModeNotify(false);
        }

        private void HidePanel()
        {
            m_Panel.SetActive(false);
        }

        private void GoToEndlessMode()
        {
            LevelGenerator.Instance.CreateEndlessMode();
        }
    }
}
