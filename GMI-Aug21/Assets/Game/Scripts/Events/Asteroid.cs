using DG.Tweening;
using Oxtail.Utils;
using System.Collections;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    public class Asteroid : CombatTarget
    {
        [Header("Comet")]
        [SerializeField] private SpriteRenderer m_Fire;
        [SerializeField] private SpriteRenderer m_Comet;

        [Header("Parts")]
        [SerializeField] private SpriteRenderer[] m_CometParts;

        private void Awake()
        {
            m_Comet.material = new Material(m_Comet.material);
        }

        private void Start()
        {
            Vector3 initialPos = transform.localPosition;
            transform.localPosition += new Vector3(0f, 5f, 0f);
            transform.DOLocalMoveY(initialPos.y, Random.Range(0.5f, 1f))
                .SetEase(Ease.InExpo)
                .SetDelay(0.1f).
                OnComplete(()=>
                {
                    m_Comet.transform.DOShakePosition(0.35f, 0.1f).SetLoops(-1);
                });
        }

        private void OnDisable()
        {
            m_Comet.transform.DOKill();
        }

        public override BigNumber TakeDamage(BigNumber damage)
        {
            m_Comet.material.SetFloat("_StrongTintFade", 0f);

            return base.TakeDamage(damage);
        }

        protected override IEnumerator DoDamageEffect()
        {
            yield return m_Comet.material.DOFloat(1f, "_StrongTintFade", 0.05f).WaitForCompletion();
            yield return m_Comet.material.DOFloat(0f, "_StrongTintFade", 0.05f).WaitForCompletion();
        }

        public override void SetDead()
        {
            IsDead = true;

            m_Fire.gameObject.SetActive(false);
            m_Comet.gameObject.SetActive(false);

            float angleStep = 360f / m_CometParts.Length;

            for (int i = 0; i < m_CometParts.Length; i++)
            {
                float force = Random.Range(1.5f, 3f);
                float duration = Random.Range(0.8f, 1.2f);
                float currentAngle = i * angleStep;
                var debris = m_CometParts[i];
                float rad = currentAngle * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                Vector3 targetPosition = transform.position + (Vector3)(direction * force);
                debris.transform.DOMove(targetPosition, duration).SetEase(Ease.OutQuad);
                debris.transform.DORotate(new Vector3(0, 0, Random.Range(-360, 360)), duration);
                debris.transform.DOScale(Vector3.zero, duration).SetEase(Ease.InBack);
                debris.DOFade(0f, duration);
            }

            SaveLoadManager.Instance.SaveTargetHealth(m_TargetIndex, -1);
        }
    }
}
