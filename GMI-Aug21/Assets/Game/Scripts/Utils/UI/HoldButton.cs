using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

namespace Oxtail.Utils
{
    public class HoldButton : Button
    {
        [Header("Hold Settings")]
        [SerializeField] private float m_FirstDelay = 0.4f;
        [SerializeField] private float m_RepeatRate = 0.15f;
        [SerializeField] private bool m_UseUnscaledTime = true;

        private bool m_IsHolding;
        private float m_NextInvokeTime;
        private bool m_RepeatingStarted;

        protected IEnumerator OnHold()
        {
            while (true)
            {
                if (!m_IsHolding || !IsActive())
                    yield break;

                float time = m_UseUnscaledTime ? Time.unscaledTime : Time.time;

                if (IsInteractable() && time >= m_NextInvokeTime)
                {
                    onClick.Invoke();

                    if (!m_RepeatingStarted)
                    {
                        m_RepeatingStarted = true;
                        m_NextInvokeTime = time + m_RepeatRate;
                    }
                    else
                    {
                        m_NextInvokeTime = time + m_RepeatRate;
                    }
                }

                yield return null;
            }
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);

            if (!IsActive() || !IsInteractable())
                return;

            m_IsHolding = true;
            m_RepeatingStarted = false;

            float time = m_UseUnscaledTime ? Time.unscaledTime : Time.time;
            m_NextInvokeTime = time + m_FirstDelay;

            StartCoroutine(OnHold());
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);
            StopHold();
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            StopHold();
        }

        private void StopHold()
        {
            m_IsHolding = false;
        }
    }
}
