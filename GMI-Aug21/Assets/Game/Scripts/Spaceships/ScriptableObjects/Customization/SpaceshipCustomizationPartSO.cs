using Oxtail.Utils;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    public abstract class SpaceshipCustomizationPartSO : ScriptableObject
    {
        [Header("Info")]
        [SerializeField] private string m_CustomizationID;
        [SerializeField] private string m_CustomizationName;

        [Header("Cost")]
        [SerializeField] private CurrencyType m_UnlockCurrency;
        [SerializeField] private BigNumber m_UnlockCost;

        [Header("Preview")]
        [SerializeField] private Sprite m_Preview;

        [Header("Unlocked")]
        [SerializeField] private bool m_Unlocked;

        public string CustomizationID => m_CustomizationID;
        public string CustomizationName => m_CustomizationName;
        public CurrencyType UnlockCurrency => m_UnlockCurrency;
        public BigNumber UnlockCost => m_UnlockCost;
        public Sprite Preview => m_Preview;
        public bool Unlocked => m_Unlocked;

    }
}
