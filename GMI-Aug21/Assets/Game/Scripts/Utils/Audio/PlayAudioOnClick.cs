using UnityEngine;
using UnityEngine.UI;

namespace Oxtail.Utils
{
    [RequireComponent(typeof(Button))]
    public class PlayAudioOnClick : MonoBehaviour
    {
        [SerializeField] private AudioClip m_Audio;

        private Button m_Button;

        private void Awake()
        {
            m_Button = GetComponent<Button>();
            m_Button.onClick.AddListener(()=> PlayAudio());
        }

        private void PlayAudio()
        {
            AudioManager.Instance.PlaySound(m_Audio);
        }
    }
}
