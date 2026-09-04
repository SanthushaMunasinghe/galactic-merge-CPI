using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Oxtail.SpaceshipIncremental
{
    public class RVAddCoinsController : MonoBehaviour
    {
        [SerializeField] private Image m_Icon;
        [SerializeField] private GameObject m_IconAnimated;
        [SerializeField] private Image m_VideoIcon;
        [SerializeField] private ParticleSystem m_ParticleSystem;

        private void OnEnable()
        {
            StartCoroutine(DoSequence());
        }

        private IEnumerator DoSequence()
        {
            m_Icon.transform.localScale = Vector3.zero;
            m_VideoIcon.transform.localScale = Vector3.zero;
            m_Icon.material.SetFloat("_ShineFade", 0f);
            m_ParticleSystem.gameObject.SetActive(false);
            m_Icon.transform.DOKill();
            m_IconAnimated.SetActive(false);

            yield return m_Icon.transform.DOScale(Vector3.one, 1f)
                .SetEase(Ease.OutBounce).WaitForCompletion();

            m_IconAnimated.SetActive(true);

            yield return m_VideoIcon.transform.DOScale(Vector3.one, 1f)
                .SetEase(Ease.OutBounce).WaitForCompletion();

            m_Icon.material.SetFloat("_ShineFade", 1f);
            m_Icon.transform.DOLocalMoveY(m_Icon.transform.localPosition.y + 2f, 1f)
                .SetLoops(-1, LoopType.Yoyo);
            m_ParticleSystem.gameObject.SetActive(true);
            m_ParticleSystem.Play();
        }
    }
}
