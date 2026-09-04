using Oxtail.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    public enum ShopIAPType
    {
        NoAds,
        Gems
    }

    [CreateAssetMenu(fileName = "Shop IAP", menuName = "Incremental/IAP/Shop Item")]
    public class ShopIAPItemSO : ScriptableObject
    {
        [SerializeField] private string m_ProductID;
        [SerializeField, PreviewField] private Sprite m_ProductIcon; 
        [SerializeField] private ShopIAPType m_ShopIAPType;
        [SerializeField, ShowIf(nameof(m_IsGems))]
        private int m_Quantity;

        [Header("Visuals")]
        [SerializeField] private Material m_IconMaterial;
        [SerializeField] private Color m_ParticlesColor;

        [Header("Best Value Badge")]
        [SerializeField] private bool m_IsBestValue;

        private bool m_IsGems => m_ShopIAPType == ShopIAPType.Gems;

        public string ProductID => m_ProductID;
        public Sprite ProductIcon => m_ProductIcon;
        public ShopIAPType ShopIAPType => m_ShopIAPType;
        public int Quantity => m_Quantity;
        public Material IconMaterial => m_IconMaterial;
        public Color ParticlesColor => m_ParticlesColor;
        public bool IsBestValue => m_IsBestValue;

        public string GetDescription()
        {
            return m_ShopIAPType switch
            {
                ShopIAPType.NoAds => "Remove<br>Ads",
                ShopIAPType.Gems => $"{m_Quantity}<br>Gems"
            };
        }
    }
}
