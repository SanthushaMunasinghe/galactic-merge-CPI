using Oxtail.Utils;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Oxtail.SpaceshipIncremental
{
    public class CustomizationView : MonoBehaviour
    {
        [SerializeField] protected Image m_Frame;
        [SerializeField] protected Image m_PreviewImage;
        [SerializeField] protected TMP_Text m_CustomizationName;

        [Header("Preview Button")]
        [SerializeField] private Button m_PreviewButton;

        [Header("Unlock Button")]
        [SerializeField] protected GameObject m_UnlockParent;
        [SerializeField] protected Button m_UnlockButton;

        [Header("Select Button")]
        [SerializeField] private GameObject m_SelectButtonParent;
        [SerializeField] protected Button m_SelectButton;

        [Header("Selected View")]
        [SerializeField] private GameObject m_SelectedView;

        [Header("Unlock")]
        [SerializeField] private Image m_CurrencyIcon;
        [SerializeField] private TMP_Text m_CurrencyText;

        public string CustomizationID { get; private set; }

        public event Action<string> OnPreviewClicked;
        public event Action<CustomizationView> OnCustomizationSelected;

        public void SetData(SpaceshipCustomizationPartSO part)
        {
            CustomizationID = part.CustomizationID;

            SetPreview(part.Preview);
            SetName(part.CustomizationName);

            bool unlocked = SaveLoadManager.Instance.GetSpaceshipCustomizationUnlocked(part.CustomizationID);
            if (unlocked)
                SetUnlocked();
            else
            {
                if (part.Unlocked)
                {
                    SaveLoadManager.Instance.SaveSpaceshipCustomizationUnlocked(part.CustomizationID);
                    SetUnlocked();
                }
                else
                    SetLocked(part.UnlockCurrency, part.UnlockCost);
            }

            m_UnlockButton.onClick.RemoveAllListeners();
            m_UnlockButton.onClick.AddListener(()=>
            {
                switch (part.UnlockCurrency)
                {
                    case CurrencyType.Gems:
                        SaveLoadManager.Instance.AddGems(-part.UnlockCost);
                        break;
                    case CurrencyType.Celestium:
                        SaveLoadManager.Instance.AddCelestium(-part.UnlockCost);
                        break;
                }

                SaveLoadManager.Instance.SaveSpaceshipCustomizationUnlocked(part.CustomizationID);
                SetUnlocked();
            });

            m_PreviewButton.onClick.RemoveAllListeners();
            m_PreviewButton.onClick.AddListener(()=> OnPreviewClicked?.Invoke(part.CustomizationID));

            m_SelectButton.onClick.RemoveAllListeners();
            m_SelectButton.onClick.AddListener(()=>
            {
                OnPreviewClicked?.Invoke(part.CustomizationID);

                if (part is SpaceshipShapeSO)
                {
                    SaveLoadManager.Instance.SaveSpaceshipShapeID(part.CustomizationID);
                    EventManager<UpdateShapeEvent>.TriggerEvent(new UpdateShapeEvent(part.CustomizationID));
                }
                else if (part is SpaceshipEffectSO)
                {
                    SaveLoadManager.Instance.SaveSpaceshipEffectID(part.CustomizationID);
                    EventManager<UpdateEffectEvent>.TriggerEvent(new UpdateEffectEvent(part.CustomizationID));
                }
                else if (part is SpaceshipTrailSO)
                {
                    SaveLoadManager.Instance.SaveSpaceshipTrailID(part.CustomizationID);
                    EventManager<UpdateTrailEvent>.TriggerEvent(new UpdateTrailEvent(part.CustomizationID));
                }

                OnCustomizationSelected?.Invoke(this);
            });
        }

        public void SetPreviewSelected()
        {
            m_Frame.color = Color.green;
            m_PreviewButton.interactable = false;
        }

        public void SetPreviewUnselected()
        {
            m_Frame.color = Color.white;
            m_PreviewButton.interactable = true;
        }

        private void SetPreview(Sprite preview)
        {
            m_PreviewImage.sprite = preview;
        }

        private void SetName(string name) => m_CustomizationName.text = name;

        private void SetLocked(CurrencyType currency, BigNumber cost)
        {
            m_CurrencyIcon.sprite = GameIconsSO.Instance.GetCurrencyIcon(currency);
            m_CurrencyText.text = cost.ToString();

            BigNumber quantity = currency switch
            {
                CurrencyType.Gems => SaveLoadManager.Instance.GetGems(),
                CurrencyType.Celestium => SaveLoadManager.Instance.GetCelestium()
            };

            m_UnlockButton.interactable = quantity >= cost;

            m_UnlockParent.gameObject.SetActive(true);
            m_SelectButtonParent.gameObject.SetActive(false);
            m_SelectedView.gameObject.SetActive(false);
        }

        private void SetUnlocked()
        {
            m_UnlockParent.gameObject.SetActive(false);
            m_SelectedView.gameObject.SetActive(false);
            m_SelectButtonParent.gameObject.SetActive(true);
        }

        public void UnsetCurrentCustomization()
        {
            if (!SaveLoadManager.Instance.GetSpaceshipCustomizationUnlocked(CustomizationID))
                return;

            m_UnlockParent.gameObject.SetActive(false);
            m_SelectedView.gameObject.SetActive(false);
            m_SelectButtonParent.gameObject.SetActive(true);
        }

        public void SetCurrentCustomization()
        {
            m_UnlockParent.gameObject.SetActive(false);
            m_SelectedView.gameObject.SetActive(true);
            m_SelectButtonParent.gameObject.SetActive(false);
        }
    }
}
