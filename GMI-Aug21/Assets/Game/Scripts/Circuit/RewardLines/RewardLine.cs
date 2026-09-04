using DG.Tweening;
using Shapes;
using System.Collections;
using TMPro;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    public class RewardLine : MonoBehaviour
    {
        [SerializeField] private GameObject m_Timer;
        [SerializeField] private TMP_Text m_TimerText;

        private GameObject m_RvEffect;
        private Line m_Line;

        private Tween m_PulseTween;

        public Transform RVEffectPos => m_RvEffect.transform;
        public bool IsShown => gameObject.activeInHierarchy;


        private void OnDisable()
        {
            transform.DOKill();
        }

        private void OnDestroy()
        {
            transform.DOKill();
        }

        public void Init()
        {
            m_Line = GetComponent<Line>();
            var canvas = transform.parent.GetComponentInChildren<Canvas>(true);
            canvas.worldCamera = Camera.main;
            m_RvEffect = canvas.gameObject;
        }

        public void Show(bool show, bool isBoost = false)
        {
            gameObject.SetActive(show);

            if (show && isBoost)
            {
                m_PulseTween = DOTween.To(() => m_Line.Color, x => m_Line.Color = x, Color.yellow, 0.5f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);

                StartCoroutine(ShowTimer());
            }
            else if (!show)
            {
                if (m_PulseTween != null)
                    m_PulseTween.Kill();
            }
        }

        private IEnumerator ShowTimer()
        {
            if (m_Timer == null || m_TimerText == null)
                yield break;

            m_Timer.transform.rotation = Quaternion.identity;
            m_Timer.SetActive(true);
            float timer = Debugger.RewardLineRVLiveDuration;

            while (timer > 0)
            {
                m_TimerText.text = timer.ToString();
                yield return new WaitForSeconds(1f);
                timer--;
            }

            m_Timer.SetActive(false);
        }

        public virtual void DoAction()
        {
            m_Line.Dashed = false;
            transform.DOKill();
            transform.localScale = Vector3.one;
            transform.DOPunchScale(Vector3.Scale(transform.localScale, new Vector3(0.2f, 0.2f, 1f)), 0.2f, 1)
                .OnComplete(() =>
                {
                    m_Line.Dashed = true;
                });
        }
    }
}
