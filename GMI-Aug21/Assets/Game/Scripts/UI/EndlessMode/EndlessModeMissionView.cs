using Oxtail.Utils;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Oxtail.SpaceshipIncremental
{
    public class EndlessModeMissionView : MonoBehaviour
    {
        [Header("Visual")]
        [SerializeField] private TMP_Text m_DescriptionText;
        [SerializeField] private TMP_Text m_ProgressionText;
        [SerializeField] private Slider m_ProgressionSlider;

        [Header("Reward")]
        [SerializeField] private TMP_Text m_RewardAmountText;
        [SerializeField] private Button m_RewardClaimButton;

        private BigNumber m_MissionGoal;
        public int RewardAmount { get; private set; }

        public event Action<EndlessModeMissionView> OnRewardClaimed;

        private void Awake()
        {
            m_RewardClaimButton.onClick.AddListener(()=> ClaimView());
        }

        public void SetInfo(BigNumber goal, int rewardAmount)
        {
            m_DescriptionText.text = $"Obtain {goal} Coins";
            m_MissionGoal = goal;
            RewardAmount = rewardAmount;
            m_RewardAmountText.text = RewardAmount.ToString();
        }

        private void Update()
        {
            BigNumber currentProgression = SaveLoadManager.Instance.GetEndlessModeStepAccumulatedObjective();
            m_ProgressionText.text = $"{BigNumber.Min(currentProgression, m_MissionGoal).ToString()} / {m_MissionGoal.ToString()}";
            m_ProgressionSlider.value = BigNumber.Ratio(currentProgression, m_MissionGoal);

            m_RewardClaimButton.interactable = currentProgression >= m_MissionGoal;
        }

        private void ClaimView()
        {
            SaveLoadManager.Instance.SaveEndlessModeClaimedRewardsIndex(RewardAmount - 1);
            SaveLoadManager.Instance.AddCelestium(RewardAmount - 1);
            OnRewardClaimed?.Invoke(this);
            Destroy(gameObject);
        }

        public bool CanClaim()
        {
            return SaveLoadManager.Instance.GetEndlessModeStepAccumulatedObjective() >= m_MissionGoal; 
        }
    }
}
