using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;

namespace Oxtail.SpaceshipIncremental
{
    public class EndlessModeMissionsView : MonoBehaviour
    {
        [SerializeField] private GameObject m_Panel;
        [SerializeField] private Button m_MissionsButton;
        [SerializeField] private GameObject m_NotifyIcon;
        [SerializeField] private Button m_CloseButton;

        [Header("Mission View")]
        [SerializeField] private EndlessModeMissionView m_ViewPrefab;
        [SerializeField] private RectTransform m_ViewParent;

        private List<EndlessModeMissionView> m_CreatedViews = new();

        private const int m_MaxViews = 10;

        private void Awake()
        {
            CreateViews();

            m_MissionsButton.onClick.AddListener(()=> ShowPanel());
            m_CloseButton.onClick.AddListener(()=> HidePanel());
        }

        private void Update()
        {
            CheckCanClaimNotify();
        }

        private void ShowPanel()
        {
            m_Panel.SetActive(true);
            m_NotifyIcon.SetActive(true);
        }

        private void HidePanel()
        {
            m_Panel.SetActive(false);
        }

        private void CreateViews()
        {
            var indexes = SaveLoadManager.Instance.GetEndlessModeClaimedRewardIndex();
            for (int i = 0; i < int.MaxValue; i++)
            {
                if (m_CreatedViews.Count == m_MaxViews)
                    break;

                if (indexes.Contains(i))
                    continue;

                if (m_CreatedViews.Any(x => x.RewardAmount - 1 == i))
                    continue;

                CreateNewView(i);
            }
        }

        private void CreateNewView(int index)
        {
            var view = Instantiate(m_ViewPrefab, m_ViewParent);
            view.SetInfo(EndlessModeLevelManager.Instance.GetGoalByIndex(index), index + 1);
            view.OnRewardClaimed += OnRewardClaimed;
            m_CreatedViews.Add(view);
        }

        private void OnRewardClaimed(EndlessModeMissionView view)
        {
            m_CreatedViews.Remove(view);
            CreateViews();
        }

        private void CheckCanClaimNotify()
        {
            foreach (var view in m_CreatedViews)
            {
                if (view.CanClaim())
                {
                    m_NotifyIcon.SetActive(true);
                    return;
                }
            }

            m_NotifyIcon.SetActive(false);
        }
    }
}
