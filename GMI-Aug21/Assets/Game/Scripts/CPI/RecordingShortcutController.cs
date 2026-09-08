#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using Oxtail.Utils;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Oxtail.SpaceshipIncremental
{
    /// <summary>
    /// Editor-only dev shortcuts for the CPI test scene: Q toggles a recording, R reloads the scene.
    /// Recording is driven through the Recorder window itself instead of settings built in code, so a
    /// take always uses exactly what is set up there (recorders, output path, resolution, format,
    /// frame rate) - pressing Q is the same as clicking its START/STOP RECORDING button. Listens to
    /// ShortcutManager the same way CPIManager does, but doesn't touch any CPI game state.
    /// </summary>
    public class RecordingShortcutController : MonoBehaviour
    {
        [SerializeField, Min(1)] private int m_MaxDurationSeconds = 60;

        private Coroutine m_CountdownCO;

        private void OnEnable()
        {
            EventManager<ShortcutManager.ShortcutTriggeredEvent>.AddListener(OnShortcutTriggered);
        }

        private void OnDisable()
        {
            EventManager<ShortcutManager.ShortcutTriggeredEvent>.RemoveListener(OnShortcutTriggered);
        }

        private void OnShortcutTriggered(ShortcutManager.ShortcutTriggeredEvent shortcutEvent)
        {
            if (shortcutEvent.Key == KeyCode.Q)
                ToggleRecording();
            else if (shortcutEvent.Key == KeyCode.R)
                ReloadScene();
        }

        private void ToggleRecording()
        {
            RecorderWindow window = GetRecorderWindow();
            if (window == null)
                return;

            if (window.IsRecording())
                StopRecording(window);
            else
                StartRecording(window);
        }

        private void StartRecording(RecorderWindow window)
        {
            window.StartRecording();

            // StartRecording swallows its failures (no enabled recorder, invalid settings, a failed
            // script compile), so ask the window whether it actually went into recording state
            // instead of counting down over a take that never began.
            if (!window.IsRecording())
            {
                Debug.LogWarning($"{nameof(RecordingShortcutController)}: the Recorder window did not start " +
                    "recording. Check that it has at least one enabled recorder and no errors.", this);
                return;
            }

            Debug.Log($"{nameof(RecordingShortcutController)}: recording started, auto-stop in " +
                $"{m_MaxDurationSeconds}s. Press Q again to stop early.");

            m_CountdownCO = StartCoroutine(CountdownCO(window));
        }

        private void StopRecording(RecorderWindow window)
        {
            if (m_CountdownCO != null)
            {
                StopCoroutine(m_CountdownCO);
                m_CountdownCO = null;
            }

            window.StopRecording();

            Debug.Log($"{nameof(RecordingShortcutController)}: recording stopped, output is in the folder " +
                "configured on the Recorder window.");
        }

        /// <summary>
        /// Logs every 10 seconds of remaining time, then every second for the last 5 seconds, so the
        /// countdown is visible in the console without spamming it for the whole take, and stops the
        /// recording once the duration runs out.
        /// </summary>
        private IEnumerator CountdownCO(RecorderWindow window)
        {
            int remaining = m_MaxDurationSeconds;

            while (remaining > 0)
            {
                yield return new WaitForSeconds(1f);

                // The window can end the take on its own (its own STOP RECORDING button, or a record
                // mode other than Manual), in which case there is nothing left to count down.
                if (window == null || !window.IsRecording())
                {
                    m_CountdownCO = null;
                    yield break;
                }

                remaining--;

                if (remaining % 10 == 0 || (remaining > 0 && remaining <= 5))
                    Debug.Log($"{nameof(RecordingShortcutController)}: recording, {remaining}s remaining.");
            }

            // Cleared first: StopRecording would otherwise stop this coroutine mid-call, before it
            // gets to stop the recording.
            m_CountdownCO = null;
            StopRecording(window);
        }

        private void ReloadScene()
        {
            // Finalize any in-progress take before the scene unloads, otherwise the recording keeps
            // running across the reload with no countdown left to end it.
            RecorderWindow window = GetRecorderWindow();
            if (window != null && window.IsRecording())
                StopRecording(window);

            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>
        /// Returns the open Recorder window, or opens one (unfocused, so the Game view keeps the
        /// keyboard and Q/R keep working) loaded with the same global settings the window persists.
        /// </summary>
        private static RecorderWindow GetRecorderWindow()
        {
            RecorderWindow window = Resources.FindObjectsOfTypeAll<RecorderWindow>().FirstOrDefault();

            if (window == null)
                window = EditorWindow.GetWindow<RecorderWindow>(false, "Recorder", false);

            return window;
        }
    }
}
#endif
