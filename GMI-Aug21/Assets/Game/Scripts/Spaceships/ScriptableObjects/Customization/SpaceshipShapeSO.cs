using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    [CreateAssetMenu(fileName = "SpaceshipSkin", menuName = "Incremental/Spaceship Customization/Spaceship Skin")]
    public class SpaceshipShapeSO : SpaceshipCustomizationPartSO
    {
        [Header("Shape")]
        [SerializeField] private SpaceshipShape m_ShapePrefab;

        public SpaceshipShape ShapePrefab => m_ShapePrefab;

    }
}
