using Oxtail.Utils;
using System.Linq;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    [CreateAssetMenu(fileName = "SpaceshipCustomization", menuName = "Incremental/Spaceship Customization")]
    public class SpaceshipCustomizationSO : ScriptableObjectSingleton<SpaceshipCustomizationSO>
    {
        [Header("Shapes")]
        [SerializeField] private SpaceshipShapeSO[] m_Shapes;

        [Space]
        [SerializeField] private SpaceshipShapeSO m_DefaultShape;

        [Header("Effects")]
        [SerializeField] private SpaceshipEffectSO[] m_Effects;

        [Space]
        [SerializeField] private SpaceshipEffectSO m_DefaultEffect;

        [Header("Trails")]
        [SerializeField] private SpaceshipTrailSO[] m_Trails;

        [Space]
        [SerializeField] private SpaceshipTrailSO m_DefaultTrail;

        public SpaceshipShapeSO DefaultShape => m_DefaultShape;
        public SpaceshipShapeSO[] Shapes => m_Shapes;

        public SpaceshipEffectSO[] Effects => m_Effects;
        public SpaceshipEffectSO DefaultEffect => m_DefaultEffect;

        public SpaceshipTrailSO[] Trails => m_Trails;
        public SpaceshipTrailSO DefaultTrail => m_DefaultTrail;

        public SpaceshipShapeSO GetShapeByID(string id)
        {
            var shape = m_Shapes.FirstOrDefault(x => x.CustomizationID == id);
            return shape ?? m_DefaultShape;
        }

        public SpaceshipEffectSO GetEffectByID(string id)
        {
            var effect = m_Effects.FirstOrDefault(x => x.CustomizationID == id);
            return effect ?? m_DefaultEffect;
        }

        public SpaceshipTrailSO GetTrailByID(string id)
        {
            var trail = m_Trails.FirstOrDefault(x => x.CustomizationID == id);
            return trail ?? m_DefaultTrail;
        }
    }
}
