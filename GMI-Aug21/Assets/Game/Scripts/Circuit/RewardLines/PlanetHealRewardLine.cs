using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    public class PlanetHealRewardLine : RewardLine
    {
        [Header("Planets")]
        [SerializeField] private Planet[] m_Planets;

        [Header("VFX")]
        [SerializeField] private PlanetTrailHealVFX m_HealParticle;

        private Queue<PlanetTrailHealVFX> m_ParticlesPool = new();

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

        private void OnDisabled(PlanetTrailHealVFX particle)
        {
            m_ParticlesPool.Enqueue(particle);
        }

        public void SpawnHealParticle(int heal)
        {
            Planet planet = m_Planets.FirstOrDefault(x => !x.FullyHealed);

            if (planet == null)
                return;

            var particle = m_ParticlesPool.Dequeue();
            particle.gameObject.SetActive(true);
            particle.Shoot(transform.position, planet, heal);
        }
    }
}
