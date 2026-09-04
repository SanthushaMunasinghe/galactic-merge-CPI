using Oxtail.Utils;
using System;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    public struct ShowAdBreak { }
    public struct HideAdBreak { }

    public class AdBreakController : MonoBehaviour
    {
        [SerializeField] private GameObject m_Panel;

        private void Awake()
        {
            EventManager<ShowAdBreak>.AddListener(ShowPanel);
            EventManager<HideAdBreak>.AddListener(HidePanel);
        }

        private void OnDestroy()
        {
            EventManager<ShowAdBreak>.RemoveListener(ShowPanel);
            EventManager<HideAdBreak>.RemoveListener(HidePanel);
        }

        private void ShowPanel(ShowAdBreak evt)
        {
            m_Panel.SetActive(true);
        }

        private void HidePanel(HideAdBreak evt)
        {
            m_Panel.SetActive(false);
        }
    }
}
