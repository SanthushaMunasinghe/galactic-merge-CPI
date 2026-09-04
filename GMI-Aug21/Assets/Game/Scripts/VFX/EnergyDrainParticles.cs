using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    public class EnergyDrainParticles : MonoBehaviour
    {
        [SerializeField] private ParticleSystem m_ParticleSystem;
        [SerializeField] private Transform m_Planet;
        [SerializeField] private Transform m_Hand;
        [SerializeField] private float m_Speed = 3f;

        ParticleSystem.Particle[] m_Particles;

        private void Start()
        {
            transform.position = m_Planet.position;
        }

        void Update()
        {
            int count = m_ParticleSystem.particleCount;

            if (m_Particles == null || m_Particles.Length < count)
                m_Particles = new ParticleSystem.Particle[count];

            m_ParticleSystem.GetParticles(m_Particles);

            for (int i = 0; i < count; i++)
            {
                Vector3 dir = (m_Hand.position - m_Particles[i].position).normalized;
                m_Particles[i].position += dir * m_Speed * Time.deltaTime;

                if (Vector3.Distance(m_Particles[i].position, m_Hand.position) < 0.1f)
                    m_Particles[i].remainingLifetime = 0;
            }

            m_ParticleSystem.SetParticles(m_Particles, count);
        }

        public void Emit()
        {
            m_ParticleSystem.Emit(1);
        }
    }
}
