using Oxtail.Utils;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    /// <summary>
    /// Shows/hides the CPI upgrade-button panel in sync with CPIManager's wave/inter-wave state.
    /// Entering wave state animates each button away (scale up, then down to zero) before disabling
    /// the panel; leaving wave state re-enables the panel and animates each button in (scale up from
    /// zero, then settle back to its original scale).
    /// </summary>
    public class CPIUpgradeButtonsPanel : MonoBehaviour
    {
        private CPIUpgradeButton[] m_Buttons;
        private int m_PendingDisableCount;

        private void Awake()
        {
            // Active buttons only: a button left disabled in the scene (e.g. Merge) never runs its own Awake,
            // so animating it would hit its uninitialized scale target.
            m_Buttons = GetComponentsInChildren<CPIUpgradeButton>();
            EventManager<CPIManager.CPIWaveStateChangedEvent>.AddListener(OnWaveStateChanged);
        }

        private void OnDestroy()
        {
            EventManager<CPIManager.CPIWaveStateChangedEvent>.RemoveListener(OnWaveStateChanged);
        }

        private void OnWaveStateChanged(CPIManager.CPIWaveStateChangedEvent evt)
        {
            if (evt.IsWaveActive)
                PlayDisableTransition();
            else
                PlayEnableTransition();
        }

        private void PlayEnableTransition()
        {
            gameObject.SetActive(true);

            foreach (var button in m_Buttons)
                button.PlayEnableAnimation();
        }

        private void PlayDisableTransition()
        {
            m_PendingDisableCount = m_Buttons.Length;

            foreach (var button in m_Buttons)
                button.PlayDisableAnimation(OnButtonDisableComplete);
        }

        private void OnButtonDisableComplete()
        {
            m_PendingDisableCount--;

            if (m_PendingDisableCount <= 0)
                gameObject.SetActive(false);
        }
    }
}
