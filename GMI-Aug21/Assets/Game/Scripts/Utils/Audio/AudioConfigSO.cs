using UnityEngine;
using UnityEngine.Audio;

namespace Oxtail.Utils
{
    [CreateAssetMenu(fileName = "SO_AudioConfig", menuName = "Oxtail/Audio/Config")]
    public class AudioConfigSO : ScriptableObjectSingleton<AudioConfigSO>
    {
        [SerializeField] private AudioMixer m_AudioMixer;
        [SerializeField] private AudioMixerGroup m_MusicGroup;
        [SerializeField] private AudioMixerGroup m_SoundsGroup;

        public AudioMixer AudioMixer => m_AudioMixer;
        public AudioMixerGroup MusicGroup => m_MusicGroup;
        public AudioMixerGroup SoundsGroup => m_SoundsGroup;
    }
}
