using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    [CreateAssetMenu(fileName = "EndlessMode", menuName = "Incremental/Modes/Endless Mode")]
    public class EndlessModeSO : ScriptableObject
    {
        [SerializeField] private EndlessModeLevelManager m_LevelPrefab;

        [Header("Ships")]
        [SerializeField] private InitialShips[] m_InitialShips;

        public EndlessModeLevelManager LevelPrefab => m_LevelPrefab;
        public InitialShips[] InitialShips => m_InitialShips;
    }
}
