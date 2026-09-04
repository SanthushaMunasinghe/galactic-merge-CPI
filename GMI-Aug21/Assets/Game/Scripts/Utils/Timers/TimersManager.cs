using System;
using System.Collections;
using UnityEngine;

namespace Oxtail.Utils
{
    public class TimersManager : MonoSingleton<TimersManager>
    {
        public Timer CreateForwardTimer(float time)
        {
            return new ForwardTimer(time);
        }

        public Timer CreateCountdownTimer(float time)
        {
            return new CountdownTimer(time);
        }

        public void StartTimer(Timer timer)
        {
            timer.StartTimer();
            StartCoroutine(UpdateTimer(timer));
        }

        public void StopTimer(Timer timer)
        {
            timer.StopTimer();
        }

        private IEnumerator UpdateTimer(Timer timer)
        {
            while (timer.IsRunning)
            {
                timer.UpdateTimer();
                yield return null;
            }
        }
    }

    public abstract class Timer
    {
        protected float m_TimerTime;
        protected float m_CurrentTime;

        public bool IsRunning { get; private set; }

        public Action<float> OnTimeUpdated;
        public Action OnTimerFinished;

        public abstract void UpdateTimer();
        public abstract void ResetTimer();

        public void StartTimer() => IsRunning = true;

        public void StopTimer() => IsRunning = false;

        protected void FinishTimer()
        {
            OnTimerFinished?.Invoke();
            OnTimeUpdated = null;
            OnTimerFinished = null;
        }
    }

    public class ForwardTimer : Timer
    {
        public ForwardTimer(float time)
        {
            m_CurrentTime = 0f;
            m_TimerTime = time;
        }

        public override void UpdateTimer()
        {
            m_CurrentTime += Time.deltaTime;
            OnTimeUpdated?.Invoke(m_CurrentTime);

            if (m_CurrentTime >= m_TimerTime)
                FinishTimer();
        }

        public override void ResetTimer()
        {
            m_CurrentTime = 0f;
        }
    }

    public class CountdownTimer : Timer
    {
        public CountdownTimer(float time)
        {
            m_TimerTime = time;
            m_CurrentTime = time;
        }

        public override void UpdateTimer()
        {
            m_CurrentTime -= Time.deltaTime;
            OnTimeUpdated?.Invoke(m_CurrentTime);

            if (m_CurrentTime <= 0f)
                FinishTimer();
        }

        public override void ResetTimer()
        {
            m_CurrentTime = m_TimerTime;
        }
    }
}

