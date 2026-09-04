using Oxtail.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    public class TapRewardView : MonoBehaviour
    {
        [Header("Destination")]
        [SerializeField] private RectTransform m_Destination;

        [Header("Reward")]
        [SerializeField] private RectTransform m_RewardParent;
        [SerializeField] private RewardController m_RewardPrefab;
        [SerializeField] private RewardController m_VideoRewardPrefab;

        private void Awake()
        {
            EventManager<ShowReward>.AddListener(CreateReward);
            EventManager<ShowVideoReward>.AddListener(CreateVideoReward);
        }

        private void OnDestroy()
        {
            EventManager<ShowReward>.RemoveListener(CreateReward);
            EventManager<ShowVideoReward>.RemoveListener(CreateVideoReward);
        }

        private void CreateReward(ShowReward eventData)
        {
            RewardController reward = Instantiate(m_RewardPrefab, m_RewardParent);
            if (reward == null)
                return;

            reward.SetDestination(m_Destination.position);
            reward.SetType(eventData.Type);
        }

        private void CreateVideoReward(ShowVideoReward eventData)
        {
            RewardController reward = Instantiate(m_VideoRewardPrefab, m_RewardParent);
            if (reward == null)
                return;

            reward.SetDestination(m_Destination.position);
            reward.SetType(eventData.Type);
        }
    }
}
