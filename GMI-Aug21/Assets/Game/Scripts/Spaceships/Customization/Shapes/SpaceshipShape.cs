using DG.Tweening;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    public class SpaceshipShape : MonoBehaviour
    {
        [SerializeField] Renderer m_Renderer;

        protected Material m_Material;

        public Renderer Renderer => m_Renderer;

        public Color ShapeColor
        {
            set => m_Material.SetColor("_Color", value);
            get => m_Material.GetColor("_Color");
        }

        public void Init()
        {
            m_Material = new Material(m_Renderer.material);
            m_Renderer.material = m_Material;
        }

        public void SetEffect(SpaceshipEffectType effect)
        {
            switch (effect)
            {
                case SpaceshipEffectType.None:
                    m_Material.SetFloat("_PixelateSize", 512f);
                    break;
                case SpaceshipEffectType.Pixelate:
                    m_Material.SetFloat("_PixelateSize", 32f);
                    break;
            }
        }

        public void DoMergeTint(Color color)
        {
            //m_Material.SetFloat("_SourceGlowDissolveFade", 0.5f);
            //ShapeColor = color;
            //float timeScale = Time.timeScale;
            //Time.timeScale = 0.25f;
            //m_Material.DOFloat(2.75f, "_SourceGlowDissolveFade", 0.25f)
            //    .OnComplete(()=>
            //    {
            //        Time.timeScale = timeScale;
            //    });

            //Time.timeScale = 0.25f;
            m_Material.DOColor(color, "_Color", 0.25f);
        }

        public void SetSortingLayer(string layer)
        {
            m_Renderer.sortingLayerName = layer;
        }
    }
}
