using DG.Tweening;
using Oxtail.Utils;
using Shapes;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Oxtail.SpaceshipIncremental
{
    public abstract class LevelCompleteView : MonoBehaviour
    {
        [Header("Panel")] [SerializeField] private GameObject m_Panel;

        [Header("Visuals")] [SerializeField] private TMP_Text m_LevelCompleteText;
        [SerializeField] private TMP_Text m_LevelReadyText;
        [SerializeField] private TMP_Text m_GemsAmountText;
        [SerializeField] private GameObject m_GemsObject;
        [SerializeField] private ShapeRenderer m_SpaceShip;
        [SerializeField] private ParticleSystem m_SpaceShipTrail;
        [SerializeField] private CanvasGroup m_FadeCanvasGroup;

        [Header("Gems Effect")] [SerializeField]
        private Image m_PowerUpsImage;
        [SerializeField] private UIBurstEffect m_GemsBurstEffect;

        [Header("Ad break Pop up")] [SerializeField]
        private GameObject m_AdBreak;

        [Header("Video Ad")] [SerializeField] private GameObject m_VideoAdPanel;
        [SerializeField] private Slider m_MulitplierSlider;
        [SerializeField] private Button m_VideoAdWatchButton;
        [SerializeField] private Button m_VideoAdCancelButton;

        [Header("Audio")] [SerializeField] private AudioClip m_SpaceshipAppearAudio;
        [SerializeField] private AudioClip m_SpaceshipMovesForwardAudio;
        [SerializeField] private AudioClip m_GemsAudio;

        private float m_PreviousVolume;

        protected string m_FinishedDescription;
        protected string m_NextLevelDescription;
        protected int m_GemsOnCompletion;

        private float m_VideoAdCountdown = 20f;
        private bool m_VideoAdCompleted;

        private bool m_AdBreakFinished;

        private float m_SliderFinalValue;
        private bool m_BurstEffectFinished;
        private BigNumber m_RemainingGems;

        protected abstract void FillData(int index);

        protected virtual void Awake()
        {
            m_VideoAdCancelButton.onClick.AddListener(() => CancelVideoAd());
            m_VideoAdWatchButton.onClick.AddListener(() => LaunchVideoAdNow());

            m_GemsBurstEffect.OnItemLanded += GemsBurstEffectOnItemLanded;
            m_GemsBurstEffect.OnBurstCompleted += GemsBurstEffectOnBurstCompleted;
        }

        private void OnDestroy()
        {
            m_GemsBurstEffect.OnItemLanded -= GemsBurstEffectOnItemLanded;
            m_GemsBurstEffect.OnBurstCompleted -= GemsBurstEffectOnBurstCompleted;
        }

        public void ShowLevelComplete(ShowLevelCompleteEvent eventData)
        {
            m_Panel.gameObject.SetActive(true);

            FillData(eventData.LevelIndex);

            m_PowerUpsImage.gameObject.SetActive(false);

            m_PreviousVolume = AudioManager.Instance.GetMusicVolume();
            AudioManager.Instance.SetMusicVolume(Mathf.Min(m_PreviousVolume, 20f));

            m_GemsObject.gameObject.SetActive(false);
            m_LevelReadyText.DOFade(0f, 0f);

            m_LevelCompleteText.text = m_FinishedDescription;
            m_LevelCompleteText.DOFade(0f, 0f);
            m_LevelCompleteText.DOFade(1f, 0.5f);

            Color color = SpaceshipTiers.Instance.GetTier(eventData.MaxTierSpaceship).SpaceShipColor;
            m_SpaceShip.Color = color;
            SetTrailColor(color);

            m_SpaceShip.transform.DOLocalMoveY(0f, 3f)
                .SetEase(Ease.InExpo)
                .OnComplete(() => { StartCoroutine(ShowGems()); });

            StartCoroutine(PlaySpaceshipArriveSoundDelay());
        }

        private IEnumerator PlaySpaceshipArriveSoundDelay()
        {
            yield return new WaitForSeconds(2f);

            AudioManager.Instance.PlaySound(m_SpaceshipAppearAudio);
        }

        private void SetTrailColor(Color color)
        {
            var col = m_SpaceShipTrail.colorOverLifetime;

            Gradient gradient = new Gradient();

            Color lighter = Color.Lerp(color, Color.white, 0.5f);

            gradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(lighter, 0f),
                    new GradientColorKey(lighter, 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(0.5f, 0f),
                    new GradientAlphaKey(0f, 1f)
                }
            );

            col.color = gradient;
        }

        protected virtual IEnumerator ShowGems()
        {
            yield return new WaitForSeconds(2f);
            yield return DoGemsTransition();
        }

        private IEnumerator DoGemsTransition(float multiplier = 1f)
        {
            m_BurstEffectFinished = false;

            float gemValue = m_GemsOnCompletion * multiplier;
            m_RemainingGems = gemValue;
            m_GemsAmountText.text = NumberFormatter.FormatValue(gemValue);
            m_GemsObject.gameObject.SetActive(true);
            m_GemsAmountText.transform.DOPunchScale(m_GemsAmountText.transform.localScale * 0.2f, 0.2f);
            AudioManager.Instance.PlaySound(m_GemsAudio);

            yield return new WaitForSeconds(2f);

            m_PowerUpsImage.gameObject.SetActive(true);
            m_GemsBurstEffect.Play(m_GemsOnCompletion * multiplier);

            yield return new WaitUntil(() => m_BurstEffectFinished);

            yield return new WaitForSeconds(1f);
        }

        private void GemsBurstEffectOnItemLanded(BigNumber value)
        {
            m_PowerUpsImage.transform.DOKill();
            m_PowerUpsImage.transform.localScale = Vector3.one;
            m_PowerUpsImage.transform.DOPunchScale(Vector3.one * 0.1f, 0.1f);
            AudioManager.Instance.PlaySound(m_GemsAudio);

            m_RemainingGems -= value;
            m_GemsAmountText.text = m_RemainingGems.ToString();
        }

        private void GemsBurstEffectOnBurstCompleted()
        {
            m_BurstEffectFinished = true;
        }

        protected IEnumerator ShowLevelReady()
        {
            m_LevelCompleteText.gameObject.SetActive(false);
            m_GemsObject.gameObject.SetActive(false);
            m_PowerUpsImage.gameObject.SetActive(false);

            m_LevelReadyText.text = m_NextLevelDescription;

            yield return new WaitForSeconds(2f);

            m_LevelReadyText.DOFade(1f, 0.5f);

            yield return new WaitForSeconds(2f);

            m_LevelReadyText.DOFade(0f, 0.5f);

            yield return new WaitForSeconds(2f);

            MoveSpaceship();
        }

        private IEnumerator ShowVideoAd()
        {
            float multiplier = m_SliderFinalValue switch
            {
                < 0.21f => .5f,
                < 0.385f => 1f,
                < 0.493f => 2f,
                < 0.507f => 3f,
                < 0.613f => 2f,
                < 0.789f => 1f,
                < 1f => .5f
            };
            m_MulitplierSlider.DOKill(true);
            m_MulitplierSlider.value = m_SliderFinalValue;

            m_VideoAdWatchButton.interactable = false;
            m_VideoAdCancelButton.interactable = false;

            yield return new WaitForSeconds(1f);

            m_VideoAdPanel.SetActive(false);

            if (m_VideoAdCompleted)
            {
                SaveLoadManager.Instance.AddGems(m_GemsOnCompletion * multiplier);
                yield return new WaitForSeconds(1f);
                yield return DoGemsTransition(multiplier);
            }

            m_VideoAdPanel.SetActive(false);
        }

        private void CancelVideoAd()
        {
        }

        private void LaunchVideoAdNow()
        {
            m_SliderFinalValue = m_MulitplierSlider.value;
            StartCoroutine(ShowVideoAd());
        }
        
        private void MoveSpaceship()
        {
            var audioListener = AudioManager.Instance.PlaySound(m_SpaceshipMovesForwardAudio);
            m_SpaceShip.transform.DOLocalMoveY(5f, 3f)
                .SetEase(Ease.OutExpo)
                .OnComplete(() =>
                {
                    audioListener.Stop();
                    StartCoroutine(DoLevelTransition());
                });
        }

        private IEnumerator DoLevelTransition()
        {
            yield return m_FadeCanvasGroup.DOFade(1f, 0.5f).SetEase(Ease.InOutSine).WaitForCompletion();

            AudioManager.Instance.SetMusicVolume(m_PreviousVolume);

            LevelGenerator.Instance.CreateNextLevel();
        }
    }
}