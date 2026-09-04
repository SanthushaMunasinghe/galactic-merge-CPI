using Oxtail.Utils;
using System.Collections;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    public abstract class CombatTarget : MonoBehaviour, ICombatTarget
    {
        [SerializeField] protected int m_TargetIndex;

        private BigNumber m_Health;

        protected Coroutine m_DamageEffectCoroutine;

        public int TargetIndex => m_TargetIndex;
        public bool CanBeTargeted { get; protected set; } = true;
        public bool IsDead { get; protected set; }

        public abstract void SetDead();
        protected abstract IEnumerator DoDamageEffect();

        public virtual void SetHealth(BigNumber health)
        {
            m_Health = health;
            if (m_Health <= 0)
                gameObject.SetActive(false);
        }

        public virtual BigNumber TakeDamage(BigNumber damage)
        {
            if (m_DamageEffectCoroutine != null)
            {
                StopCoroutine(m_DamageEffectCoroutine);
                m_DamageEffectCoroutine = null;
            }

            BigNumber clampedDamage = BigNumber.Clamp(damage, BigNumber.Zero, m_Health); 

            m_Health -= clampedDamage;
            SaveLoadManager.Instance.SaveTargetHealth(m_TargetIndex, m_Health);
            if (m_Health == BigNumber.Zero)
                SetDead();
            else
            {
                if (gameObject.activeInHierarchy)
                    m_DamageEffectCoroutine = StartCoroutine(DoDamageEffect());
            }

            return clampedDamage;
        }

        public void ResetSaveLife()
        {
            SaveLoadManager.Instance.SaveTargetHealth(m_TargetIndex, new BigNumber(-1));
        }

        public void SetTargeteable(bool targeted) => CanBeTargeted = targeted;
    }
}
