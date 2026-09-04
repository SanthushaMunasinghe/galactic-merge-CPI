using System;
using UnityEngine;
using UnityEngine.UI;

namespace Oxtail.SpaceshipIncremental
{
    public class MapSelectorController : MonoBehaviour
    {
        [SerializeField] private GameObject m_Panel;
        [SerializeField] private Button m_MapSelectionButton;
        [SerializeField] private Button m_CloseButton;

        [Header("Maps")]
        [SerializeField] private MapSelectionView m_MapView;
        [SerializeField] private RectTransform m_MapViewsParent;

        private void Awake()
        {
            m_MapSelectionButton.onClick.AddListener(OnMapSelectionButtonButtonClicked);
            m_CloseButton.onClick.AddListener(OnCloseButtonClicked);

            CreateMaps();
        }

        private void CreateMaps()
        {
            for (int i = 0; i < GameLevelsSO.Instance.LevelsCount; i++)
            {
                var mapView = Instantiate(m_MapView, m_MapViewsParent);
                mapView.SetIndex(i);
            }
        }

        private void OnMapSelectionButtonButtonClicked()
        {
            m_Panel.gameObject.SetActive(true);
        }

        private void OnCloseButtonClicked()
        {
            m_Panel.gameObject.SetActive(false);
        }
    }
}
