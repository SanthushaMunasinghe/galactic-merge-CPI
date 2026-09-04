using Oxtail.Utils;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    [System.Serializable]
    public struct InitialShips
    {
        public int InitialShipTier;
        public int InitialShipsAmount;
    }

    [CreateAssetMenu(fileName = "Level", menuName = "Incremental/Level/Level Data")]
    public class LevelSO : ScriptableObject
    {
        [SerializeField] private int m_LevelIndex;
        [SerializeField] private StandardLevelManager m_LevelPrefab;
        [SerializeField] private int m_GemsOnCompletion;
        [SerializeField, TextArea] private string m_LevelDescription;
        [SerializeField, TextArea] private string m_LevelFinishedDescription;

        [Header("Ships")]
        [SerializeField] private InitialShips[] m_InitialShips;

        [Header("Power Up")]
        [SerializeField] private PowerUpSO m_PowerUpReward;
        [SerializeField] private int m_PowerUpUnlockPercentage;

        public int LevelIndex => m_LevelIndex;
        public StandardLevelManager LevelPrefab => m_LevelPrefab;
        public int GemsOnCompletion => m_GemsOnCompletion;
        public string LevelDescription => m_LevelDescription;
        public string LevelFinishedDescription => m_LevelFinishedDescription;
        public InitialShips[] InitialShips => m_InitialShips;
        public PowerUpSO PowerUpReward => m_PowerUpReward;
        public int PowerUpUnlockPercentage => m_PowerUpUnlockPercentage;

        void OnValidate()
        {
            m_PowerUpUnlockPercentage = Mathf.Clamp
            (
                m_PowerUpUnlockPercentage,
                0,
                100
            );
        }
    }
}
