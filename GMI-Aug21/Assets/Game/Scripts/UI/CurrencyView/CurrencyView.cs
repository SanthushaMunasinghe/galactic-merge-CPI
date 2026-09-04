using Oxtail.Utils;
using System;
using TMPro;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    public class CurrencyView : MonoBehaviour
    {
        [SerializeField] private CurrencyType m_CurrencyType;
        [SerializeField] private TMP_Text m_CurrencyAmountText;

        private void Awake()
        {
            switch (m_CurrencyType)
            {
                case CurrencyType.Gems:
                    SaveLoadManager.Instance.OnGemsUpdated += UpdateCurrencyAmount;
                    break;
                case CurrencyType.Celestium:
                    SaveLoadManager.Instance.OnCelestiumUpdated += UpdateCurrencyAmount;
                    break;
            }
        }

        private void OnDestroy()
        {
            switch (m_CurrencyType)
            {
                case CurrencyType.Gems:
                    SaveLoadManager.Instance.OnGemsUpdated -= UpdateCurrencyAmount;
                    break;
                case CurrencyType.Celestium:
                    SaveLoadManager.Instance.OnCelestiumUpdated -= UpdateCurrencyAmount;
                    break;
            }
        }

        private void OnEnable()
        {
            UpdateCurrencyAmount();
        }

        private void UpdateCurrencyAmount()
        {
            m_CurrencyAmountText.text = m_CurrencyType switch
            {
                CurrencyType.Coins => SaveLoadManager.Instance.GetMoney().ToString(),
                CurrencyType.Gems => SaveLoadManager.Instance.GetGems().ToString(),
                CurrencyType.Celestium => SaveLoadManager.Instance.GetCelestium().ToString()
            };
        }
    }
}