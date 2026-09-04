using Oxtail.Utils;
using System.Linq;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    public class PlanetLevelManager : StandardLevelManager
    {
        [Header("Planets")]
        [SerializeField] private Planet[] m_Planets;

        public static new PlanetLevelManager Instance => StandardLevelManager.Instance as PlanetLevelManager;

        public override void InitLevel(LevelSO level)
        {
            base.InitLevel(level);

            var planetSteps = m_SelectedLevelProgression.Steps.Where(x => x.StepType == ProgressionObjectiveType.RevivePlanet);
            var totalHeal = BigNumber.Zero;
            foreach ( var step in planetSteps )
            {
                totalHeal += step.StepGoal;
            }
            var planetHeal = totalHeal / m_Planets.Length;
            var rest = totalHeal % m_Planets.Length;

            for (int i = 0; i < m_Planets.Length; i++)
            {
                m_Planets[i].transform.SetParent(m_CurrentCircuit.ShapeParent);
                if (i == m_Planets.Length - 1)
                    m_Planets[i].SetPlanetLife(planetHeal + rest);
                else
                    m_Planets[i].SetPlanetLife(planetHeal);
            }
        }

        protected override void IncreaseProgressionStep()
        {
            base.IncreaseProgressionStep();
            var nextstep = m_SelectedLevelProgression.GetStep(LevelProgressionStepIndex);
            if (nextstep.StepType == ProgressionObjectiveType.RevivePlanet)
            {
                var destroyedPlanets = m_Planets.Where(x => !x.FullyHealed);
                if (destroyedPlanets.Any())
                    StepAccumulatedObjective.Value = destroyedPlanets.First().CurrentHeal;
            }
        }

        public void PlanetHealed(int heal)
        {
            UpdateProgressionStep(ProgressionObjectiveType.RevivePlanet, heal);
        }

        public void PlanetDrain(int drain)
        {
            UpdateProgressionStep(ProgressionObjectiveType.RevivePlanet, -drain);
        }

        public override void ChangeCircuit()
        {
            base.ChangeCircuit();
            
            foreach (var planet in m_Planets)
            {
                planet.transform.SetParent(m_CurrentCircuit.ShapeParent);
            }
        }

        protected override void CompleteLevel()
        {
            base.CompleteLevel();

            foreach (var planet in m_Planets)
            {
                planet.ResetPlanetSaveInfo();
            }

            SaveLoadManager.Instance.AddAchievementProgression(AchievementType.HealPlanet, 1);
        }
    }
}
