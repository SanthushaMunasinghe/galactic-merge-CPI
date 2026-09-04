using Oxtail.Utils;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Oxtail.SpaceshipIncremental
{
    public struct ShowRemoveAdsPopUp { }

    public class RemoveAdsPopUp : MonoBehaviour
    {
        [SerializeField] private GameObject m_Panel;

        [Header("Button")]
        [SerializeField] private Button m_CloseButton;

        private void Awake()
        {
            m_CloseButton.onClick.AddListener(() => ClosePanel());

            EventManager<ShowRemoveAdsPopUp>.AddListener(ShowPopUp);
        }

        private void OnDestroy()
        {
            EventManager<ShowRemoveAdsPopUp>.RemoveListener(ShowPopUp);
        }

        private void ShowPopUp(ShowRemoveAdsPopUp evt)
        {
            if (Debugger.FakeNoAds) return;
            if (SaveLoadManager.Instance.GetNoAdsStatus())
                return;

            m_Panel.SetActive(true);
        }

        private void PremiumChanged(bool state)
        {
            if (state)
                ClosePanel();
        }

        private void ClosePanel()
        {
            m_Panel.SetActive(false);
        }
    }
}
