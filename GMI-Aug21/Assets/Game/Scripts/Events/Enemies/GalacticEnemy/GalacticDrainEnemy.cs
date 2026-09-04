using DG.Tweening;
using Shapes;
using System.Collections;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    public class GalacticDrainEnemy : MonoBehaviour
    {
        [SerializeField] private GameObject m_Face;
        [SerializeField] private GameObject m_LeftHand;
        [SerializeField] private GameObject m_RightHand;

        [Header("Particles")]
        [SerializeField] private EnergyDrainParticles[] m_LeftHandParticles;
        [SerializeField] private EnergyDrainParticles[] m_RightHandParticles;

        [Header("Planet")]
        [SerializeField] private Planet[] m_Planets;

        [Header("Drain Power")]
        [SerializeField] private int m_DrainPower;

        private float m_AmplitudeY = 0.05f;
        private float m_AmplitudeX = 0.1f;
        private float m_Duration = 2.5f;

        private Coroutine m_DrainCoroutine;

        private void Start()
        {
            MoveFace();
            MoveHand(m_LeftHand.transform, 1f);
            MoveHand(m_RightHand.transform, -1f);

            m_DrainCoroutine = StartCoroutine(DrainPlanet());
        }

        private void OnDisable()
        {
            StopCoroutine(m_DrainCoroutine);
            m_LeftHand.transform.DOKill();
            m_RightHand.transform.DOKill();
            m_Face.transform.DOKill();
        }

        private void MoveFace()
        {
            m_Face.transform.DOScale(new Vector3(1.05f, 1.05f, 1f), 2f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

            m_Face.transform.DORotate(new Vector3(0, 0, 3f), 3f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        private void MoveHand(Transform hand, float direction)
        {
            Vector3 startPos = hand.position;

            hand.DOMoveX(startPos.x + (m_AmplitudeX * direction), m_Duration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);

            hand.DOMoveY(startPos.y + m_AmplitudeY, m_Duration * 0.8f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);

            hand.DORotate(new Vector3(0, 0, 15f * direction), m_Duration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        private IEnumerator DrainPlanet()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.1f / LevelManager.Instance.RewardLinesCount);

                foreach (var planet in m_Planets)
                {
                    if (planet.FullyHealed)
                        continue;

                    planet.DrainPlanet(m_DrainPower);
                    PlanetLevelManager.Instance.PlanetDrain(m_DrainPower);

                    foreach (var particles in m_LeftHandParticles)
                    {
                        particles.Emit();
                    }

                    foreach (var particles in m_RightHandParticles)
                    {
                        particles.Emit();
                    }
                }
            }
        }
    }
}
