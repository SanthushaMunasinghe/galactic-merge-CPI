using Oxtail.Utils;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    public class LevelGenerator : MonoSingleton<LevelGenerator>
    {
        [Header("Endless Level")]
        [SerializeField] private EndlessModeSO m_EndlessMode;

        private int m_LevelIndex;

        private LevelManager m_CurrentLevel;

        public bool PlayedOtherLevelBefore { get; private set; } = false;

        protected override void Awake()
        {
            base.Awake();

            SaveLoadManager.Instance.RefreshDailyState();
            CreateNextLevel();
        }

        public void CreateNextLevel()
        {
            if (m_CurrentLevel != null)
            {
                EventManager<DestroyFloatingTextEvent>.TriggerEvent();
                DestroyImmediate(m_CurrentLevel.gameObject);
                PlayedOtherLevelBefore = true;
            }

            m_LevelIndex = SaveLoadManager.Instance.GetLevelIndex();
            LevelSO level = GameLevelsSO.Instance.GetLevelByIndex(m_LevelIndex);
            if (level != null)
            {
                var instantiatedLevel = Instantiate(level.LevelPrefab, transform);
                instantiatedLevel.InitLevel(level);
                m_CurrentLevel = instantiatedLevel;
            }
        }

        public void CreateDailyChallenge()
        {
            int index = SaveLoadManager.Instance.GetDailyChallengeIndex();
            var challenge = GameDailyChallengesSO.Instance.GetDailyChallengeByIndex(index);
            if (challenge != null)
            {
                if (m_CurrentLevel != null)
                {
                    EventManager<DestroyFloatingTextEvent>.TriggerEvent();
                    DestroyImmediate(m_CurrentLevel.gameObject);
                    PlayedOtherLevelBefore = true;
                }

                var level = Instantiate(challenge.LevelPrefab, transform);
                level.InitLevel(challenge);
                m_CurrentLevel = level;
            }
        }

        public void CreateEndlessMode()
        {
            if (m_CurrentLevel != null)
            {
                EventManager<DestroyFloatingTextEvent>.TriggerEvent();
                DestroyImmediate(m_CurrentLevel.gameObject);
                PlayedOtherLevelBefore = true;
            }

            var level = Instantiate(m_EndlessMode.LevelPrefab, transform);
            level.InitLevel(m_EndlessMode);
            m_CurrentLevel = level;
        }
    }
}