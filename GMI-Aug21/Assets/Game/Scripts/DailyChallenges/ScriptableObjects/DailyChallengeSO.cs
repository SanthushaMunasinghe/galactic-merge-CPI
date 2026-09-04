using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    [CreateAssetMenu(fileName = "DailyChallenge", menuName = "Incremental/Modes/Daily Challenge")]
    public class DailyChallengeSO : ScriptableObject
    {
        [SerializeField] protected int m_ChallengeIndex;
        [SerializeField] private DailyChallengeLevelManager m_LevelPrefab;
        [SerializeField] private int m_GemsOnCompletion;
        [SerializeField, TextArea] private string m_LevelFinishedDescription;

        [Header("Ships")]
        [SerializeField] private InitialShips[] m_InitialShips;

        public int ChallengeIndex => m_ChallengeIndex;
        public DailyChallengeLevelManager LevelPrefab => m_LevelPrefab;
        public int GemsOnCompletion => m_GemsOnCompletion;
        public string LevelFinishedDescription => m_LevelFinishedDescription;
        public InitialShips[] InitialShips => m_InitialShips;
    }
}
