using UnityEngine;

namespace Oxtail.Utils
{
    public class PlayMusicOnEnable : MonoBehaviour
    {
        [SerializeField] private AudioClip m_Audio;

        private void OnEnable()
        {
            AudioManager.Instance.PlayMusic(m_Audio);
        }
    }
}
