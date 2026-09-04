using System;
using System.Collections.Generic;
using UnityEngine.Events;

namespace Oxtail.Utils
{
    public static class EventManager<EventData> where EventData : struct
    {
        private static Dictionary<string, UnityEvent<EventData>> m_Events = new Dictionary<string, UnityEvent<EventData>>();

        public static void AddListener(UnityAction<EventData> listener)
        {
            string eventName = nameof(EventData);
            UnityEvent<EventData> evt = null;

            if (!m_Events.TryGetValue(eventName, out evt))
            {
                evt = new UnityEvent<EventData>();
                m_Events[eventName] = evt;
            }

            evt.AddListener(listener);
        }

        public static void RemoveListener(UnityAction<EventData> listener)
        {
            if (!GetEvent(out UnityEvent<EventData> evt))
                return;

            evt.RemoveListener(listener);
        }

        public static void RemoveAllListeners()
        {
            if (!GetEvent(out UnityEvent<EventData> evt))
                return;

            evt.RemoveAllListeners();
        }

        public static void TriggerEvent(EventData data)
        {
            if (!GetEvent(out UnityEvent<EventData> evt))
                return;

            evt?.Invoke(data);
        }

        public static void TriggerEvent()
        {
            if (!GetEvent(out UnityEvent<EventData> evt))
                return;

            evt?.Invoke(default);
        }

        private static bool GetEvent(out UnityEvent<EventData> evt)
        {
            return m_Events.TryGetValue(nameof(EventData), out evt);
        }
    }
}
