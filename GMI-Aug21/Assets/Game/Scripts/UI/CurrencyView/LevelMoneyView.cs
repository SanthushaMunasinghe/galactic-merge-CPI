using DG.Tweening;
using Oxtail.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    public class LevelMoneyView : MonoBehaviour
    {
        [SerializeField] private TMP_Text m_MoneyText;
        [SerializeField] private Transform m_MoneyView;
        [SerializeField] private Animator m_MoneyFireAnimation;
        [SerializeField] private GameObject m_MoneyMultiplierView;

        [Header("Coins Per Second")]
        [SerializeField] private TMP_Text m_CoinsPerSecondText;
        [SerializeField] private GameObject m_CoinsPerSecondBoostEffect;

        private Tween m_MoneyPunchTween;

        private int m_FireAnimationStart = Animator.StringToHash("StartFire");
        private int m_FireAnimationEnd = Animator.StringToHash("FireFinish");

        private void Awake()
        {
            LevelManager.Instance.Money.OnPropertyChanged += UpdateMoneyText;
            EventManager<DoubleCoinEventStarted>.AddListener(DoDoubleCoinEffectStarted);
            EventManager<DoubleCoinEventFinished>.AddListener(DoDoubleCoinEffectFinished);
            EventManager<UpdateCoinsPerSecond>.AddListener(UpdateCoinsPerSecondText);
            EventManager<SpeedUpEventStarted>.AddListener(SpeedUpLevelStarted);
            EventManager<SpeedUpEventFinished>.AddListener(SpeedUpLevelFinished);
        }

        private void OnDestroy()
        {
            EventManager<DoubleCoinEventStarted>.RemoveListener(DoDoubleCoinEffectStarted);
            EventManager<DoubleCoinEventFinished>.RemoveListener(DoDoubleCoinEffectFinished);
            EventManager<UpdateCoinsPerSecond>.RemoveListener(UpdateCoinsPerSecondText);
            EventManager<SpeedUpEventStarted>.RemoveListener(SpeedUpLevelStarted);
            EventManager<SpeedUpEventFinished>.RemoveListener(SpeedUpLevelFinished);

            m_MoneyPunchTween.Kill();
            m_MoneyText.transform.DOKill();
        }

        private void OnEnable()
        {
            m_MoneyText.text = LevelManager.Instance.Money.Value.ToString();
        }

        private void DoDoubleCoinEffectStarted(DoubleCoinEventStarted eventData)
        {
            m_MoneyFireAnimation.SetTrigger(m_FireAnimationStart);
            m_MoneyPunchTween = m_MoneyView.DOPunchScale(Vector3.one * 0.1f, 0.25f, 1).SetEase(Ease.Linear).SetLoops(-1);
            m_MoneyMultiplierView.SetActive(true);
        }

        private void DoDoubleCoinEffectFinished(DoubleCoinEventFinished eventData)
        {
            m_MoneyFireAnimation.SetTrigger(m_FireAnimationEnd);
            m_MoneyPunchTween.Kill();
            m_MoneyMultiplierView.SetActive(false);
        }

        private void UpdateMoneyText(BigNumber value)
        {
            m_MoneyText.text = value.ToString();
            DoMoneyEffect();
        }

        private void DoMoneyEffect()
        {
            m_MoneyText.transform.DOKill();
            m_MoneyText.transform.localScale = Vector3.one;
            m_MoneyText.transform.DOPunchScale(Vector3.one * 0.1f, 0.1f);
        }

        private void UpdateCoinsPerSecondText(UpdateCoinsPerSecond coins)
        {
            string number = coins.CoinsPerSecond < 1000 ? coins.CoinsPerSecond.ToFloat().ToString("0.##")
                : coins.CoinsPerSecond.ToString();
            m_CoinsPerSecondText.text = number + "/s";
        }

        private void SpeedUpLevelStarted(SpeedUpEventStarted evt)
        {
            m_CoinsPerSecondBoostEffect.SetActive(true);
        }

        private void SpeedUpLevelFinished(SpeedUpEventFinished evt)
        {
            m_CoinsPerSecondBoostEffect.SetActive(false);
        }
    }
}
