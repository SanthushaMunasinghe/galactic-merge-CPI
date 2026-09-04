using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    public class SpaceshipParticleTrail : SpaceshipTrail
    {
        [SerializeField] private ParticleSystem m_ParticleSystem;

        private ParticleSystemRenderer m_Renderer;
        private Material m_Material;

        public override void Init(bool overrideColor)
        {
            m_Renderer = m_ParticleSystem.GetComponent<ParticleSystemRenderer>();
            m_Material = new Material(m_Renderer.material);
            m_Renderer.material = m_Material;
            m_ParticleSystem.Play();
            m_OverrideColor = overrideColor;
        }

        public override void ResetTrail()
        {
            m_ParticleSystem.Stop();
        }

        public override void SetColor(Color color)
        {
            if (!m_OverrideColor)
                return;

            m_Material.SetColor("_StrongTintTint", color);
        }

        public override void SetSortingLayer(int layer)
        {
            m_Renderer.sortingLayerID = layer;
        }

        public override void SetSortingOrder(int order)
        {
            m_Renderer.sortingOrder = order;
        }
    }
}
