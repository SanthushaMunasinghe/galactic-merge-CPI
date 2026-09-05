using System.Collections.Generic;
using Oxtail.Utils;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    /// <summary>
    /// Polls a configurable set of keyboard shortcuts and broadcasts a ShortcutTriggeredEvent via
    /// EventManager whenever one is pressed, so listeners (e.g. CPIManager) can react without a
    /// direct reference to this manager.
    /// </summary>
    public class ShortcutManager : MonoSingleton<ShortcutManager>
    {
        public struct ShortcutTriggeredEvent
        {
            public KeyCode Key;
        }

        [SerializeField] private List<KeyCode> m_WatchedShortcuts = new List<KeyCode> { KeyCode.S };

        private void Update()
        {
            foreach (KeyCode key in m_WatchedShortcuts)
            {
                if (Input.GetKeyDown(key))
                    EventManager<ShortcutTriggeredEvent>.TriggerEvent(new ShortcutTriggeredEvent { Key = key });
            }
        }
    }
}
