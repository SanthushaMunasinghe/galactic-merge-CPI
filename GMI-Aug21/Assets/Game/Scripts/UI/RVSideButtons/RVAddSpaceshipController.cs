using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Oxtail.SpaceshipIncremental
{
    public class RVAddSpaceshipController : MonoBehaviour
    {
        [SerializeField] private Animator m_Animator;
        [SerializeField] private Image m_Spaceship;
        [SerializeField] private ParticleSystem m_ParticleSystem;

        private bool m_AnimationFinished;
        private Sequence m_Sequence;

        private void OnEnable()
        {
            StartCoroutine(DoSequence());
        }

        private IEnumerator DoSequence()
        {
            if (m_Sequence != null)
                m_Sequence.Kill();

            m_Animator.Rebind();
            m_Spaceship.material.SetFloat("_ShineFade", 0f);
            m_Animator.enabled = true;
            m_AnimationFinished = false;
            m_ParticleSystem.gameObject.SetActive(false);

            yield return new WaitUntil(() => m_AnimationFinished);

            m_Animator.enabled = false;
            m_Spaceship.material.SetFloat("_ShineFade", 1f);
            m_Sequence = DOTween.Sequence();
            m_Sequence.Append(m_Spaceship.transform.DOLocalMoveY(m_Spaceship.transform.localPosition.y + 2f, 1f))
                .Join(m_Spaceship.transform.DOLocalRotate(new Vector3(0f, 0f, 2f), 1f))
                .SetLoops(-1, LoopType.Yoyo);
            m_ParticleSystem.gameObject.SetActive(true);
            m_ParticleSystem.Play();
        }

        private void SpaceshipFinished()
        {
            m_AnimationFinished = true;
        }
    }
}
