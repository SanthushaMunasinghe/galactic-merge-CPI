using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    [CreateAssetMenu(fileName = "SpaceshipTrail", menuName = "Incremental/Spaceship Customization/Spaceship Trail")]
    public class SpaceshipTrailSO : SpaceshipCustomizationPartSO
    {
        [Header("Color")]
        [SerializeField] private bool m_OverrideColor;

        [Header("Trail")]
        [SerializeField] private SpaceshipTrail m_TrailPrefab;

        public SpaceshipTrail TrailPrefab => m_TrailPrefab;
        public bool OverrideColor => m_OverrideColor;
    }
}
