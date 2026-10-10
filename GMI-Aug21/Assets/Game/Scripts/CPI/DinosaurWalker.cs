using DG.Tweening;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    /// <summary>
    /// Attach to a planet dinosaur. Walks it round a loop of path points, flipping it to face the way
    /// it is heading, and plays its two animations: the stretch and squash it idles with, and a side
    /// to side rock while it is on the move. Each is worked out on its own and all of them are written
    /// to the transform together once a frame, which is what lets them run on top of each other.
    /// CPIManager leaves the idle animation of a dinosaur carrying this component to it, and switches
    /// the component off when the dinosaur dies.
    /// </summary>
    public class DinosaurWalker : MonoBehaviour
    {
        [Header("Walk")]
        [Tooltip("Off leaves the dinosaur standing wherever it is. Turned off part way between two " +
            "points it stops there, and carries on from the same spot when turned back on.")]
        [SerializeField] private bool m_Walk = true;
        [Tooltip("Visited in order, then round again from the first. The dinosaur's feet go on the " +
            "point, not its centre.")]
        [SerializeField] private Transform[] m_PathPoints;
        [SerializeField, Min(0f)] private float m_WalkDuration = 1f;
        [SerializeField, Min(0f)] private float m_StayDuration = 1f;
        [SerializeField] private Ease m_WalkEase = Ease.InOutSine;
        [Tooltip("On when the sprite is drawn looking right. The X scale is negated whenever that is " +
            "not the way the dinosaur is heading.")]
        [SerializeField] private bool m_SpriteFacesRight = true;

        [Header("Walk Animation")]
        [Tooltip("Rocks the dinosaur side to side around its feet while it walks, like a toy being " +
            "walked along by hand.")]
        [SerializeField] private bool m_WalkAnimation = true;
        [SerializeField, Min(0f)] private float m_WalkTiltDegrees = 10f;
        [Tooltip("Full left and right rocks per second.")]
        [SerializeField, Min(0f)] private float m_WalkTiltsPerSecond = 3f;

        [Header("Scale Animation")]
        [Tooltip("Loops whether the dinosaur is walking or standing: it stretches taller, squashes " +
            "shorter and settles back, then does the same three steps with its width. The feet stay " +
            "where they are.")]
        [SerializeField] private bool m_ScaleAnimation = true;
        [SerializeField, Min(0f)] private float m_StretchPercent = 5f;
        [SerializeField, Min(0f)] private float m_SquashPercent = 6f;
        [SerializeField, Min(0f)] private float m_StretchDuration = 0.4f;
        [SerializeField, Min(0f)] private float m_SquashDuration = 0.3f;
        [SerializeField, Min(0f)] private float m_ReturnDuration = 0.25f;

        // How long an animation takes to fade in or out when its toggle changes or the walk stops,
        // so the dinosaur settles back instead of snapping upright.
        private const float k_TiltBlendDuration = 0.12f;
        private const float k_ScaleBlendDuration = 0.2f;
        // Feet this close to a path point count as already standing on it.
        private const float k_OnPointDistance = 0.05f;

        private Vector3 m_BaseScale;
        private Quaternion m_BaseRotation;
        private Vector3 m_FootAnchor;

        private Vector3 m_FootPosition;
        private Vector3 m_WalkStartPosition;
        private int m_TargetIndex;
        private float m_Timer;
        private bool m_IsWalking;
        private float m_Facing = 1f;

        private float m_TiltPhase;
        private float m_TiltWeight;
        private float m_ScaleTime;
        private float m_ScaleWeight;

        private void Awake()
        {
            m_BaseScale = transform.localScale;
            m_BaseRotation = transform.localRotation;

            // Everything is done around the middle of the feet rather than the pivot, which sits mid
            // sprite: it is the point put on the path, kept still by the stretch and rocked around.
            // Taken from the sprite's mesh because its bounds include the empty space under the feet.
            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                float bottom = float.MaxValue;
                foreach (Vector2 vertex in spriteRenderer.sprite.vertices)
                    bottom = Mathf.Min(bottom, vertex.y);

                m_FootAnchor = new Vector3(0f, bottom, 0f);
            }

            m_FootPosition = transform.localPosition + m_BaseRotation * Vector3.Scale(m_BaseScale, m_FootAnchor);

            // Started somewhere different for each dinosaur, so they do not all pulse in step.
            m_ScaleTime = Random.Range(0f, GetScaleCycleDuration());
            m_ScaleWeight = m_ScaleAnimation ? 1f : 0f;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            bool isWalking = m_Walk && AdvanceWalk(deltaTime);

            bool isTilting = isWalking && m_WalkAnimation;
            m_TiltWeight = Mathf.MoveTowards(m_TiltWeight, isTilting ? 1f : 0f, deltaTime / k_TiltBlendDuration);
            if (isTilting)
                m_TiltPhase += deltaTime * m_WalkTiltsPerSecond * Mathf.PI * 2f;

            m_ScaleWeight = Mathf.MoveTowards(m_ScaleWeight, m_ScaleAnimation ? 1f : 0f, deltaTime / k_ScaleBlendDuration);
            m_ScaleTime += deltaTime;

            ApplyPose();
        }

        /// <summary>
        /// Runs the stay and walk timers and moves the feet along the current leg. Returns whether the
        /// dinosaur is between two points once it has.
        /// </summary>
        private bool AdvanceWalk(float deltaTime)
        {
            if (m_PathPoints == null || m_PathPoints.Length == 0)
                return false;

            m_Timer += deltaTime;

            if (!m_IsWalking)
            {
                if (m_Timer < m_StayDuration)
                    return false;

                // A dinosaur placed on its first point would otherwise spend a whole walk going nowhere.
                if (m_PathPoints.Length > 1 && IsStandingOn(m_PathPoints[m_TargetIndex]))
                    m_TargetIndex = (m_TargetIndex + 1) % m_PathPoints.Length;

                if (m_PathPoints[m_TargetIndex] == null)
                {
                    m_TargetIndex = (m_TargetIndex + 1) % m_PathPoints.Length;
                    return false;
                }

                m_IsWalking = true;
                m_Timer = 0f;
                m_WalkStartPosition = m_FootPosition;
                FaceTowards(GetPathPosition(m_PathPoints[m_TargetIndex]) - m_FootPosition);

                // Only while upright, so a rock still fading out from the last leg is not cut short.
                if (m_TiltWeight <= 0f)
                    m_TiltPhase = 0f;
            }

            Transform target = m_PathPoints[m_TargetIndex];
            float progress = m_WalkDuration > 0f ? Mathf.Clamp01(m_Timer / m_WalkDuration) : 1f;

            if (target != null)
            {
                float easedProgress = DOVirtual.EasedValue(0f, 1f, progress, m_WalkEase);
                m_FootPosition = Vector3.LerpUnclamped(m_WalkStartPosition, GetPathPosition(target), easedProgress);
            }

            if (progress >= 1f || target == null)
            {
                m_IsWalking = false;
                m_Timer = 0f;
                m_TargetIndex = (m_TargetIndex + 1) % m_PathPoints.Length;
            }

            return m_IsWalking;
        }

        /// <summary>
        /// Where the feet go to stand on a path point, in the space the dinosaur is positioned in. The
        /// dinosaur keeps its own depth whatever the point's is.
        /// </summary>
        private Vector3 GetPathPosition(Transform pathPoint)
        {
            Transform parent = transform.parent;
            Vector3 position = parent != null ? parent.InverseTransformPoint(pathPoint.position) : pathPoint.position;
            position.z = m_FootPosition.z;

            return position;
        }

        private bool IsStandingOn(Transform pathPoint)
        {
            return pathPoint != null && (GetPathPosition(pathPoint) - m_FootPosition).sqrMagnitude < k_OnPointDistance * k_OnPointDistance;
        }

        private void FaceTowards(Vector3 direction)
        {
            // Which way the dinosaur looks along X as it was placed: it may have been flipped by hand,
            // with a negative scale or a turn around Y, and that has to count as its starting side.
            float placedFacing = (m_BaseRotation * Vector3.right).x * m_BaseScale.x * (m_SpriteFacesRight ? 1f : -1f);

            // Heading straight up or down, there is no side to pick: it keeps the one it has.
            if (Mathf.Abs(direction.x) < 0.0001f || Mathf.Approximately(placedFacing, 0f))
                return;

            m_Facing = direction.x * placedFacing > 0f ? 1f : -1f;
        }

        private void ApplyPose()
        {
            Vector2 stretch = Vector2.LerpUnclamped(Vector2.one, EvaluateStretch(m_ScaleTime), m_ScaleWeight);
            float tilt = Mathf.Sin(m_TiltPhase) * m_WalkTiltDegrees * m_TiltWeight;

            Vector3 scale = new Vector3(m_BaseScale.x * m_Facing * stretch.x, m_BaseScale.y * stretch.y, m_BaseScale.z);
            Quaternion rotation = Quaternion.Euler(0f, 0f, tilt) * m_BaseRotation;

            transform.localScale = scale;
            transform.localRotation = rotation;
            transform.localPosition = m_FootPosition - rotation * Vector3.Scale(scale, m_FootAnchor);
        }

        private float GetScaleCycleDuration()
        {
            return (m_StretchDuration + m_SquashDuration + m_ReturnDuration) * 2f;
        }

        /// <summary>
        /// The width and height multipliers at a point in the looping scale animation: the first half
        /// of a cycle works on the height, the second half repeats it on the width.
        /// </summary>
        private Vector2 EvaluateStretch(float time)
        {
            float halfDuration = GetScaleCycleDuration() / 2f;
            if (halfDuration <= 0f)
                return Vector2.one;

            time = Mathf.Repeat(time, halfDuration * 2f);

            bool isWidth = time >= halfDuration;
            if (isWidth)
                time -= halfDuration;

            float stretched = 1f + m_StretchPercent / 100f;
            float squashed = 1f - m_SquashPercent / 100f;
            float squashStartTime = m_StretchDuration;
            float returnStartTime = m_StretchDuration + m_SquashDuration;

            float multiplier;
            if (time < squashStartTime)
                multiplier = DOVirtual.EasedValue(1f, stretched, time / m_StretchDuration, Ease.InOutSine);
            else if (time < returnStartTime)
                multiplier = DOVirtual.EasedValue(stretched, squashed, (time - squashStartTime) / m_SquashDuration, Ease.InOutSine);
            else
                multiplier = DOVirtual.EasedValue(squashed, 1f, (time - returnStartTime) / m_ReturnDuration, Ease.InOutSine);

            return isWidth ? new Vector2(multiplier, 1f) : new Vector2(1f, multiplier);
        }

        private void OnDrawGizmosSelected()
        {
            if (m_PathPoints == null)
                return;

            Gizmos.color = Color.yellow;

            for (int i = 0; i < m_PathPoints.Length; i++)
            {
                Transform pathPoint = m_PathPoints[i];
                if (pathPoint == null)
                    continue;

                Gizmos.DrawWireSphere(pathPoint.position, 0.05f);

                Transform nextPathPoint = m_PathPoints[(i + 1) % m_PathPoints.Length];
                if (nextPathPoint != null)
                    Gizmos.DrawLine(pathPoint.position, nextPathPoint.position);
            }
        }
    }
}
