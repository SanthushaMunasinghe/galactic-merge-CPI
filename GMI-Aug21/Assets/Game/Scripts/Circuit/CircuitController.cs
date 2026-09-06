using DG.Tweening;
using Oxtail.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    public struct RewardLineAddedEvent { }

    public class CircuitController : MonoBehaviour
    {
        [Header("Path")]
        [SerializeField] private Transform[] m_Path;
        [SerializeField] private PathType m_PathType;

        [Header("Reward Lines")]
        [SerializeField] private GameObject[] m_RewardLines;

        [Header("Shape")]
        [SerializeField] private Transform m_ShapeParent;

        private SpaceshipParent[] m_SpaceshipParents;
        private Tween m_CircuitVibration;

        private List<RewardLine> m_InternalRewardLines = new();

        public Transform ShapeParent => m_ShapeParent;
        public bool MaxRewardLines => m_InternalRewardLines.All(x => x.IsShown);
        public int RemainingRewardLines => m_InternalRewardLines.Where(x => !x.IsShown).Count();
        public bool FreeSpaceShipParents => m_SpaceshipParents.Any(x => x.IsFree);
        public int SpaceshipParentsCount => m_SpaceshipParents.Length;

        private void OnEnable()
        {
            m_SpaceshipParents = GetComponentsInChildren<SpaceshipParent>();
            m_InternalRewardLines.Clear();
            foreach (var rewardLine in m_RewardLines)
            {
                var line = rewardLine.GetComponentInChildren<RewardLine>(true);
                line.Init();
                m_InternalRewardLines.Add(line);
            }
        }

        private void OnDisable()
        {
            if (m_CircuitVibration != null)
                m_CircuitVibration.Kill();
        }

        private void OnDestroy()
        {
            if (m_CircuitVibration != null)
                m_CircuitVibration.Kill();
        }

        public void SetSpaceshipParentsPath(bool alignWithPathLocal = false, float rotationOffsetDegrees = 0f, bool negateDirection = false)
        {
            foreach (var spaceshipParent in m_SpaceshipParents)
            {
                Vector3[] path = GetPathPoints(spaceshipParent, negateDirection);
                spaceshipParent.SetPath(path, m_PathType, alignWithPathLocal, rotationOffsetDegrees);
            }
        }

        public void AddRewardLine()
        {
            if (m_InternalRewardLines.All(x => x.IsShown))
                return;

            m_InternalRewardLines.First(x => !x.IsShown).Show(true);
            UpdateCoinsPerSecond();
        }

        public void RemoveAllRewardLines()
        {
            foreach (var line in m_InternalRewardLines)
            {
                line.Show(false);
            }
        }

        /// <summary>
        /// Overrides the resting color, thickness and dashed state of every reward line on this
        /// circuit, shown or not yet shown. Used by CPIManager for local testing; normal gameplay
        /// never calls this, so authored per-circuit visuals are untouched elsewhere.
        /// </summary>
        public void SetRewardLinesAppearance(Color color, bool dashed, float thickness)
        {
            foreach (var line in m_InternalRewardLines)
            {
                line.SetAppearance(color, dashed, thickness);
            }
        }

        public RewardLine GetNextRewardLineToShow()
        {
            foreach(var line in m_InternalRewardLines)
            {
                if (!line.IsShown)
                    return line;
            }

            return null;
        }

        public void RemoveSpaceShip(Spaceship spaceship)
        {
            var parent = m_SpaceshipParents.FirstOrDefault(parent => parent.Spaceship == spaceship);
            if (parent != null)
                parent.RemoveSpaceship();

            UpdateCoinsPerSecond();
        }

        private Vector3[] GetPathPoints(SpaceshipParent parent, bool negateDirection = false)
        {
            List<Vector3> pathPoints = new();

            int startIndex = m_Path.ToList().IndexOf(parent.InitialWaypoint);
            int direction = negateDirection ? -1 : 1;

            for (int i = 1; i <= m_Path.Length; i++)
            {
                int index = ((startIndex + i * direction) % m_Path.Length + m_Path.Length) % m_Path.Length;
                pathPoints.Add(m_Path[index].position);
            }

            return pathPoints.ToArray();
        }

        public void StopPath()
        {
            foreach (var spaceshipParent in m_SpaceshipParents)
            {
                spaceshipParent.StopPath();
            }
        }

        public void ResumePath()
        {
            foreach (var spaceshipParent in m_SpaceshipParents)
            {
                spaceshipParent.ResumePath();
            }
        }

        private int GetClosestWaypointIndex(Transform spaceship)
        {
            float minDistance = float.MaxValue;
            int closest = 0;

            for (int i = 0; i < m_Path.Length; i++)
            {
                float dist = Vector3.Distance(spaceship.position, m_Path[i].position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closest = i;
                }
            }

            return closest;
        }

        public void IncreaseSpeed()
        {
            if (m_CircuitVibration != null)
                m_CircuitVibration.Kill();

            if (m_ShapeParent != null)
            {
                m_ShapeParent.localPosition = Vector3.zero;
                m_CircuitVibration = m_ShapeParent.DOShakePosition(0.1f, 0.1f).SetLoops(-1);
            }

            foreach (SpaceshipParent parent in m_SpaceshipParents)
            {
                parent.IncreaseSpeed();
            }

            UpdateCoinsPerSecond();
        }

        public void DecreaseSpeed()
        {
            if (m_CircuitVibration != null)
                m_CircuitVibration.Kill();

            if (m_ShapeParent != null)
                m_ShapeParent.localPosition = Vector3.zero;

            foreach (SpaceshipParent parent in m_SpaceshipParents)
            {
                if (parent != null)
                    parent.DecreaseSpeed();
            }

            UpdateCoinsPerSecond();
        }

        public SpaceshipParent GetSpaceshipParent(Spaceship spaceship)
        {
            var parent = m_SpaceshipParents.FirstOrDefault(parent => parent.Spaceship == spaceship);
            return parent;
        }

        //public SpaceshipParent GetSpaceshipParentByIndex(int index)
        //{
        //    return m_SpaceshipParents.Single(parent => parent.transform.parent.GetSiblingIndex() == index);
        //}

        public SpaceshipParent GetRandomFreeSpaceshipParent()
        {
            var parents = m_SpaceshipParents.Where(parent => parent.IsFree).ToList();
            if (parents.Count == 0)
                return null;

            return parents[Random.Range(0, parents.Count)];
        }

        public void UpdateCoinsPerSecond()
        {
            float coinsPerSecond = 0;
            foreach (var parent in m_SpaceshipParents)
            {
                if (parent.Spaceship != null)
                {
                    float lapsPerSecond = parent.Velocity / GetPathLongitude();
                    SpaceshipTier tier = SpaceshipTiers.Instance.GetTier(parent.Spaceship.TierNumber);
                    int rewardLinesCount = m_InternalRewardLines.Where(x => x.IsShown).Count();
                    coinsPerSecond += lapsPerSecond * rewardLinesCount * tier.Coins;
                }
            }

            EventManager<UpdateCoinsPerSecond>.TriggerEvent(new UpdateCoinsPerSecond(coinsPerSecond));
        }

        private float GetPathLongitude()
        {
            float longitude = 0f;
            for (int i = 0; i < m_Path.Length - 1; i++)
            {
                float distance = Vector3.Distance(m_Path[i].position, m_Path[i + 1].position);
                longitude += distance;
            }
            longitude += Vector3.Distance(m_Path[0].position, m_Path.Last().position);
            return longitude;
        }
    }
}
