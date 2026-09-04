using DG.Tweening;
using System.Collections;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    public class GalacticEnemy : MonoBehaviour
    {
        [SerializeField] private GalacticEnemyPart m_Face;
        [SerializeField] private GalacticEnemyPart m_LeftHand;
        [SerializeField] private GalacticEnemyPart m_RightHand;

        private float m_AmplitudeY = 0.05f;
        private float m_AmplitudeX = 0.1f;
        private float m_Duration = 2.5f;

        private void Awake()
        {
            m_LeftHand.OnDead += LeftHandOnDead;
            m_RightHand.OnDead += RightHandOnDead;

            m_Face.SetTargeteable(false);
            m_LeftHand.SetTargeteable(true);
            m_RightHand.SetTargeteable(true);
        }

        private void Start()
        {
            StartCoroutine(Doattack());

            MoveFace();
            if (!m_LeftHand.IsDead)
                MoveHand(m_LeftHand.transform, 1f);

            if (!m_RightHand.IsDead)
                MoveHand(m_RightHand.transform, -1f);
        }

        private void OnDisable()
        {
            m_Face.transform.DOKill();
            m_LeftHand.transform.DOKill();
            m_RightHand.transform.DOKill();
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

        private void RightHandOnDead()
        {
            if (m_LeftHand.IsDead)
                m_Face.SetTargeteable(true);
        }

        private void LeftHandOnDead()
        {
            if (m_RightHand.IsDead)
                m_Face.SetTargeteable(true);
        }

        private IEnumerator Doattack()
        {
            while (true)
            {
                yield return new WaitForSeconds(2 * 60f);

                if (m_LeftHand.IsDead && m_RightHand.IsDead)
                    yield break;

                if (LevelManager.Instance.SpaceshipsAmount > 3)
                {
                    if (Random.value <= 0.5f)
                        DoLeftHandAttack();
                    else
                        DoRightHandAttack();
                }
            }
        }

        private void DoLeftHandAttack()
        {
            if (m_LeftHand.IsDead)
            {
                DoRightHandAttack();
                return;
            }

            DoHandAttack(m_LeftHand.transform);
        }

        private void DoRightHandAttack()
        {
            if (m_RightHand.IsDead)
            {
                DoLeftHandAttack();
                return;
            }

            DoHandAttack(m_RightHand.transform);
        }

        private void DoHandAttack(Transform hand)
        {
            Spaceship spaceship = CombatLevelManager.Instance.GetRandomSpaceship();
            if (spaceship == null)
                return;

            spaceship.SetCanMerge(false);

            Vector3 initialPos = hand.transform.position;
            hand.transform.DOTogglePause();
            Sequence seq = DOTween.Sequence();
            seq.Append(hand.transform.DOMove(spaceship.transform.position, 0.5f).SetEase(Ease.OutExpo).
                OnComplete(()=> LevelManager.Instance.RemoveSpaceship(spaceship)))
                .Append(hand.transform.DOMove(initialPos, 0.5f).SetEase(Ease.OutExpo))
                .OnComplete(() =>
                {
                    hand.transform.DOTogglePause();
                });
        }
    }
}
