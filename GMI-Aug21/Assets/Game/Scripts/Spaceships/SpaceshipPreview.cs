using Oxtail.Utils;
using System;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    public class SpaceshipPreview : MonoBehaviour
    {
        [SerializeField] private int m_Tier;

        private SpaceshipShape m_SpaceshipShape;
        private SpaceshipTrail m_SpaceshipTrail;

        private SpaceshipEffectType m_Effect;

        private void Awake()
        {
            EventManager<UpdateShapePreviewEvent>.AddListener(UpdateShapePreview);
            EventManager<UpdateEffectPreviewEvent>.AddListener(UpdateEffectPreview);
            EventManager<UpdateTrailPreviewEvent>.AddListener(UpdateTrailPreview);
        }

        private void OnDestroy()
        {
            EventManager<UpdateShapePreviewEvent>.RemoveListener(UpdateShapePreview);
            EventManager<UpdateEffectPreviewEvent>.RemoveListener(UpdateEffectPreview);
            EventManager<UpdateTrailPreviewEvent>.RemoveListener(UpdateTrailPreview);
        }

        private void UpdateShapePreview(UpdateShapePreviewEvent evt)
        {
            if (m_SpaceshipShape != null)
                Destroy(m_SpaceshipShape.gameObject);

            m_SpaceshipShape = Instantiate(SpaceshipCustomizationSO.Instance.GetShapeByID(evt.ShapeID).ShapePrefab, transform);
            m_SpaceshipShape.Init();
            m_SpaceshipShape.SetEffect(m_Effect);

            SetTierColor();
        }

        private void UpdateEffectPreview(UpdateEffectPreviewEvent evt)
        {
            var effect = SpaceshipCustomizationSO.Instance.GetEffectByID(evt.EffectID);
            m_Effect = effect.EffectType;
            m_SpaceshipShape.SetEffect(SpaceshipEffectType.None);
            m_SpaceshipShape.SetEffect(effect.EffectType);
        }

        private void UpdateTrailPreview(UpdateTrailPreviewEvent evt)
        {
            if (m_SpaceshipTrail != null)
                Destroy(m_SpaceshipTrail.gameObject);

            var trail = SpaceshipCustomizationSO.Instance.GetTrailByID(evt.TrailID);
            m_SpaceshipTrail = Instantiate(trail.TrailPrefab, transform);
            m_SpaceshipTrail.Init(trail.OverrideColor);

            SetTierColor();
        }

        private void SetTierColor()
        {
            var tier = SpaceshipTiers.Instance.GetTier(m_Tier);
            
            if (m_SpaceshipShape != null)
                m_SpaceshipShape.ShapeColor = tier.SpaceShipColor;
            
            if (m_SpaceshipTrail != null)
                m_SpaceshipTrail.SetColor(tier.SpaceShipColor);
        }
    }
}
