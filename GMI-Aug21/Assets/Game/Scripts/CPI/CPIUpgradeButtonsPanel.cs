using Oxtail.Utils;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    /// <summary>
    /// Shows/hides the CPI upgrade-button panel in sync with CPIManager's wave/inter-wave state.
    /// Entering wave state animates each button away (scale up, then down to zero) before disabling
    /// the panel; leaving wave state re-enables instantly with no animation.
    /// </summary>
    public class CPIUpgradeButtonsPanel : MonoBehaviour
    {
        private CPIUpgradeButton[] m_Buttons;
        private int m_PendingDisableCount;

        private void Awake()
        {
            m_Buttons = GetComponentsInChildren<CPIUpgradeButton>(true);
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
                EnableInstant();
        }

        private void EnableInstant()
        {
            gameObject.SetActive(true);

            foreach (var button in m_Buttons)
                button.ResetVisualState();
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
