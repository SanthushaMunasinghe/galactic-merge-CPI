using DG.Tweening;
using Oxtail.Utils;
using System;
using System.Collections;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    public class GalacticEnemyPart : CombatTarget
    {
        [SerializeField] private SpriteRenderer m_EnemyPart;

        public event Action OnDead;

        private void Start()
        {
            m_EnemyPart.DOFade(0f, 0f);
            m_EnemyPart.DOFade(1f, 1f);
        }

        public override void SetDead()
        {
            IsDead = true;

            transform.DOKill();
            
            if (m_EnemyPart != null && m_EnemyPart.gameObject.activeInHierarchy)
            {
                m_EnemyPart.DOFade(0f, 1f);
                m_EnemyPart.transform.DOShakePosition(1f);
            }

            OnDead?.Invoke();
        }

        public override BigNumber TakeDamage(BigNumber damage)
        {
            m_EnemyPart.material.SetFloat("_StrongTintFade", 0f);

            return base.TakeDamage(damage);
        }

        protected override IEnumerator DoDamageEffect()
        {
            yield return m_EnemyPart.material.DOFloat(1f, "_StrongTintFade", 0.05f).WaitForCompletion();
            yield return m_EnemyPart.material.DOFloat(0f, "_StrongTintFade", 0.05f).WaitForCompletion();
        }
    }
}
