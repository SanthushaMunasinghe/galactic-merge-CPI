using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    public class SpaceshipTrailRenderer : SpaceshipTrail
    {
        [SerializeField] TrailRenderer m_TrailRenderer;

        private Material m_Material;

        public override void Init(bool overrideColor)
        {
            m_Material = new Material(m_TrailRenderer.material);
            m_TrailRenderer.material = m_Material;
            m_OverrideColor = overrideColor;
        }

        public override void ResetTrail()
        {
            m_TrailRenderer.Clear();
        }

        public override void SetColor(Color color)
        {
            if (!m_OverrideColor)
                return;

            m_Material.SetColor("_Color", color);
        }

        public override void SetGradient(Gradient gradient)
        {
            if (!m_OverrideColor)
                return;

            m_TrailRenderer.colorGradient = gradient;
        }

        public override void SetSortingLayer(int layer)
        {
            m_TrailRenderer.sortingLayerID = layer;
        }

        public override void SetSortingOrder(int order)
        {
            m_TrailRenderer.sortingOrder = order;
        }
    }
}
