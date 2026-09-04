using UnityEngine;

namespace Oxtail.Utils
{
    public class PlayAudioOnEnable : MonoBehaviour
    {
        [SerializeField] private AudioClip m_Audio;

        private void OnEnable()
        {
            AudioManager.Instance.PlaySound(m_Audio);
        }
    }
}
