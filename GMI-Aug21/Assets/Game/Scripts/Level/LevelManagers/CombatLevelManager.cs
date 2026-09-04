using DG.Tweening;
using Oxtail.Utils;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    public class CombatLevelManager : StandardLevelManager
    {
        [Header("Targets")]
        [SerializeField] private CombatTarget[] m_Targets;

        [Header("Circuit")]
        [SerializeField] private GameObject m_CircuitsParent;
        [SerializeField] private float m_CircuitMoveY;

        public static new CombatLevelManager Instance => StandardLevelManager.Instance as CombatLevelManager;

        public override void InitLevel(LevelSO level)
        {
            base.InitLevel(level);
            m_CircuitsParent.transform.position += new Vector3(0f, -m_CircuitMoveY, 0f);
            m_CurrentCircuit.SetSpaceshipParentsPath();
        }

        protected override void SetCircuitPath()
        {
        }

        protected override void UpdateProgressionStep()
        {
            base.UpdateProgressionStep();

            if (m_ProgressionInfo.StepType != ProgressionObjectiveType.DestroyTargets)
                return;

            SetTargetsLife(m_ProgressionInfo.StepGoal);
        }

        private void SetTargetsLife(BigNumber totalHealth)
        {
            BigNumber targetHealth = totalHealth / m_Targets.Length;
            BigNumber rest = totalHealth % m_Targets.Length;

            foreach (var target in m_Targets)
            {
                int index = target.TargetIndex;
                BigNumber savedHealth = SaveLoadManager.Instance.GetTargetHealth(index);
                if (savedHealth > -1)
                    targetHealth = savedHealth;

                target.SetHealth(targetHealth + ((index == 0) ? rest : 0));
                if (targetHealth == 0)
                    target.SetDead();
            }

            if (m_Targets.Length == 0 ||
                m_Targets.All(x => x.IsDead))
                StepCompleted();
        }

        public CombatTarget GetRandomTarget()
        {
            if (m_Targets.Length == 0) 
                return null;

            if (m_Targets.All(x => x.IsDead))
                return null;

            if (m_Targets.All(x => !x.CanBeTargeted))
                return null;

            return m_Targets.Where(x => !x.IsDead && x.CanBeTargeted).GetRandomElement();
        }

        public void TakeDamage(BigNumber damage)
        {
            UpdateProgressionStep(ProgressionObjectiveType.DestroyTargets, damage);
            SaveLoadManager.Instance.AddAchievementProgression(AchievementType.TotalHazardDamage, damage);

            if (m_Targets.Length == 0 ||
                m_Targets.All(x => x.IsDead))
                StepCompleted();
        }

        public Spaceship GetRandomSpaceship()
        {
            if (m_SpawnedSpaceships.Count == 0) 
                return null;

            return m_SpawnedSpaceships.Where(x => x.CanMerge()).GetRandomElement();
        }

        protected override IEnumerator CompleteLevelCO()
        {
            yield return base.CompleteLevelCO();

            foreach (var target in m_Targets)
            {
                target.ResetSaveLife();
            }
        }
    }
}
