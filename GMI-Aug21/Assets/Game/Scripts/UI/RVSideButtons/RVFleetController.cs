using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Oxtail.SpaceshipIncremental
{
    public class RVFleetController : MonoBehaviour
    {
        [SerializeField] private Animator m_Animator;
        [SerializeField] private Image[] m_Spaceships;
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
            foreach (var spaceship in m_Spaceships)
            {
                spaceship.material.SetFloat("_ShineFade", 0f);
            }

            m_Animator.enabled = true;
            m_AnimationFinished = false;
            m_ParticleSystem.gameObject.SetActive(false);

            yield return new WaitUntil(() => m_AnimationFinished);

            m_Animator.enabled = false;
            foreach (var spaceship in m_Spaceships)
            {
                spaceship.material.SetFloat("_ShineFade", 1f);
            }
            m_Sequence = DOTween.Sequence();
            foreach (var spaceship in m_Spaceships)
            {
                spaceship.material.SetFloat("_ShineFade", 0f);

                m_Sequence.Join(spaceship.transform.DOLocalMoveY(spaceship.transform.localPosition.y + 2f, 1f))
    .Join(spaceship.transform.DOLocalRotate(new Vector3(0f, 0f, 2f), 1f))
    .SetLoops(-1, LoopType.Yoyo);
            }

            m_ParticleSystem.gameObject.SetActive(true);
            m_ParticleSystem.Play();
        }

        private void SpaceshipFinished()
        {
            m_AnimationFinished = true;
        }
    }
}
