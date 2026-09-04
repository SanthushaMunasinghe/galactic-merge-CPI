using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Oxtail.SpaceshipIncremental
{
    public class UpgradesTutorialController : MonoBehaviour
    {
        [SerializeField] private GameObject m_Panel;
        [SerializeField] private GameObject m_Background;
        [SerializeField] private GameObject m_LeftSideButtons;

        [Header("Panels")]
        [SerializeField] private CanvasGroup m_UICanvasGroup;
        [SerializeField] private GameObject m_OfflineRewardsPanel;
        [SerializeField] private GameObject m_DailyRewardsPanel;

        [Header("Power Up")]
        [SerializeField] private GameObject m_PowerUpsPanel;
        [SerializeField] private ScrollRect m_PowerUpsPanelScroll;
        [SerializeField] private Button m_PowerUpsCloseButton;
        [SerializeField] private GameObject m_PowerUpContent;

        [Header("Buttons")]
        [SerializeField] private Button m_ExpandButton;
        [SerializeField] private Button m_UpgradesButton;

        [Header("Fingers")]
        [SerializeField] private GameObject m_TutorialFinger1;
        [SerializeField] private GameObject m_TutorialFinger2;
        [SerializeField] private GameObject m_TutorialFinger3;

        private void Awake()
        {
            m_ExpandButton.onClick.AddListener(()=> ExpandButtons());
            m_UpgradesButton.onClick.AddListener(()=> ShowUpgrades());

            if (!SaveLoadManager.Instance.GetPowerUpTutorialFinished())
            {
                m_LeftSideButtons.SetActive(false);
                StartCoroutine(ShowTutorial());
            }
        }

        private IEnumerator ShowTutorial()
        {
            yield return new WaitForSeconds(0.25f);

            yield return new WaitUntil(()=> m_UICanvasGroup.alpha == 1);

            yield return new WaitUntil(() => !m_DailyRewardsPanel.activeInHierarchy &&
            !m_OfflineRewardsPanel.activeInHierarchy);

            Time.timeScale = 0;

            yield return new WaitForSecondsRealtime(0.25f);

            m_Panel.SetActive(true);

            yield return new WaitForSecondsRealtime(0.25f);

            m_TutorialFinger1.SetActive(true);

            SaveLoadManager.Instance.SetPowerUpTutorialFinished();
        }

        private void ExpandButtons()
        {
            StartCoroutine(ShowUpgradesFinger());
            m_ExpandButton.interactable = false;
            m_TutorialFinger1.SetActive(false);
        }

        private IEnumerator ShowUpgradesFinger()
        {
            yield return new WaitForSecondsRealtime(0.25f);

            m_TutorialFinger2.SetActive(true);
        }

        private void ShowUpgrades()
        {
            m_TutorialFinger2.SetActive(false);
            m_Background.SetActive(false);
            m_PowerUpsPanel.SetActive(true);
            m_PowerUpsCloseButton.enabled = false;
            m_PowerUpsPanelScroll.enabled = false;
            StartCoroutine(ShowPowerUpFinger());
        }

        private IEnumerator ShowPowerUpFinger()
        {
            yield return new WaitForSecondsRealtime(0.25f);

            var powerUpView = m_PowerUpContent.transform.GetChild(0).GetComponent<PowerUpView>();
            if (!powerUpView.CanUpgrade)
            {
                m_PowerUpsCloseButton.enabled = true;
                m_PowerUpsPanelScroll.enabled = true;
                m_Panel.SetActive(false);
                Time.timeScale = 1;
                m_LeftSideButtons.SetActive(true);
                yield break;
            }

            var powerUpButton = powerUpView.UpgradeButton;
            m_TutorialFinger3.transform.position = powerUpButton.transform.position;
            m_TutorialFinger3.gameObject.SetActive(true);
            bool upgraded = false;
            powerUpButton.onClick.AddListener(()=> upgraded = true);

            yield return new WaitUntil(() => upgraded);

            m_TutorialFinger3.SetActive(false);

            yield return new WaitForSecondsRealtime(1f);

            m_PowerUpsPanel.SetActive(false);

            m_Panel.SetActive(false);
            Time.timeScale = 1;
            m_LeftSideButtons.SetActive(true);
            m_PowerUpsCloseButton.enabled = true;
            m_PowerUpsPanelScroll.enabled = true;
        }
    }
}
