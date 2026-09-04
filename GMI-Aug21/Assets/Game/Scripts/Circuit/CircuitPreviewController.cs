using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    public class CircuitPreviewController : MonoBehaviour
    {
        [Header("Path")]
        [SerializeField] private Transform[] m_Path;
        [SerializeField] private PathType m_PathType;

        [Header("Reward Lines")]
        [SerializeField] private GameObject[] m_RewardLines;

        private SpaceshipParent[] m_SpaceshipParents;

        private void Awake()
        {
            m_SpaceshipParents = GetComponentsInChildren<SpaceshipParent>(true);
            SetSpaceshipParentsPath();
        }

        private void SetSpaceshipParentsPath()
        {
            foreach (var spaceshipParent in m_SpaceshipParents)
            {
                Vector3[] path = GetPathPoints(spaceshipParent.transform);
                spaceshipParent.SetPath(path, m_PathType);
            }
        }

        private Vector3[] GetPathPoints(Transform spaceship)
        {
            List<Vector3> pathPoints = new();

            int closestIndex = GetClosestWaypointIndex(spaceship);

            for (int i = 0; i < m_Path.Length; i++)
            {
                int index = (closestIndex + i) % m_Path.Length;
                pathPoints.Add(m_Path[index].position);
            }

            return pathPoints.ToArray();
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
    }
}
