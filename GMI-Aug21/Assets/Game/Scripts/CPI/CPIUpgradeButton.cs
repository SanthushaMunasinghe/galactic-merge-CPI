using System;
using DG.Tweening;
using Oxtail.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Oxtail.SpaceshipIncremental
{
    public enum CPIUpgradeType { Merge, AddSpaceship, AddRewardLine, UpgradeCircuit }

    /// <summary>
    /// Single, unified upgrade-button script for the CPI test scene. One instance per button,
    /// with the action picked via <see cref="CPIUpgradeType"/> instead of a dedicated subclass.
    /// </summary>
    public class CPIUpgradeButton : MonoBehaviour
    {
        [SerializeField] private CPIUpgradeType m_Type;
        [SerializeField] private string m_Title;
        [SerializeField] private bool m_ForceFakeMax;

        [SerializeField] private Button m_Button;
        [SerializeField] private TMP_Text m_TitleText;
        [SerializeField] private TMP_Text m_PriceText;

        [Header("Interactable State")]
        [SerializeField] private GameObject m_NotInteractableOverlay;
        [SerializeField] private Color m_NotInteractableTextColor = Color.gray;

        [Header("Click Feedback")]
        [SerializeField] private Transform m_ScaleTarget;
        [SerializeField] private float m_ScaleUpMultiplier = 1.15f;
        [SerializeField] private float m_ScaleDuration = 0.15f;
        [SerializeField] private float m_DisableScaleDownDuration = 0.2f;

        private Color m_InteractableTitleColor;
        private Color m_InteractablePriceColor;
        private Transform m_ResolvedScaleTarget;
        private Vector3 m_InitialScale;

        private void Awake()
        {
            m_Button.onClick.AddListener(OnClick);
            m_TitleText.text = m_Title;

            m_InteractableTitleColor = m_TitleText.color;
            m_InteractablePriceColor = m_PriceText.color;

            m_ResolvedScaleTarget = m_ScaleTarget != null ? m_ScaleTarget : transform;
            m_InitialScale = m_ResolvedScaleTarget.localScale;

            LevelManager.Instance.Money.OnPropertyChanged += OnStateChanged;
            LevelManager.Instance.CanMerge.OnPropertyChanged += OnStateChanged;
            LevelManager.Instance.CanSpawnSpaceship.OnPropertyChanged += OnStateChanged;
            LevelManager.Instance.CanChangeCircuit.OnPropertyChanged += OnStateChanged;
        }

        private void OnDestroy()
        {
            if (LevelManager.Instance == null)
                return;

            LevelManager.Instance.Money.OnPropertyChanged -= OnStateChanged;
            LevelManager.Instance.CanMerge.OnPropertyChanged -= OnStateChanged;
            LevelManager.Instance.CanSpawnSpaceship.OnPropertyChanged -= OnStateChanged;
            LevelManager.Instance.CanChangeCircuit.OnPropertyChanged -= OnStateChanged;
        }

        private void Start()
        {
            RefreshState();
        }

        private void OnStateChanged<T>(T _)
        {
            RefreshState();
        }

        private void OnClick()
        {
            PlayClickFeedback();
            Debug.Log("Clicked");

            switch (m_Type)
            {
                case CPIUpgradeType.Merge:
                    LevelManager.Instance.MergeSpaceship();
                    LevelManager.Instance.IncreaseMergeSpaceshipLevel();
                    UIParticleActions.PlayMerge();
                    break;
                case CPIUpgradeType.AddSpaceship:
                    LevelManager.Instance.RemoveMoney(GameUpgradesCostSO.Instance.GetAddSpaceshipUpgradeCost());
                    LevelManager.Instance.AddDefaultSpaceship();
                    LevelManager.Instance.IncreaseAddSpaceshipLevel();
                    UIParticleActions.PlayShipAdded();
                    break;
                case CPIUpgradeType.AddRewardLine:
                    LevelManager.Instance.AddRewardLine();
                    LevelManager.Instance.IncreaseRewardLineLevel();
                    UIParticleActions.PlayLineAdded();
                    break;
                case CPIUpgradeType.UpgradeCircuit:
                    LevelManager.Instance.ChangeCircuit();
                    LevelManager.Instance.IncreaseCircuitLevel();
                    break;
            }

            RefreshState();
        }

        public void PlayDisableAnimation(Action onComplete)
        {
            m_NotInteractableOverlay.SetActive(true);

            DOTween.Sequence()
                .Append(m_ResolvedScaleTarget.DOScale(m_InitialScale * m_ScaleUpMultiplier, m_ScaleDuration))
                .Append(m_ResolvedScaleTarget.DOScale(Vector3.zero, m_DisableScaleDownDuration))
                .OnComplete(() => onComplete?.Invoke());
        }

        public void PlayEnableAnimation()
        {
            m_ResolvedScaleTarget.DOKill();
            m_ResolvedScaleTarget.localScale = Vector3.zero;

            DOTween.Sequence()
                .Append(m_ResolvedScaleTarget.DOScale(m_InitialScale * m_ScaleUpMultiplier, m_ScaleDuration))
                .Append(m_ResolvedScaleTarget.DOScale(m_InitialScale, m_DisableScaleDownDuration));

            RefreshState();
        }

        private void PlayClickFeedback()
        {
            m_ResolvedScaleTarget.DOKill();
            m_ResolvedScaleTarget.localScale = m_InitialScale;
            m_ResolvedScaleTarget.DOScale(m_InitialScale * m_ScaleUpMultiplier, m_ScaleDuration / 2f)
                .SetLoops(2, LoopType.Yoyo);
        }

        private void RefreshState()
        {
            BigNumber cost = m_ForceFakeMax ? -1 : GetCost();
            bool isMax = cost < 0;
            m_PriceText.text = isMax ? "MAX" : cost == 0 ? "FREE" : cost.ToString();

            bool canAfford = !isMax && LevelManager.Instance.Money.Value >= cost;
            bool interactable = canAfford && IsAvailable();

            m_Button.interactable = interactable;
            m_NotInteractableOverlay.SetActive(!interactable);

            m_TitleText.color = interactable ? m_InteractableTitleColor : m_NotInteractableTextColor;
            m_PriceText.color = interactable ? m_InteractablePriceColor : m_NotInteractableTextColor;
        }

        private BigNumber GetCost()
        {
            switch (m_Type)
            {
                case CPIUpgradeType.Merge:
                    return GameUpgradesCostSO.Instance.GetMergeUpgradeCost();
                case CPIUpgradeType.AddSpaceship:
                    return LevelManager.Instance.CanSpawnSpaceship.Value
                        ? GameUpgradesCostSO.Instance.GetAddSpaceshipUpgradeCost() : -1;
                case CPIUpgradeType.AddRewardLine:
                    return LevelManager.Instance.CanSpawnRewardLine
                        ? GameUpgradesCostSO.Instance.GetRewardLineUpgradeCost() : -1;
                case CPIUpgradeType.UpgradeCircuit:
                    return LevelManager.Instance.HasMoreCircuits
                        ? GameUpgradesCostSO.Instance.GetCircuitUpgradeCost() : -1;
                default:
                    return -1;
            }
        }

        private bool IsAvailable()
        {
            return m_Type != CPIUpgradeType.Merge || LevelManager.Instance.CanMerge.Value;
        }
    }
}
