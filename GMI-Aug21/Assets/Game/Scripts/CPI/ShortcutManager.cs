using System.Collections.Generic;
using Oxtail.Utils;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    /// <summary>
    /// Polls a configurable set of keyboard shortcuts and broadcasts a ShortcutTriggeredEvent via
    /// EventManager whenever one is pressed, so listeners (e.g. CPIManager) can react without a
    /// direct reference to this manager.
    ///
    /// Lives on its own dedicated GameObject, separate from the CPI level, and persists across scene
    /// reloads (a PersistentMonoSingleton) so the dev-shortcut layer - and RecordingShortcutController,
    /// which shares that GameObject - never resets when the level does. A duplicate spawned by the next
    /// scene load destroys itself in Awake, taking RecordingShortcutController down with it, leaving the
    /// original persistent pair as the only one listening.
    /// </summary>
    public class ShortcutManager : PersistentMonoSingleton<ShortcutManager>
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
