using DG.Tweening;
using Oxtail.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Oxtail.SpaceshipIncremental
{
    public class TapScreenController : MonoBehaviour
    {
        [SerializeField] private Button m_BackgroundButton;
        [SerializeField] private GameObject m_TextView;
        [SerializeField] private TMP_Text m_TapText;

        private void Awake()
        {
            m_BackgroundButton.onClick.AddListener(BackgroundPressed);
            m_TapText.transform.DOScale(Vector3.one * 1.05f, 1f).SetEase(Ease.OutExpo).SetLoops(-1, LoopType.Yoyo);
            Invoke(nameof(ShowText), 10f);
            EventManager<SpeedUpEventStarted>.AddListener(SpeedUpStarted);
            EventManager<SpeedUpEventFinished>.AddListener(SpeedUpFinished);
        }

        private void OnDestroy()
        {
            m_TapText.transform.DOKill();
            EventManager<SpeedUpEventStarted>.RemoveListener(SpeedUpStarted);
            EventManager<SpeedUpEventFinished>.RemoveListener(SpeedUpFinished);
        }

        private void ShowText()
        {
            m_TextView.SetActive(true);
        }

        private void SpeedUpStarted(SpeedUpEventStarted eventData)
        {
            m_BackgroundButton.enabled = false;
            DisableInvoke();
        }

        private void SpeedUpFinished(SpeedUpEventFinished eventData)
        {
            m_BackgroundButton.enabled = true;
            Invoke(nameof(ShowText), 30f);
        }

        private void DisableInvoke()
        {
            m_TextView.SetActive(false);
            CancelInvoke(nameof(ShowText));
        }

        private void BackgroundPressed()
        {
            DisableInvoke();
            LevelManager.Instance.SpeedUpSpaceships();
        }
    }
}
