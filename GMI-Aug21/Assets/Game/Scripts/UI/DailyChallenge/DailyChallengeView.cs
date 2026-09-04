using UnityEngine;
using UnityEngine.UI;

namespace Oxtail.SpaceshipIncremental
{
    public class DailyChallengeView : MonoBehaviour
    {
        [SerializeField] private GameObject m_Panel;
        [SerializeField] private Button m_DailyChallengeButton;
        [SerializeField] private Button m_GoButton;
        [SerializeField] private Button m_CloseButton;
        [SerializeField] private GameObject m_NotifyIcon;

        private void Awake()
        {
            bool available = SaveLoadManager.Instance.GetDailyChallengeAvailable();
            if (!available)
            {
                m_DailyChallengeButton.gameObject.SetActive(false);
                return;
            }
            
            m_DailyChallengeButton.onClick.AddListener(()=> ShowPanel());
            m_GoButton.onClick.AddListener(()=> GoToDailyChallenge());
            m_CloseButton.onClick.AddListener(()=> HidePanel());

            m_NotifyIcon.SetActive(SaveLoadManager.Instance.GetShowDailyChallengeNotify());
        }

        private void GoToDailyChallenge()
        {
            LevelGenerator.Instance.CreateDailyChallenge();
        }

        private void ShowPanel()
        {
            m_Panel.SetActive(true);
            m_NotifyIcon.SetActive(false);
            SaveLoadManager.Instance.SaveShowDailyChallengeNotify(false);
        }

        private void HidePanel()
        {
            m_Panel.SetActive(false);
        }
    }
}
