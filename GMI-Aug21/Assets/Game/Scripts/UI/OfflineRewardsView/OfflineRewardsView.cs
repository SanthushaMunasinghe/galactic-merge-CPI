using Oxtail.Utils;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Oxtail.SpaceshipIncremental
{
    public class OfflineRewardsView : MonoBehaviour
    {
        [SerializeField] private GameObject m_ShowPanel;
        [SerializeField] private Button m_MultiplierButton;
        [SerializeField] private Button m_ClaimButton;
        [SerializeField] private TMP_Text m_RewardsText;

        private int m_Coins;

        private Coroutine m_AutoHideCoroutine;

        private void Awake()
        {
            m_MultiplierButton.onClick.AddListener(()=> ClaimDoubleRewards());
            m_ClaimButton.onClick.AddListener(()=> ClaimRewards());

            EventManager<ShowOfflineRewardsEvent>.AddListener(ShowPanel);
        }

        private void OnDestroy()
        {
            EventManager<ShowOfflineRewardsEvent>.RemoveListener(ShowPanel);
        }

        private void ShowPanel(ShowOfflineRewardsEvent evt)
        {
            m_Coins = evt.Coins;
            m_RewardsText.text = evt.Coins.ToString();
            m_ShowPanel.SetActive(true);

            if (gameObject.activeInHierarchy)
                m_AutoHideCoroutine = StartCoroutine(AutoHide());
        }

        private void ClaimDoubleRewards()
        {
            m_Coins *= 2;
            ClaimRewards();
        }

        private void ClaimRewards()
        {
            if (m_AutoHideCoroutine != null)
            {
                StopCoroutine(m_AutoHideCoroutine);
                m_AutoHideCoroutine = null;
            }

            LevelManager.Instance.AddMoney(m_Coins, false);

            HidePanel();
        }

        private void HidePanel()
        {
            m_ShowPanel.SetActive(false);
        }

        private IEnumerator AutoHide()
        {
            yield return new WaitForSeconds(10f);

            ClaimRewards();
        }
    }
}
