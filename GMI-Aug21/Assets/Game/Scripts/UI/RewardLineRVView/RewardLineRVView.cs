using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Oxtail.Utils;

namespace Oxtail.SpaceshipIncremental
{
    public class RewardLineRVView : MonoBehaviour
    {
        [SerializeField] private Button m_RewardLineEffect;
        [SerializeField] private TMP_Text m_TimerText;

        private float m_Cooldown;
        private float m_ChanceTimer;
        private float m_LiveDuration;

        private Coroutine m_ShowCoroutine;

        private void Awake()
        {
            m_RewardLineEffect.onClick.AddListener(() => ShowRV());

            EventManager<RewardLineAddedEvent>.AddListener(PlayerAddedRewardLine);
        }

        private void Start()
        {
            StartCountdown();
        }

        private void OnDestroy()
        {
            EventManager<RewardLineAddedEvent>.RemoveListener(PlayerAddedRewardLine);
        }

        private void StartCountdown()
        {
            m_ShowCoroutine = StartCoroutine(ShowRewardLineRV());
        }

        private IEnumerator ShowRewardLineRV()
        {
            m_Cooldown = Debugger.RewardLineRVCountdown;
            m_ChanceTimer = Debugger.RewardLineRVLiveTime;
            m_LiveDuration = Debugger.RewardLineRVLiveDuration;

            if (m_Cooldown <= 0)
                yield break;

            if (!LevelManager.Instance.CanSpawnRewardLine)
                yield break;

            while (true)
            {
                yield return new WaitForSeconds(m_Cooldown);

                if (!LevelManager.Instance.CanSpawnRewardLine)
                    yield break;

                m_RewardLineEffect.transform.position = LevelManager.Instance.NextRewardLineToShow.RVEffectPos.position;
                m_RewardLineEffect.transform.localScale = Vector3.zero;
                m_RewardLineEffect.gameObject.SetActive(true);
                m_RewardLineEffect.transform.DOScale(Vector3.one, 0.5f)
                    .SetEase(Ease.OutExpo);

                float timer = m_ChanceTimer;
                m_TimerText.text = m_ChanceTimer.ToString();

                while (timer > 0f)
                {
                    yield return new WaitForSeconds(1);
                    timer--;
                    m_TimerText.text = timer.ToString();
                }

                m_RewardLineEffect.gameObject.SetActive(false);
            }
        }

        private void ShowRV()
        {
            m_RewardLineEffect.transform.DOScale(Vector3.zero, 0.5f)
                .SetEase(Ease.InExpo)
                .OnComplete(() => { m_RewardLineEffect.gameObject.SetActive(false); });
            StopCoroutine(m_ShowCoroutine);
            m_ShowCoroutine = null;
            StartCountdown();
        }

        private void PlayerAddedRewardLine(RewardLineAddedEvent evt)
        {
            if (m_ShowCoroutine == null)
                return;

            m_RewardLineEffect.transform.DOScale(Vector3.zero, 0.25f)
                .SetEase(Ease.InExpo)
                .OnComplete(() => { m_RewardLineEffect.gameObject.SetActive(false); });
            StopCoroutine(m_ShowCoroutine);
            m_ShowCoroutine = null;
            StartCountdown();
        }
    }
}