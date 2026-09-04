using Oxtail.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Oxtail.SpaceshipIncremental
{
    public class SettingsView : MonoBehaviour
    {
        [SerializeField] protected GameObject m_Panel;
        [SerializeField] private Button m_SettingsButton;

        [Header("Sliders")]
        [SerializeField] private Slider m_MusicVolumeSlider;
        [SerializeField] private Slider m_SoundsVolumeSlider;

        [Header("Restore Purchases")]
        [SerializeField] private Button m_RestorePurchasesButton;
        [SerializeField] private TMP_Text m_RestoreStatus;

        [Header("Privacy")]
        [SerializeField] private Button m_PrivacyButton;

        [Header("Buttons")]
        [SerializeField] private Button m_CloseButton;

        protected virtual void Awake()
        {
            m_SoundsVolumeSlider.onValueChanged.AddListener((volume)=> SoundsVolumeChanged(volume));
            m_MusicVolumeSlider.onValueChanged.AddListener((volume)=> MusicVolumeChanged(volume));
            m_CloseButton.onClick.AddListener(()=> ClosePanel());
            m_SettingsButton.onClick.AddListener(()=> ShowPanel());
        }

        private void Start()
        {
            SetVolumes();
        }

        private void SetVolumes()
        {
            float musicVolume = SaveLoadManager.Instance.GetMusicVolume();
            m_MusicVolumeSlider.value = musicVolume;
            MusicVolumeChanged(musicVolume);

            float soundsVolume = SaveLoadManager.Instance.GetSoundsVolume();
            m_SoundsVolumeSlider.value = soundsVolume;
            SoundsVolumeChanged(soundsVolume);

        }

        private void ShowPanel()
        {
            m_Panel.SetActive(true);

            if (SaveLoadManager.Instance.GetRestoredPurchases())
            {
                m_RestorePurchasesButton.enabled = false;
                m_RestoreStatus.text = "Purchases<br>Restored!";
            }
            else
                m_RestoreStatus.text = "Restoring...";
        }

        private void ClosePanel()
        {
            m_Panel.SetActive(false);
        }

        private void MusicVolumeChanged(float volume)
        {
            AudioManager.Instance.SetMusicVolume(volume);
            SaveLoadManager.Instance.SaveMusicVolume(volume);
        }

        private void SoundsVolumeChanged(float volume)
        {
            AudioManager.Instance.SetSoundsVolume(volume);
            SaveLoadManager.Instance.SaveSoundsVolume(volume);
        }

    }
}
