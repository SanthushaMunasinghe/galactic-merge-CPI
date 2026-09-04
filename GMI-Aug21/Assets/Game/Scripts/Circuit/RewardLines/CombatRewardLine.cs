using System.Collections.Generic;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    public class CombatRewardLine : RewardLine
    {
        [Header("VFX")]
        [SerializeField] private CombatTrailDamageVFX m_HealParticle;

        private Queue<CombatTrailDamageVFX> m_ParticlesPool = new();

        protected void Awake()
        {
            CreatePool();
        }

        private void CreatePool()
        {
            for (int i = 0; i < 50; i++)
            {
                var particle = Instantiate(m_HealParticle, transform);
                particle.gameObject.SetActive(false);
                particle.OnDisabled += OnDisabled;
                m_ParticlesPool.Enqueue(particle);
            }
        }

        private void OnDisabled(CombatTrailDamageVFX particle)
        {
            m_ParticlesPool.Enqueue(particle);
        }

        public void SpawnAttackParticle(Color color, int damage)
        {
            CombatTarget target = CombatLevelManager.Instance.GetRandomTarget();

            if (target == null)
                return;

            if (target.IsDead)
                return;

            var particle = m_ParticlesPool.Dequeue();
            particle.gameObject.SetActive(true);
            particle.SetColor(color);
            particle.Shoot(transform.position, target, damage);
        }
    }
}
