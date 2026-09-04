using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace Oxtail.SpaceshipIncremental
{
    public class PowerUpsView : MonoBehaviour
    {
        [SerializeField] private GameObject m_Panel;
        [SerializeField] private Button m_PowerUpButton;
        [SerializeField] private GameObject m_NotifyIcon;
        [SerializeField] private Button m_CloseButton;

        [Header("PowerUp")]
        [SerializeField] private PowerUpView m_PowerUp;
        [SerializeField] private RectTransform m_PowerUpParent;

        private List<PowerUpView> m_PowerUpViews = new();

        private void Awake()
        {
            m_PowerUpButton.onClick.AddListener(OnPowerUpButtonClicked);
            m_CloseButton.onClick.AddListener(OnCloseButtonClicked);

            m_NotifyIcon.SetActive(false);
            CreatePowerUps();
        }

        private void Start()
        {
            SaveLoadManager.Instance.OnGemsUpdated += OnGemsUpdated;
            SaveLoadManager.Instance.OnPowerUpUpdated += OnPowerUpUpdated;
        }

        private void OnDestroy()
        {
            SaveLoadManager.Instance.OnGemsUpdated -= OnGemsUpdated;
            SaveLoadManager.Instance.OnPowerUpUpdated -= OnPowerUpUpdated;
        }

        private void OnPowerUpUpdated(string id)
        {
            foreach (var powerUp in m_PowerUpViews)
            {
                powerUp.UpdateData();
            }

            CheckNotifyIcon();
        }

        private void OnGemsUpdated()
        {
            CheckNotifyIcon();
        }

        private void CheckNotifyIcon()
        {
            m_NotifyIcon.SetActive(m_PowerUpViews.Any(x => x.CanUpgrade));
        }

        private void CreatePowerUps()
        {
            var powerUps = GamePowerUpsSO.Instance.PowerUps;

            foreach (var powerUp in powerUps)
            {
                PowerUpView newPowerUp = Instantiate(m_PowerUp, m_PowerUpParent);
                newPowerUp.SetData(powerUp);
                m_PowerUpViews.Add(newPowerUp);
            }

            CheckNotifyIcon();
        }

        private void OnPowerUpButtonClicked()
        {
            m_Panel.gameObject.SetActive(true);
        }

        private void OnCloseButtonClicked()
        {
            m_Panel.gameObject.SetActive(false);
            m_NotifyIcon.SetActive(false);
        }
    }
}
