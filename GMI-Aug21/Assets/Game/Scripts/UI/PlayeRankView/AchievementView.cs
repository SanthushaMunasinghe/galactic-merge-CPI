using Oxtail.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Oxtail.SpaceshipIncremental
{
    public class AchievementView : MonoBehaviour
    {
        [Header("Info")]
        [SerializeField] private Image m_AchievementIcon;
        [SerializeField] private TMP_Text m_Description;

        [Header("Progression")]
        [SerializeField] private TMP_Text m_ProgressionText;
        [SerializeField] private Slider m_ProgressionSlider;

        [Header("Reward")]
        [SerializeField] private Image m_RewardIcon;
        [SerializeField] private HorizontalLayoutGroup m_RewardLayout;
        [SerializeField] private TMP_Text m_RewardText;
        [SerializeField] private Button m_ClaimButton;
        [SerializeField] private GameObject m_RewardsNotClaimedView;
        [SerializeField] private GameObject m_AllRewardsClaimedView;

        private AchievementSO m_AchievementData;

        private void Awake()
        {
            m_ClaimButton.onClick.AddListener(()=> RewardClaimed());
        }

        public void InitAchievement(AchievementSO achivementData)
        {
            m_AchievementData = achivementData;
            UpdateData();
        }

        public void UpdateData()
        {
            m_RewardsNotClaimedView.SetActive(false);
            m_AllRewardsClaimedView.SetActive(false);

            m_AchievementIcon.sprite = m_AchievementData.AchievementIcon;
            int achievementGoalIndex = SaveLoadManager.Instance.GetAchievementGoalIndex(m_AchievementData.AchievementID);

            if (achievementGoalIndex >= m_AchievementData.Goals.Length)
            {
                m_ProgressionSlider.value = 0f;
                m_AllRewardsClaimedView.SetActive(true);
                m_ProgressionText.text = "COMPLETE!";
                return;
            }

            AchievementGoal goal = m_AchievementData.Goals[achievementGoalIndex];
            m_Description.text = m_AchievementData.AchievementDescription.Replace("{x}", $"<color=#FF8400>{goal.Goal.ToString()}</color>");

            BigNumber progression = BigNumber.Abs(SaveLoadManager.Instance.GetAchievementProgression(m_AchievementData.Type));
            m_ProgressionText.text = $"{progression.ToString()}/{goal.Goal.ToString()}";
            m_ProgressionSlider.value = BigNumber.Ratio(progression, goal.Goal);

            m_ClaimButton.interactable = progression >= goal.Goal;

            Rewards(goal.GameRewardInfo);
        }

        private void Rewards(GameRewardInfo reward)
        {
            if (reward.Type == GameRewardType.PowerUpUnlock)
            {
                m_RewardLayout.childControlWidth = false;
                m_RewardIcon.sprite = reward.PowerUp.PowerUpIcon;
                m_RewardIcon.rectTransform.sizeDelta = new Vector2(50f, 50f);
                m_RewardText.text = reward.PowerUp.PowerUpDescription;
                m_RewardText.fontSize = 17f;
                m_RewardText.rectTransform.sizeDelta = new Vector2(110f, m_RewardText.rectTransform.sizeDelta.y);
            }
            else
                m_RewardText.text = reward.Quantity.ToString();

            m_RewardsNotClaimedView.SetActive(true);
        }

        private void RewardClaimed()
        {
            m_ClaimButton.interactable = false;
            int achievementGoalIndex = SaveLoadManager.Instance.GetAchievementGoalIndex(m_AchievementData.AchievementID);

            AchievementGoal goal = m_AchievementData.Goals[achievementGoalIndex];
            if (goal.GameRewardInfo.Type == GameRewardType.PowerUpUnlock)
            {
                if (SaveLoadManager.Instance.GetPowerUpLevel(goal.GameRewardInfo.PowerUp.PowerUpID) == 0)
                {
                    SaveLoadManager.Instance.SavePowerUpUnlockPercentage(goal.GameRewardInfo.PowerUp.PowerUpID, 100);
                    SaveLoadManager.Instance.SavePowerUpLevel(goal.GameRewardInfo.PowerUp.PowerUpID, 1);
                }
            }
            else
                SaveLoadManager.Instance.AddGems(goal.GameRewardInfo.Quantity);

            if (achievementGoalIndex >= m_AchievementData.Goals.Length)
                return;

            SaveLoadManager.Instance.SaveAchievementGoalIndex(m_AchievementData.AchievementID, achievementGoalIndex + 1);
            UpdateData();
        }

        public bool CanClaim()
        {
            if (m_AchievementData == null) 
                return false;

            int achievementGoalIndex = SaveLoadManager.Instance.GetAchievementGoalIndex(m_AchievementData.AchievementID);
            if (achievementGoalIndex >= m_AchievementData.Goals.Length)
                return false;

            AchievementGoal goal = m_AchievementData.Goals[achievementGoalIndex];
            BigNumber progression = SaveLoadManager.Instance.GetAchievementProgression(m_AchievementData.Type);
            if (progression < goal.Goal)
                return false;

            return true;
        }
    }
}
