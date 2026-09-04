using DG.Tweening;
using Oxtail.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    public enum RewardType
    {
        SpeedTime,
        Coins,
        DoubleCoins,
        FreeMerge
    }

    public struct ShowReward 
    {
        public RewardType Type { get; private set; }

        public ShowReward(RewardType type)
        {
            Type = type;
        }
    }

    public struct ShowVideoReward
    {
        public RewardType Type { get; private set; }

        public ShowVideoReward(RewardType type)
        {
            Type = type;
        }
    }

    public struct SpeedUpEventStarted { }
    public struct SpeedUpEventFinished { }

    public struct DoubleCoinEventStarted { }
    public struct DoubleCoinEventFinished { }

    public struct UpdateCoinsPerSecond
    {
        public BigNumber CoinsPerSecond { get; private set; }

        public UpdateCoinsPerSecond(BigNumber coinsPerSecond)
        {
            CoinsPerSecond = coinsPerSecond;
        }
    }


    public struct UpdateLevelProgression
    {
        public ProgressionStepInfo Step { get; private set; }

        public UpdateLevelProgression(ProgressionStepInfo step)
        {
            Step = step;
        }
    }

    public struct ShowFreeMerge { }

    public struct ShowLevelHUDEvent { }

    public struct ShowLevelCompleteEvent 
    {
        public int LevelIndex { get; private set; }
        public int MaxTierSpaceship { get; private set; }

        public ShowLevelCompleteEvent(int levelIndex, int maxTier)
        {
            LevelIndex = levelIndex;
            MaxTierSpaceship = maxTier;
        }
    }


    public abstract class LevelManager : MonoSingleton<LevelManager>
    {
        protected enum LevelType
        {
            PvE,
            DailyChallenge,
            EndlessMode
        }

        [Header("Level")]
        [SerializeField] private GameObject m_LevelView;
        [SerializeField] private GameObject m_MergeBackground;

        [Header("Canvas")]
        [SerializeField] private CanvasGroup m_CanvasGroup;

        [Header("Progression")]
        [SerializeField] private LevelProgressionSO m_LevelProgression;

        [Header("Optional Progression")]
        [SerializeField] private LevelProgressionSO m_OptionalLevelProgression;

        [Header("Spaceship")]
        [SerializeField] private Spaceship m_SpaceshipPrefab;

        [Header("Circuits")]
        [SerializeField] private CircuitController[] m_Circuits;

        [Header("Speed Particles")]
        [SerializeField] private ParticleSystem m_SpeedParticleSystem;

        [Header("Audio")]
        [SerializeField] protected AudioClip m_BackgroundMusic;
        [SerializeField] private AudioClip m_MergeSound;
        [SerializeField] private AudioClip m_CoinSound;
        [SerializeField] private AudioClip m_EngineSound;
        [SerializeField] private AudioClip m_SpeedSound;

        protected LevelProgressionSO m_SelectedLevelProgression;

        protected int m_LevelIndex;

        protected int m_CircuitIndex;
        protected CircuitController m_CurrentCircuit;

        protected List<Spaceship> m_SpawnedSpaceships = new();

        protected int m_MaxSpaceShipTierCreated;

        private int m_FreeMergeCount;

        protected int m_GemsOnCompletion;

        protected ProgressionStepInfo m_ProgressionInfo;

        private Coroutine m_SpeedUpCoroutine;

        protected Coroutine m_RewardSpawnCoroutine;
        protected Coroutine m_PassiveIncomeCoroutine;

        protected bool m_LevelFinished = false;

        private bool m_CanSpeedUp = true;

        protected bool m_IsRVFreeUpgrade;
        private float m_GoalStartTime;

        private bool m_IsMerging = false;

        public float MoneyMultiplier { get; private set; } = 1f;
        private float m_ExtraTierCreationChance = 0f;
        private float m_ExtraTierMergeChance = 0f;
        private float m_RewardTimeDecrease = 0f;
        private int m_PassiveIncome = 0;
        private float m_ExtraBoostTime = 0f;

        protected BigNumber m_CoinsEarnedPerGoal = 0;
        protected BigNumber m_CoinsSpendPerGoal = 0;
        protected int m_MergeCountPerGoal = 0;
        protected int m_AddSpaceshipCountPerGoal = 0;
        protected int m_AddRewardLineCountPerGoal = 0;
        protected int m_UpgradeCircuitCountPerGoal = 0;
        protected int m_SpeedUpCountPerGoal = 0;

        public ReactiveProperty<BigNumber> Money = new();
        public ReactiveProperty<BigNumber> StepAccumulatedObjective = new();
        public ReactiveProperty<bool> CanMerge = new();
        public ReactiveProperty<bool> CanSpawnSpaceship = new();
        public ReactiveProperty<bool> CanChangeCircuit = new(true);

        public int LevelIndex => m_LevelIndex;
        public int RewardLinesCount { get; protected set; }
        public int LevelProgressionStepIndex { get; protected set; }
        public int LevelProgressionSteps => m_SelectedLevelProgression.MaxStepIndex + 1;
        public bool DoubleCoinsActive { get; private set; }
        public bool CanSpawnRewardLine => !m_CurrentCircuit.MaxRewardLines;
        public bool HasMoreCircuits => m_CircuitIndex < m_Circuits.Length - 1;
        public int SpaceshipsAmount => m_SpawnedSpaceships.Count;
        public bool LevelFinished => m_LevelFinished;
        public float SpaceshipFreePercentage => (1f - (SpaceshipsAmount / (float)m_CurrentCircuit.SpaceshipParentsCount)) * 100f;
        public RewardLine NextRewardLineToShow => m_CurrentCircuit.GetNextRewardLineToShow();

        private const float m_SpeedUpTime = 5f;
        private const float m_DoubleCoinsTime = 10f;

        protected override void Awake()
        {
            base.Awake();
            SaveLoadManager.Instance.OnPowerUpUpdated += OnPowerUpUpdated;
        }

        private void OnDestroy()
        {
            Money.RemoveListeners();
            SaveLoadManager.Instance.OnPowerUpUpdated -= OnPowerUpUpdated;
        }

        private string FormatGoalSeconds()
        {
            float goalSeconds = Time.time - m_GoalStartTime;
            int mins = Mathf.FloorToInt(goalSeconds / 60);
            int seconds = Mathf.FloorToInt(goalSeconds % 60);
            return string.Format("{0:00}m:{1:00}s", mins, seconds);
        }

        protected void SelectStepsOption()
        {
            m_SelectedLevelProgression = m_LevelProgression;
            
            var levels = Debugger.ShowSecondaryStepsOnLevels;

            if (levels.Contains(m_LevelIndex + 1))
            {
                if (m_OptionalLevelProgression != null)
                    m_SelectedLevelProgression = m_OptionalLevelProgression;
            }
        }

        protected virtual void SetCircuitPath()
        {
            m_CurrentCircuit.SetSpaceshipParentsPath();
        }

        protected void SetCircuit(int index)
        {
            m_CircuitIndex = index;
            m_CurrentCircuit = m_Circuits[m_CircuitIndex];
            m_CurrentCircuit.gameObject.SetActive(true);

            for (int i = 0; i < m_CircuitIndex; i++)
            {
                m_Circuits[i].gameObject.SetActive(false);
            }
        }

        protected abstract List<int> GetSpawnedSpaceships();

        protected virtual IEnumerator CreateInitialSpaceships()
        {
            m_CanvasGroup.alpha = 0f;

            yield return new WaitForSeconds(0.5f);

            var spawnedTiers = GetSpawnedSpaceships();
            foreach (var tierNumber in spawnedTiers)
            {
                SpaceshipTier tier = SpaceshipTiers.Instance.GetTier(tierNumber);
                SpaceshipParent parent = m_CurrentCircuit.GetRandomFreeSpaceshipParent();
                if (parent == null)
                    continue;

                var spaceship = CreateSpaceship(tier, parent);
                DoSpaceshipCreationTransition(spaceship);
            }

            yield return new WaitForSeconds(1f);

            m_CanvasGroup.DOFade(1f, 0.5f);
        }

        protected abstract void CreateInitialRewardLines();

        protected virtual void StepCompleted()
        {
            SaveLoadManager.Instance.AddStepCompleted();
            SaveLoadManager.Instance.IncreaseCurrentPrestigeGoals();

            StartCoroutine(PerformStepCompleted());
        }

        private IEnumerator PerformStepCompleted()
        {
            yield return null;

            if (LevelProgressionStepIndex == m_SelectedLevelProgression.MaxStepIndex)
                CompleteLevel();
            else
            {
                IncreaseProgressionStep();
                UpdateProgressionStep();
            }
        }

        protected abstract void IncreaseProgressionStep();

        protected virtual void UpdateProgressionStep()
        {
            m_ProgressionInfo = m_SelectedLevelProgression.GetStep(LevelProgressionStepIndex);
            if (m_ProgressionInfo.StepType == ProgressionObjectiveType.MaxCircuitSpaceships)
            {
                m_ProgressionInfo = new ProgressionStepInfo
                {
                    StepType = ProgressionObjectiveType.MaxCircuitSpaceships,
                    StepGoal = m_CurrentCircuit.SpaceshipParentsCount
                };

                EventManager<UpdateGoalValue>.TriggerEvent(new UpdateGoalValue(m_ProgressionInfo.StepGoal));
                EventManager<UpdateLevelProgression>.TriggerEvent(new UpdateLevelProgression(m_ProgressionInfo));

                UpdateProgressionStep(ProgressionObjectiveType.MaxCircuitSpaceships, m_SpawnedSpaceships.Count);
            }
            else if (m_ProgressionInfo.StepType == ProgressionObjectiveType.UpgradeCircuit)
            {
                if (m_CircuitIndex == m_Circuits.Length - 1)
                    StepCompleted();
                else
                    EventManager<UpdateLevelProgression>.TriggerEvent(new UpdateLevelProgression(m_ProgressionInfo));
            }
            else if (m_ProgressionInfo.StepType == ProgressionObjectiveType.AddRewardLines)
            {
                int totalLines = 0;
                for (int i = m_CircuitIndex; i < m_Circuits.Length; i++)
                {
                    totalLines += m_Circuits[i].RemainingRewardLines;
                }
                if (totalLines < m_ProgressionInfo.StepGoal)
                    StepCompleted();
                else
                    EventManager<UpdateLevelProgression>.TriggerEvent(new UpdateLevelProgression(m_ProgressionInfo));
            }
            else if (m_ProgressionInfo.StepType == ProgressionObjectiveType.CreateTierSpaceship)
            {
                if (m_MaxSpaceShipTierCreated >= m_ProgressionInfo.StepGoal)
                    StepCompleted();
                else
                {
                    UpdateProgressionStep(ProgressionObjectiveType.CreateTierSpaceship, m_MaxSpaceShipTierCreated);
                    EventManager<UpdateLevelProgression>.TriggerEvent(new UpdateLevelProgression(m_ProgressionInfo));
                }
            }
            else
                EventManager<UpdateLevelProgression>.TriggerEvent(new UpdateLevelProgression(m_ProgressionInfo));
        }

        protected virtual void CompleteLevel()
        {
            if (m_LevelFinished)
                return;

            m_LevelFinished = true;
            
            if (m_PassiveIncomeCoroutine != null)
            {
                StopCoroutine(m_PassiveIncomeCoroutine);
                m_PassiveIncomeCoroutine = null;
            }
            if (m_RewardSpawnCoroutine != null)
            {
                StopCoroutine(m_RewardSpawnCoroutine);
                m_RewardSpawnCoroutine = null;
            }
            if (m_SpeedUpCoroutine != null)
            {
                StopCoroutine(m_SpeedUpCoroutine);
                m_SpeedUpCoroutine = null;
            }

            SaveLoadManager.Instance.AddMapCompleted();

            StartCoroutine(CompleteLevelCO());
        }

        protected abstract void SaveLevelCompletionData();

        protected virtual IEnumerator CompleteLevelCO()
        {
            yield return new WaitForSecondsRealtime(1f);

            SaveLoadManager.Instance.AddGems(m_GemsOnCompletion);

            SaveLevelCompletionData();

            m_LevelView.gameObject.SetActive(false);
        }

        protected abstract void SaveMoney();

        public void AddMoney(BigNumber money, bool countAsMission = true)
        {
            Money.Value += money * GetPrestigeMoneyBonus();
            SaveMoney();
            m_CoinsEarnedPerGoal += money;

            if (countAsMission)
                UpdateProgressionStep(ProgressionObjectiveType.CollectCoins, money);

            if (money > 0)
                AudioManager.Instance.PlaySound(m_CoinSound);
        }

        private float GetPrestigeMoneyBonus()
        {
            var prestigeConfig = Prestige.GetConfig();
            int prestigeLevel = SaveLoadManager.Instance.GetPrestigeLevel();
            return 1f + (prestigeLevel * prestigeConfig.CoinsBonusMultiplier);
        }

        public virtual void RemoveMoney(BigNumber money)
        {
            if (m_IsRVFreeUpgrade)
            {
                m_IsRVFreeUpgrade = false;
                return;
            }

            Money.Value -= money;
            SaveMoney();
            m_CoinsSpendPerGoal += money;
        }

        protected abstract int GetSpawnSpaceshipTier();

        public virtual void AddDefaultSpaceship()
        {
            int tierNumber = GetSpawnSpaceshipTier();
            if (UnityEngine.Random.value <= m_ExtraTierCreationChance)
                tierNumber++;

            AddSpaceshipToCircuit(tierNumber);
            m_AddSpaceshipCountPerGoal++;
        }

        public void AddNextTierSpaceship()
        {
            int tierNumber = GetSpawnSpaceshipTier();
            AddSpaceshipToCircuit(tierNumber + 1);

            UpdateProgressionStep(ProgressionObjectiveType.SpawnSpaceships, 1);
        }

        private void AddSpaceshipToCircuit(int tierNumber)
        {
            SpaceshipTier tier = SpaceshipTiers.Instance.GetTier(tierNumber);
            SpaceshipParent parent = m_CurrentCircuit.GetRandomFreeSpaceshipParent();
            if (parent == null)
                return;

            Spaceship spaceship = CreateSpaceship(tier, parent);
            DoSpaceshipCreationTransition(spaceship);
        }

        protected abstract void SetNewFloorTier();

        private Spaceship CreateSpaceship(SpaceshipTier tier, SpaceshipParent parent, bool fromMerge = false)
        {
            Spaceship spaceship = Instantiate(m_SpaceshipPrefab, parent.transform.position, parent.transform.rotation, parent.transform);
            spaceship.SetCanMerge(false);
            spaceship.SetSpaceshipTier(tier, fromMerge);
            parent.AddSpaceship(spaceship);
            AddSpaceship(spaceship);

            if (tier.TierNumber > m_MaxSpaceShipTierCreated)
                m_MaxSpaceShipTierCreated = tier.TierNumber;

            SetNewFloorTier();

            return spaceship;
        }

        private void DoSpaceshipCreationTransition(Spaceship spaceship)
        {
            float radius = 1f;
            float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            Vector3 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            spaceship.transform.localPosition += offset;
            spaceship.transform.DOLocalMove(Vector3.zero, 1f)
                .OnComplete(() =>
                {
                    spaceship.SetCanMerge(true);
                    CheckCanMerge();
                });
            spaceship.DoShowFade(0f, 1f, 1f);
            AudioManager.Instance.PlaySound(m_EngineSound);
        }

        protected abstract void SaveSpawnedSpaceshipsTiers(List<int> spaceships);

        private void AddSpaceship(Spaceship spaceship)
        {
            m_SpawnedSpaceships.Add(spaceship);
            var lists = m_SpawnedSpaceships.Select(spaceship => spaceship.TierNumber).ToList();
            SaveSpawnedSpaceshipsTiers(lists);
            CanSpawnSpaceship.Value = m_CurrentCircuit.FreeSpaceShipParents;
            UpdateProgressionStep(ProgressionObjectiveType.MaxCircuitSpaceships, 1);
            m_CurrentCircuit.UpdateCoinsPerSecond();
        }

        public void RemoveSpaceship(Spaceship spaceship, bool destroy = true)
        {
            m_SpawnedSpaceships.Remove(spaceship);
            m_CurrentCircuit.RemoveSpaceShip(spaceship);

            if (destroy)
                Destroy(spaceship.gameObject);

            CanSpawnSpaceship.Value = m_CurrentCircuit.FreeSpaceShipParents;

            UpdateProgressionStep(ProgressionObjectiveType.MaxCircuitSpaceships, -1);
        }

        public void CheckCanMerge()
        {
            CanMerge.Value = !m_IsMerging && m_SpawnedSpaceships.Where(spaceship => spaceship.CanMerge())
                .GroupBy(spaceship => spaceship.TierNumber)
                .Any(group => group.Count() >= 3);
        }

        public void MergeSpaceship()
        {
            StartCoroutine(MergeShipsCO());
            m_MergeCountPerGoal++;
        }

        private IEnumerator MergeShipsCO()
        {
            m_IsMerging = true;
            CanMerge.Value = false;
            CanSpawnSpaceship.Value = false;
            CanChangeCircuit.Value = false;

            var filterSpaceships = m_SpawnedSpaceships.Where(x => x.CanMerge());
            var groups = filterSpaceships.GroupBy(i => i.TierNumber);

            var maxTier = groups
                .Where(g => g.Count() >= 3)
                .OrderByDescending(g => g.Key)
                .FirstOrDefault();

            if (maxTier != null)
            {
                var spaceshipsGroup = m_SpawnedSpaceships.Where(spaceship => spaceship != null && spaceship.TierNumber == maxTier.Key && spaceship.CanMerge()).Take(3).ToList();
                if (spaceshipsGroup.Count() > 0)
                {
                    Spaceship firstSpaceship = spaceshipsGroup.ElementAt(0);
                    SpaceshipParent parent = m_CurrentCircuit.GetSpaceshipParent(firstSpaceship);
                    if (parent != null)
                    {
                        for (int i = 1; i < spaceshipsGroup.Count; i++)
                        {
                            Spaceship mergeSpaceship = spaceshipsGroup[i];
                            mergeSpaceship.transform.SetParent(null, true);
                            mergeSpaceship.transform.LookAt2D(firstSpaceship.transform.position);
                            mergeSpaceship.transform.eulerAngles += new Vector3(0f, 0f, -90f);
                            mergeSpaceship.transform.DOMove(firstSpaceship.transform.position, 0.25f)
                                .OnComplete(()=>
                                {
                                    Destroy(mergeSpaceship.gameObject);
                                });
                        }

                        for (int i = 0; i < spaceshipsGroup.Count; i++)
                        {
                            Spaceship mergeSpaceship = spaceshipsGroup[i];
                            mergeSpaceship.SetCanMerge(false);
                            RemoveSpaceship(mergeSpaceship, false);
                        }

                        yield return new WaitForSeconds(0.25f);

                        int tierNumber = maxTier.Key;
                        if (UnityEngine.Random.value <= m_ExtraTierMergeChance)
                            tierNumber++;

                        int newTier = tierNumber + 1;

                        if (newTier > m_MaxSpaceShipTierCreated)
                            UpdateProgressionStep(ProgressionObjectiveType.CreateTierSpaceship, 1);

                        UpdateMaxTierAchievement(newTier);

                        if (this is StandardLevelManager)
                            SaveLoadManager.Instance.AddAchievementProgression(AchievementType.MergeSpaceships, 1);

                        SpaceshipTier tier = SpaceshipTiers.Instance.GetTier(newTier);
                        var spaceship = CreateSpaceship(tier, parent, true);
                        AudioManager.Instance.PlaySound(m_MergeSound);

                        //m_MergeBackground.SetActive(true);
                        //float timeScale = Time.timeScale;
                        //yield return DisableMergeBackground();
                        //Time.timeScale = timeScale;
                        yield return new WaitForSeconds(0.5f);
                        yield return new WaitUntil(() => spaceship.FinishedMerging());
                    }
                }
            }

            CanSpawnSpaceship.Value = true;
            CanChangeCircuit.Value = true;

            m_IsMerging = false;
            CheckCanMerge();
        }

        private IEnumerator DisableMergeBackground()
        {
            yield return new WaitForSeconds(0.15f);
            m_MergeBackground.SetActive(false);
        }

        protected virtual void UpdateMaxTierAchievement(int newTier)
        {
            BigNumber achievementMaxTier = SaveLoadManager.Instance.GetAchievementProgression(AchievementType.SpaceshipTier);
            if (newTier > achievementMaxTier)
                SaveLoadManager.Instance.SaveAchievementProgression(AchievementType.SpaceshipTier, 1);
        }

        public virtual void AddRewardLine()
        {
            CreateRewardLine();
            SaveLoadManager.Instance.AddAchievementProgression(AchievementType.AddRewardLines, 1);
            m_AddRewardLineCountPerGoal++;
        }

        protected abstract void CreateRewardLine();

        public virtual void ChangeCircuit()
        {
            CanChangeCircuit.Value = false;
            m_CanSpeedUp = false;

            CircuitController oldCircuit = m_CurrentCircuit;
            m_CircuitIndex = Mathf.Min(m_CircuitIndex + 1, m_Circuits.Length - 1);
            m_CurrentCircuit = m_Circuits[m_CircuitIndex];
            m_CurrentCircuit.gameObject.SetActive(true);

            m_UpgradeCircuitCountPerGoal++;

            oldCircuit.DecreaseSpeed();

            foreach (var spaceship in m_SpawnedSpaceships)
            {
                if (spaceship == null) 
                    continue;

                SpaceshipParent oldParent = oldCircuit.GetSpaceshipParent(spaceship);
                if (oldParent == null)
                    continue;

                oldParent.RemoveSpaceship();
                int index = oldParent.transform.parent.GetSiblingIndex();
                SpaceshipParent newParent = m_CurrentCircuit.GetRandomFreeSpaceshipParent();
                if (newParent == null)
                    continue;

                spaceship.transform.SetParent(newParent.transform, false);
                spaceship.transform.rotation = newParent.transform.rotation;
                spaceship.transform.position = newParent.transform.position;
                spaceship.ResetTrail();
                newParent.AddSpaceship(spaceship);
            }

            oldCircuit.gameObject.SetActive(false);

            m_CurrentCircuit.SetSpaceshipParentsPath();
            CreateInitialRewardLines();

            if (m_SpeedUpCoroutine != null)
                m_CurrentCircuit.IncreaseSpeed();

            CanSpawnSpaceship.Value = m_CurrentCircuit.FreeSpaceShipParents;

            if (m_ProgressionInfo.StepType == ProgressionObjectiveType.MaxCircuitSpaceships)
            {
                m_ProgressionInfo = new ProgressionStepInfo
                {
                    StepType = ProgressionObjectiveType.MaxCircuitSpaceships,
                    StepGoal = m_CurrentCircuit.SpaceshipParentsCount
                };

                EventManager<UpdateGoalValue>.TriggerEvent(new UpdateGoalValue(m_ProgressionInfo.StepGoal));

                UpdateProgressionStep(ProgressionObjectiveType.MaxCircuitSpaceships, m_SpawnedSpaceships.Count);
            }

            CanChangeCircuit.Value = true;
            m_CanSpeedUp = true;
        }

        public abstract void IncreaseAddSpaceshipLevel();
        public abstract void SaveIncreaseMergeSpaceshipLevel();

        public void IncreaseMergeSpaceshipLevel()
        {
            if (m_FreeMergeCount > 0)
                m_FreeMergeCount = 0;
            else
            {
                BigNumber cost = GameUpgradesCostSO.Instance.GetMergeUpgradeCost();
                RemoveMoney(cost);
            }

            SaveIncreaseMergeSpaceshipLevel();

            UpdateProgressionStep(ProgressionObjectiveType.MergeSpaceShips, 1);
        }

        protected abstract void SaveIncreaseRewardLineLevel();

        public void IncreaseRewardLineLevel()
        {
            BigNumber cost = GameUpgradesCostSO.Instance.GetRewardLineUpgradeCost();
            RemoveMoney(cost);

            SaveIncreaseRewardLineLevel();

            UpdateProgressionStep(ProgressionObjectiveType.AddRewardLines, 1);
        }

        public virtual void IncreaseCircuitLevel()
        {
            BigNumber cost = GameUpgradesCostSO.Instance.GetCircuitUpgradeCost();
            RemoveMoney(cost);
        }

        public void RewardLineCrossed()
        {
            UpdateProgressionStep(ProgressionObjectiveType.CrossRewardLines, 1);
        }

        public void SpeedUpSpaceships()
        {
            if (!m_CanSpeedUp)
                return;

            if (m_SpeedUpCoroutine == null)
                m_SpeedUpCoroutine = StartCoroutine(SpeedUpSpaceshipsCO(m_SpeedUpTime + m_ExtraBoostTime));
        }

        private IEnumerator SpeedUpSpaceshipsCO(float time)
        {
            StartSpeedUp();

            //AudioSource speedAudioSOurce = AudioManager.Instance.PlaySound(m_SpeedSound);
            //speedAudioSOurce.loop = true;
            //speedAudioSOurce.pitch = -1.5f;

            yield return new WaitForSeconds(time);

            EndSpeedUp();

            //speedAudioSOurce.loop = false;
            //speedAudioSOurce.pitch = 1f;
            //speedAudioSOurce.Stop();
        }

        protected virtual void StartSpeedUp()
        {
            if (m_CurrentCircuit == null)
                return;

            EventManager<SpeedUpEventStarted>.TriggerEvent(new SpeedUpEventStarted());
            m_CurrentCircuit.IncreaseSpeed();
            m_SpeedParticleSystem.Play();
            UpdateProgressionStep(ProgressionObjectiveType.UseSpeedBoost, 1);
            m_SpeedUpCountPerGoal++;
        }

        private void EndSpeedUp()
        {
            m_CurrentCircuit.DecreaseSpeed();
            EventManager<SpeedUpEventFinished>.TriggerEvent(new SpeedUpEventFinished());
            m_SpeedUpCoroutine = null;
            m_SpeedParticleSystem.Stop();
        }

        private IEnumerator DoubleCoinsTimerCO(float time)
        {
            DoubleCoinsActive = true;
            EventManager<DoubleCoinEventStarted>.TriggerEvent(new DoubleCoinEventStarted());

            yield return new WaitForSeconds(time);

            DoubleCoinsActive = false;
            EventManager<DoubleCoinEventFinished>.TriggerEvent(new DoubleCoinEventFinished());
        }

        protected IEnumerator RewardSpawnCO()
        {
            while (true)
            {
                float randomTimer = UnityEngine.Random.Range(
                    Debugger.FloatingGiftRVRangeMinTime,
                    Debugger.FloatingGiftRVRangeMaxTime);
                randomTimer *= (1f - (m_RewardTimeDecrease / 100f));

                yield return new WaitForSeconds(randomTimer);

                float rvSpawnChance = Debugger.FloatingGiftRVShowPercentage / 100f;
                if (UnityEngine.Random.value <= rvSpawnChance)
                    SpawnVideoReward();
                else
                    SpawnReward();
            }
        }

        protected IEnumerator PassiveIncomeCO()
        {
            while (true)
            {
                yield return new WaitForSeconds(1f);

                if (m_PassiveIncome > 0)
                    AddMoney(m_PassiveIncome);
            }
        }

        private void SpawnReward()
        {
            var rewards = Enum.GetValues(typeof(RewardType)).Cast<RewardType>().ToList();
            int index = UnityEngine.Random.Range(0, rewards.Count);
            var type = rewards[index];
            EventManager<ShowReward>.TriggerEvent(new ShowReward(type));
        }

        private void SpawnVideoReward()
        {
            var rewards = Enum.GetValues(typeof(RewardType)).Cast<RewardType>().ToList();
            int index = UnityEngine.Random.Range(0, rewards.Count);
            var type = rewards[index];
            EventManager<ShowVideoReward>.TriggerEvent(new ShowVideoReward(type));
        }

        public virtual void ApplyReward(RewardType type)
        {
            switch (type)
            {
                case RewardType.SpeedTime:
                    StopSpeedUp();
                    m_SpeedUpCoroutine = StartCoroutine(SpeedUpSpaceshipsCO(20f));
                    break;
                case RewardType.Coins:
                    AddMoney(GameUpgradesCostSO.Instance.GetAddSpaceshipUpgradeCost());
                    break;
                case RewardType.DoubleCoins:
                    StartCoroutine(DoubleCoinsTimerCO(m_DoubleCoinsTime));
                    break;
                case RewardType.FreeMerge:
                    m_FreeMergeCount = 1;
                    EventManager<ShowFreeMerge>.TriggerEvent(new ShowFreeMerge());
                    break;
            }

            UpdateProgressionStep(ProgressionObjectiveType.CollectShinyGifts, 1);
        }

        public virtual void ApplyVideoReward(RewardType type)
        {
            switch (type)
            {
                case RewardType.SpeedTime:
                    StopSpeedUp();
                    m_SpeedUpCoroutine = StartCoroutine(SpeedUpSpaceshipsCO(60f));
                    break;
                case RewardType.Coins:
                    AddMoney(GameUpgradesCostSO.Instance.GetAddSpaceshipUpgradeCost() * 3f);
                    break;
                case RewardType.DoubleCoins:
                    StartCoroutine(DoubleCoinsTimerCO(m_DoubleCoinsTime * 3f));
                    break;
                case RewardType.FreeMerge:
                    m_FreeMergeCount = 3;
                    EventManager<ShowFreeMerge>.TriggerEvent(new ShowFreeMerge());
                    break;
            }

            UpdateProgressionStep(ProgressionObjectiveType.CollectShinyGifts, 1);
        }

        private void StopSpeedUp()
        {
            if (m_SpeedUpCoroutine != null)
            {
                StopCoroutine(m_SpeedUpCoroutine);
                m_SpeedUpCoroutine = null;
                m_CurrentCircuit.DecreaseSpeed();
            }
        }

        protected abstract void SaveStepAccumulatedObjective();

        protected void UpdateProgressionStep(ProgressionObjectiveType type, BigNumber value)
        {
            if (m_ProgressionInfo.StepType != type)
                return;

            if (m_LevelFinished)
                return;

            if (type == ProgressionObjectiveType.CreateTierSpaceship)
            {
                if (StepAccumulatedObjective.Value < value)
                    StepAccumulatedObjective.Value = value;
                else
                    StepAccumulatedObjective.Value = BigNumber.Max(StepAccumulatedObjective.Value + value, BigNumber.Zero);
            }
            else
                StepAccumulatedObjective.Value = BigNumber.Max(StepAccumulatedObjective.Value + value, BigNumber.Zero);
            
            SaveStepAccumulatedObjective();
            if (StepAccumulatedObjective.Value >= m_ProgressionInfo.StepGoal)
                StepCompleted();
        }

        private void OnPowerUpUpdated(string powerUpID)
        {
            CalculateMoneyMultiplier();
            CalculateExtraTierCreationChance();
            CalculateExtraTierMergeChance();
            CalculateRewardTimeDecrease();
            CalculatePassiveIncome();
            CalculateExtraBoostTime();
        }

        protected void CalculateMoneyMultiplier()
        {
            var powerup = GamePowerUpsSO.Instance.GetPowerUpByType(PowerUpType.Coins);
            int level = SaveLoadManager.Instance.GetPowerUpLevel(powerup.PowerUpID);
            if (level > 0)
                MoneyMultiplier = 1f * (1f + (powerup.GetUpgradeByLevel(level).Value / 100f));
            else
                MoneyMultiplier = 1;
        }

        protected void CalculateExtraTierCreationChance()
        {
            var powerup = GamePowerUpsSO.Instance.GetPowerUpByType(PowerUpType.CriticalCreation);
            int level = SaveLoadManager.Instance.GetPowerUpLevel(powerup.PowerUpID);
            if (level > 0)
                m_ExtraTierCreationChance = powerup.GetUpgradeByLevel(level).Value / 100f;
            else
                m_ExtraTierCreationChance = 0f;
        }

        protected void CalculateExtraTierMergeChance()
        {
            var powerup = GamePowerUpsSO.Instance.GetPowerUpByType(PowerUpType.CriticalMerge);
            int level = SaveLoadManager.Instance.GetPowerUpLevel(powerup.PowerUpID);
            if (level > 0)
                m_ExtraTierMergeChance = powerup.GetUpgradeByLevel(level).Value / 100f;
            else
                m_ExtraTierMergeChance = 0f;
        }

        protected void CalculateRewardTimeDecrease()
        {
            var powerup = GamePowerUpsSO.Instance.GetPowerUpByType(PowerUpType.RewardChance);
            int level = SaveLoadManager.Instance.GetPowerUpLevel(powerup.PowerUpID);
            if (level > 0)
                m_RewardTimeDecrease = powerup.GetUpgradeByLevel(level).Value;
            else
                m_RewardTimeDecrease = 0f;
        }

        protected void CalculatePassiveIncome()
        {
            var powerup = GamePowerUpsSO.Instance.GetPowerUpByType(PowerUpType.PassiveIncome);
            int level = SaveLoadManager.Instance.GetPowerUpLevel(powerup.PowerUpID);
            if (level > 0)
                m_PassiveIncome = Mathf.Max((int)powerup.GetUpgradeByLevel(level).Value, 1);
            else
                m_PassiveIncome = 0;
        }

        protected void CalculateExtraBoostTime()
        {
            var powerup = GamePowerUpsSO.Instance.GetPowerUpByType(PowerUpType.SpeedBoost);
            int level = SaveLoadManager.Instance.GetPowerUpLevel(powerup.PowerUpID);
            if (level > 0)
                m_ExtraBoostTime = powerup.GetUpgradeByLevel(level).Value;
            else
                m_ExtraBoostTime = 0;
        }

        protected void CalculateCoinsPerSecond()
        {

        }

        public void FinishLevelInstantly()
        {
            CompleteLevel();
        }

        public void SetIsRVFreeUpgrade()
        {
            m_IsRVFreeUpgrade = true;
        }
    }
}
