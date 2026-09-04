using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Oxtail.SpaceshipIncremental
{
    public class BadgesViewController : MonoBehaviour
    {
        [SerializeField] private GameObject m_Panel;
        [SerializeField] private Button m_BadgesButton;
        [SerializeField] private Button m_CloseButton;
        [SerializeField] private GameObject m_NotifyIcon;

        [Header("Ranks")]
        [SerializeField] private Image m_RankBadge;
        [SerializeField] private TMP_Text m_RankName;

        [Header("Achievements")]
        [SerializeField] private RectTransform m_AchievementsParent;
        [SerializeField] private AchievementView m_AchievementViewPrefab;

        private List<AchievementView> m_Achievements = new();

        private void Awake()
        {
            m_BadgesButton.onClick.AddListener(()=> ShowPanel());
            m_CloseButton.onClick.AddListener(()=> CloseButtonPressed());

            CreateAchievements();
            CheckNotifyToggle();

            SaveLoadManager.Instance.OnAchievementUpdated += AchievementUpdated;
        }

        private void OnDestroy()
        {
            SaveLoadManager.Instance.OnAchievementUpdated -= AchievementUpdated;
        }

        private void ShowPanel()
        {
            ShowRankInfo();
            UpdateAchievementsInfo();
            m_Panel.SetActive(true);
            m_NotifyIcon.SetActive(false);
        }

        private void ShowRankInfo()
        {
            int completedAchievementsPercentage = GameAchievementsSO.Instance.CompletedAchievementsPercentage();
            var rank = GamePlayerRanksSO.Instance.GetRankByPercentage(completedAchievementsPercentage);
            m_RankName.text = rank.RankName;
            m_RankBadge.sprite = rank.RankBadge;
        }

        private void CreateAchievements()
        {
            foreach(var achivement in GameAchievementsSO.Instance.Achievements)
            {
                var view = Instantiate(m_AchievementViewPrefab, m_AchievementsParent);
                view.InitAchievement(achivement);
                m_Achievements.Add(view);
            }
        }

        private void AchievementUpdated()
        {
            CheckNotifyToggle();
        }

        private void CheckNotifyToggle()
        {
            m_NotifyIcon.SetActive(false);

            foreach (var achievement in m_Achievements)
            {
                if (achievement.CanClaim())
                {
                    m_NotifyIcon.SetActive(true);
                    return;
                }
            }
        }

        private void UpdateAchievementsInfo()
        {
            foreach (var achievement in m_Achievements)
            {
                achievement.UpdateData();
            }
        }

        private void CloseButtonPressed()
        {
            m_Panel.SetActive(false);
        }
    }
}
