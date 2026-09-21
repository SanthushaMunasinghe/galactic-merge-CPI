using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    /// <summary>
    /// Attach to the camera. Every LateUpdate, eases the camera toward Target plus the offset the camera
    /// started at (captured on Awake when Capture Offset On Awake is on, so the current framing is
    /// preserved), using SmoothDamp (Smooth Time is roughly how long it takes to catch up; Max Speed
    /// caps how fast it may move, 0 = uncapped). Vertical Offset and Shake Offset are added on top of
    /// the smoothed position rather than being smoothed themselves: CPIManager tweens Vertical Offset
    /// between its inter-wave and battle framing, and CameraShake drives Shake Offset, so neither
    /// fights the follow by writing to the transform directly. This component is the only writer of
    /// the camera's position. Runs before other LateUpdates (parallax backgrounds read the camera's
    /// final position).
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform m_Target;
        [Tooltip("Keeps whatever offset from Target the camera starts with. Turn off to use Offset as authored.")]
        [SerializeField] private bool m_CaptureOffsetOnAwake = true;
        [SerializeField] private Vector3 m_Offset = new Vector3(0f, 0f, -10f);

        [Header("Smoothing")]
        [Tooltip("Approximate time in seconds to catch up with the target. Higher is floatier, 0 snaps.")]
        [SerializeField, Min(0f)] private float m_SmoothTime = 0.35f;
        [Tooltip("Cap on how fast the camera may move in units per second. 0 means uncapped.")]
        [SerializeField, Min(0f)] private float m_MaxSpeed;
        [SerializeField] private bool m_FollowX = true;
        [SerializeField] private bool m_FollowY = true;

        private Vector3 m_HomePosition;
        private Vector3 m_SmoothedPosition;
        private Vector3 m_Velocity;

        /// <summary>Extra world-space Y added on top of the followed position (CPIManager tweens this
        /// between its inter-wave and battle framing).</summary>
        public float VerticalOffset { get; set; }

        /// <summary>Extra world-space offset added on top of the followed position (CameraShake drives this).</summary>
        public Vector3 ShakeOffset { get; set; }

        private void Awake()
        {
            m_HomePosition = transform.position;

            if (m_Target != null && m_CaptureOffsetOnAwake)
                m_Offset = m_HomePosition - m_Target.position;

            m_SmoothedPosition = DesiredPosition();
        }

        private void LateUpdate()
        {
            m_SmoothedPosition = Vector3.SmoothDamp(
                m_SmoothedPosition,
                DesiredPosition(),
                ref m_Velocity,
                m_SmoothTime,
                m_MaxSpeed > 0f ? m_MaxSpeed : Mathf.Infinity,
                Time.deltaTime);

            transform.position = m_SmoothedPosition + new Vector3(0f, VerticalOffset, 0f) + ShakeOffset;
        }

        private Vector3 DesiredPosition()
        {
            if (m_Target == null)
                return m_HomePosition;

            Vector3 followed = m_Target.position + m_Offset;

            // Z is never followed: the camera keeps its own depth.
            return new Vector3(
                m_FollowX ? followed.x : m_HomePosition.x,
                m_FollowY ? followed.y : m_HomePosition.y,
                m_HomePosition.z);
        }
    }
}
