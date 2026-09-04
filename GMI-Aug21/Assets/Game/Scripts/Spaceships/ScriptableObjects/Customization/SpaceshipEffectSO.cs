using Oxtail.Utils;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    public enum SpaceshipEffectType
    {
        None,
        Pixelate
    }

    [CreateAssetMenu(fileName = "SpaceshipEffect", menuName = "Incremental/Spaceship Customization/Spaceship Effect")]
    public class SpaceshipEffectSO : SpaceshipCustomizationPartSO
    {
        [Header("Effect")]
        [SerializeField] private SpaceshipEffectType m_EffectType;

        public SpaceshipEffectType EffectType => m_EffectType;
    }
}
