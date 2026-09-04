using DG.Tweening;
using Oxtail.Utils;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Oxtail.SpaceshipIncremental
{
    public class PowerUpUnlockView : MonoBehaviour
    {
        [SerializeField] private GameObject m_Panel;

        [Space]
        [SerializeField] private GameObject m_IconObject;
        [SerializeField] private Image m_Icon;
        [SerializeField] private Image m_LockedIcon;
        [SerializeField] private TMP_Text m_UnlockText;
        [SerializeField] private TMP_Text m_DescriptionText;

        [Header("Audio")]
        [SerializeField] private AudioClip m_UnlockFillAudio;

        private const float m_DefaultLockValue = 0.1f;
        private const float m_FullUnlockValue = -3.6f;

        public IEnumerator ShowUnlock(LevelSO level)
        {
            m_LockedIcon.material = new Material(m_LockedIcon.material);

            int powerUpUnlockPercentage = SaveLoadManager.Instance.GetPowerUpUnlockPercentage(level.PowerUpReward.PowerUpID);
            m_Icon.sprite = level.PowerUpReward.PowerUpIcon;
            m_LockedIcon.sprite = level.PowerUpReward.PowerUpIcon;
            float value = Mathf.Lerp(m_DefaultLockValue, m_FullUnlockValue, powerUpUnlockPercentage / 100f);
            m_LockedIcon.material.SetFloat("_DirectionalGlowFadeFade", value);
            m_DescriptionText.text = level.PowerUpReward.PowerUpDescription;
            m_UnlockText.text = $"{powerUpUnlockPercentage}%";
            
            m_Panel.SetActive(true);

            yield return FillUnlock(level.PowerUpUnlockPercentage);
        }

        private IEnumerator FillUnlock(int unlockPercentage)
        {
            yield return new WaitForSeconds(1f);

            AudioManager.Instance.PlaySound(m_UnlockFillAudio);
            float value = Mathf.Lerp(m_DefaultLockValue, m_FullUnlockValue, unlockPercentage / 100f);
            yield return m_LockedIcon.material.DOFloat(value, "_DirectionalGlowFadeFade", 2f)
                .OnUpdate(()=>
                {
                    float fadeValue = m_LockedIcon.material.GetFloat("_DirectionalGlowFadeFade");
                    float unlockedValue = Mathf.InverseLerp(m_DefaultLockValue, m_FullUnlockValue, fadeValue);
                    m_UnlockText.text = $"{Mathf.Ceil(unlockedValue * 100f)}%";
                })
                .WaitForCompletion();

            yield return new WaitForSeconds(1f);

            if (unlockPercentage == 100)
            {
                m_IconObject.transform.DOPunchScale(m_IconObject.transform.localScale * 0.2f, 0.2f);
                m_UnlockText.text = "Unlocked!";
            }

            yield return new WaitForSeconds(2f);
        }

        public void HidePowerUpInfo()
        {
            StandardLevelManager.Instance.SetPowerUpData();
            m_Panel.SetActive(false);
        }
    }
}
