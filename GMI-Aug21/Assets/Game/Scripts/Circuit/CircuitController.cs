using DG.Tweening;
using Oxtail.Utils;
using Shapes;
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

        // Each side of an outline stroke gets two stacked glow rings, a tight bright core and a wide
        // faint halo, which together read as a blur. Width is a fraction of the requested glow width,
        // Alpha a multiplier on the requested intensity.
        private static readonly (float Width, float Alpha)[] s_OutlineGlowLayers = { (0.25f, 1.5f), (1f, 0.3f) };

        private SpaceshipParent[] m_SpaceshipParents;
        private Tween m_CircuitVibration;
        private GameObject m_OutlineGlow;

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

        /// <summary>
        /// Adds a soft additive halo on both sides of the two outline strokes of this circuit, made
        /// of Shapes rings that fade out radially, so it needs no texture and no post processing.
        /// Only a shape drawn as a track ring over a slightly thicker outline ring (the circle
        /// circuit) is supported. Calling it again rebuilds the halo, and a zero width or intensity
        /// removes it. Used by CPIManager for local testing; normal gameplay never calls this.
        /// </summary>
        public void SetOutlineGlow(Color color, float width, float intensity)
        {
            if (m_OutlineGlow != null)
                Destroy(m_OutlineGlow);

            if (width <= 0f || intensity <= 0f || m_ShapeParent == null)
                return;

            var rings = m_ShapeParent.Cast<Transform>()
                .Select(child => child.GetComponent<Disc>())
                .Where(disc => disc != null && disc.Type == DiscType.Ring)
                .OrderBy(disc => disc.Thickness)
                .ToList();

            if (rings.Count < 2)
            {
                Debug.LogWarning($"{nameof(CircuitController)}: {name} is not drawn as a track ring over an " +
                    "outline ring, cannot add an outline glow.", this);
                return;
            }

            Disc track = rings.First();
            Disc outline = rings.Last();

            // Parented to the outline itself so the halo keeps its centre and scale, and shakes with it.
            m_OutlineGlow = new GameObject("OutlineGlow") { layer = outline.gameObject.layer };
            m_OutlineGlow.transform.SetParent(outline.transform, false);

            float trackInner = track.Radius - track.Thickness * 0.5f;
            float trackOuter = track.Radius + track.Thickness * 0.5f;
            float outlineInner = outline.Radius - outline.Thickness * 0.5f;
            float outlineOuter = outline.Radius + outline.Thickness * 0.5f;

            // Glow facing away from the track sits behind the outline. Glow spilling onto the track
            // has to be drawn over it, and stops at the track centre so the two sides never stack
            // into a solid band.
            int behindOrder = outline.SortingOrder - 1;
            int overTrackOrder = track.SortingOrder + 1;

            foreach (var glowLayer in s_OutlineGlowLayers)
            {
                var glowColor = new Color(color.r, color.g, color.b, color.a * intensity * glowLayer.Alpha);
                float layerWidth = width * glowLayer.Width;
                float trackWidth = Mathf.Min(layerWidth, track.Thickness * 0.5f);

                AddOutlineGlowRing(outline, Mathf.Max(0f, outlineInner - layerWidth), outlineInner, Color.clear, glowColor, behindOrder);
                AddOutlineGlowRing(outline, trackInner, trackInner + trackWidth, glowColor, Color.clear, overTrackOrder);
                AddOutlineGlowRing(outline, trackOuter - trackWidth, trackOuter, Color.clear, glowColor, overTrackOrder);
                AddOutlineGlowRing(outline, outlineOuter, outlineOuter + layerWidth, glowColor, Color.clear, behindOrder);
            }
        }

        // Fading both color and alpha to nothing makes the additive result fall off quadratically
        // rather than linearly, which is what keeps the edge of the halo from showing as a line.
        private void AddOutlineGlowRing(Disc outline, float innerRadius, float outerRadius, Color innerColor, Color outerColor, int sortingOrder)
        {
            var ring = new GameObject("GlowRing") { layer = outline.gameObject.layer };
            ring.transform.SetParent(m_OutlineGlow.transform, false);

            var disc = ring.AddComponent<Disc>();
            disc.Type = DiscType.Ring;
            disc.BlendMode = ShapesBlendMode.Additive;
            disc.Radius = (innerRadius + outerRadius) * 0.5f;
            disc.Thickness = outerRadius - innerRadius;
            disc.ColorMode = Disc.DiscColorMode.Radial;
            disc.ColorInner = innerColor;
            disc.ColorOuter = outerColor;
            disc.SortingLayerID = outline.SortingLayerID;
            disc.SortingOrder = sortingOrder;
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

        /// <summary>
        /// First free slot in authored order (matching the sibling order GetComponentsInChildren
        /// returned in OnEnable, i.e. each SpaceshipParent's position around the track), rather than
        /// a random one. Used by CPIManager to queue the initial batch of ships one behind another.
        /// </summary>
        public SpaceshipParent GetNextFreeSpaceshipParentInOrder()
        {
            return m_SpaceshipParents.FirstOrDefault(parent => parent.IsFree);
        }

        /// <summary>
        /// Overrides the resting speed multiplier of every SpaceshipParent on this circuit, occupied
        /// or not, so a ship added later inherits it too. Used by CPIManager for local testing;
        /// normal gameplay never calls this.
        /// </summary>
        public void SetSpeedMultiplier(float multiplier)
        {
            foreach (var parent in m_SpaceshipParents)
            {
                parent.SetSpeedMultiplier(multiplier);
            }
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
