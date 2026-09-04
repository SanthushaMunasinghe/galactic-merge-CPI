using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    public class IdleTrailMovement : MonoBehaviour
    {
        public float amplitude = 0.002f;
        public float frequency = 8f;

        Vector3 basePos;

        void Start()
        {
            basePos = transform.localPosition;
        }

        void Update()
        {
            float offset = Mathf.Sin(Time.time * frequency) * amplitude;
            transform.localPosition = basePos + Vector3.up * offset;
        }
    }
}
