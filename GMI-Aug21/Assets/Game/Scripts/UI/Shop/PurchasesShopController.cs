using Oxtail.Utils;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Oxtail.SpaceshipIncremental
{
    public struct ShopMessageEvent
    {
        public string Tittle { get; private set; }
        public string Message { get; private set; }

        public ShopMessageEvent(string tittle, string message)
        {
            Tittle = tittle;
            Message = message;
        }
    }

    public class PurchasesShopController : MonoBehaviour
    {
        [SerializeField] private GameObject m_Panel;
        [SerializeField] private TMP_Text m_GemsText;
        [SerializeField] private Button m_ShopButton;
        [SerializeField] private Button m_CloseButton;

        [Header("No Ads")]
        [SerializeField] private ShopIAPItemSO m_NoAdsItem;
        [SerializeField] private ShopPurchaseButtonWithInfo m_NoAdsButton;
        [SerializeField] private GameObject m_NoAdsPanel;

        [Header("Message Panel")]
        [SerializeField] private GameObject m_ShopMessagePanel;
        [SerializeField] private TMP_Text m_ShopMessageTittleText;
        [SerializeField] private TMP_Text m_ShopMessageMessageText;
        [SerializeField] private Button m_ShopMessageCloseButton;

        [Header("Shop Items")]
        [SerializeField] private ShopPurchaseButtonWithInfo m_ItemPrefab;
        [SerializeField] private RectTransform m_ItemsParent;
        [SerializeField] private ShopIAPItemSO[] m_ShopItems;

        [Header("Reward Panel")]
        [SerializeField] private GameObject m_RewardPanel;
        [SerializeField] private Image m_RewardIcon;
        [SerializeField] private TMP_Text m_RewardText;
        [SerializeField] private AudioClip m_PurchaseAudio;

        [Header("Shop Size")]
        [SerializeField] private RectTransform m_ScrollContent;
        [SerializeField] private RectTransform m_ScrollView;
        [SerializeField] private RectTransform m_ShopPanel;
        [SerializeField] private RectTransform m_GemsView;
        [SerializeField] private VerticalLayoutGroup m_VerticalLayoutGroup;
        [SerializeField] private VerticalLayoutGroup m_PurchasesVerticalLayoutGroup;

        private void Awake()
        {
            m_ShopButton.onClick.AddListener(()=> ShowShop());
            m_CloseButton.onClick.AddListener(()=> CloseShop());
            m_ShopMessageCloseButton.onClick.AddListener(()=> CloseShopMessage());

            EventManager<ShopMessageEvent>.AddListener(ShowShopMessage);
        }

        private void Start()
        {
            if (!Debugger.FakeShop)
            {
                m_ShopButton.gameObject.SetActive(false);
                return;
            }

            PremiumStatusChanged(Debugger.IsPremium);
            
            m_NoAdsButton.SetIcon(m_NoAdsItem.ProductIcon);
            m_NoAdsButton.SetDescription(m_NoAdsItem.GetDescription());

            foreach (var shopItem in m_ShopItems)
            {
                var item = Instantiate(m_ItemPrefab, m_ItemsParent);
                item.SetIcon(shopItem.ProductIcon);
                item.SetIconMaterial(shopItem.IconMaterial);
                item.SetDescription(shopItem.GetDescription());
                item.SetPrice(GetLocalizedPrice(shopItem.ProductID));
                item.ActivateParticles();
                item.SetParticlesColor(shopItem.ParticlesColor);
                if (shopItem.IsBestValue)
                    item.ShowBestValueBadge();
                item.OnPressed += () => GivePurchaseReward(shopItem.ProductID);
            }

            ResizeShopPanel();

            SaveLoadManager.Instance.OnGemsUpdated += OnGemsUpdated;
        }

        private void OnDestroy()
        {
            SaveLoadManager.Instance.OnGemsUpdated -= OnGemsUpdated;
            EventManager<ShopMessageEvent>.RemoveListener(ShowShopMessage);
        }

        private void PremiumStatusChanged(bool premium)
        {
            m_NoAdsPanel.SetActive(!premium);
            ResizeShopPanel();
            SaveLoadManager.Instance.SaveNoAdsStatus(premium);
        }

        private void ShowShopMessage(ShopMessageEvent evt)
        {
            m_ShopMessageTittleText.text = evt.Tittle;
            m_ShopMessageMessageText.text = evt.Message;
            m_ShopMessagePanel.SetActive(true);
        }

        private void CloseShopMessage()
        {
            m_ShopMessagePanel.SetActive(false);
        }

        public void ToggleShopButton(bool isVisible)
        {
            m_ShopButton.gameObject.SetActive(isVisible);
        }

        private void ShowShop()
        {
            m_Panel.SetActive(true);
            ResizeShopPanel();
        }

        private void CloseShop()
        {
            m_Panel.SetActive(false);
        }

        private void OnGemsUpdated()
        {
            m_GemsText.text = SaveLoadManager.Instance.GetGems().ToString();
        }

        private string GetLocalizedPrice(string productId) => "$1";

        private void GivePurchaseReward(string productID)
        {
            AudioManager.Instance.PauseMusic();
            AudioManager.Instance.PlaySound(m_PurchaseAudio);

            if (productID == m_NoAdsItem.ProductID)
            {
                m_RewardIcon.sprite = m_NoAdsItem.ProductIcon;
                m_RewardText.text = "Enjoy your game without Ads!";
                SaveLoadManager.Instance.SaveNoAdsStatus(true);
            }
            else
            {
                foreach (var shopItem in m_ShopItems)
                {
                    if (shopItem.ProductID == productID)
                    {
                        m_RewardIcon.sprite = shopItem.ProductIcon;

                        switch (shopItem.ShopIAPType)
                        {
                            case ShopIAPType.Gems:
                                m_RewardText.text = $"You've received {shopItem.Quantity} Gems!";
                                SaveLoadManager.Instance.AddGems(shopItem.Quantity);
                                break;
                        }
                    }
                }
            }

            m_RewardPanel.SetActive(true);

            StartCoroutine(HideRewardPanel());
        }

        private IEnumerator HideRewardPanel()
        {
            yield return new WaitForSecondsRealtime(4f);

            AudioManager.Instance.UnpauseMusic();
            m_RewardPanel.SetActive(false);
        }

        private void ResizeShopPanel()
        {
            StartCoroutine(ResizeDelay());
        }

        private IEnumerator ResizeDelay()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_ShopPanel);

            yield return new WaitForSecondsRealtime(0.1f);

            Vector2 newSize = new();
            newSize.x = m_ShopPanel.sizeDelta.x;
            newSize.y = m_PurchasesVerticalLayoutGroup.spacing + (m_VerticalLayoutGroup.spacing * 2f) + m_VerticalLayoutGroup.padding.top + m_ScrollView.sizeDelta.y + m_ScrollContent.sizeDelta.y + m_GemsView.sizeDelta.y;
            m_ShopPanel.sizeDelta = newSize;
        }
    }
}
