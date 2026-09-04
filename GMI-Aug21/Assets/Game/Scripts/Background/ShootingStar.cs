using UnityEngine;
using System.Collections;

namespace Oxtail.SpaceshipIncremental
{
    /// <summary>
    /// Shooting star: plays the fx_shooting_star_strip frames (10 frames, 128 px)
    /// ONCE and then destroys itself. Add it to a GameObject with a SpriteRenderer,
    /// or launch one straight away with the static Spawn() method.
    /// </summary>
    public class ShootingStar : MonoBehaviour
    {
        [Tooltip("The 10 sprites of the strip, in order")]
        [SerializeField] private Sprite[] m_Frames;

        [SerializeField] private float m_Fps = 24f;

        [Tooltip("Drift speed while glowing (world units/second)")]
        [SerializeField] private Vector2 m_Drift = new Vector2(-0.6f, 0.45f);

        private int m_Index;
        private float m_Timer;
        private SpriteRenderer m_SpriteRenderer;

        private void Start()
        {
            m_SpriteRenderer = GetComponent<SpriteRenderer>();
            if (m_Frames == null || m_Frames.Length == 0)
            {
                Destroy(gameObject);
                return;
            }
            m_SpriteRenderer.sprite = m_Frames[0];
            m_Fps *= Random.Range(0.9f, 1.15f); // slight duration variety

            StartCoroutine(Trigger());
        }

        private IEnumerator Trigger()
        {
            while (true)
            {
                yield return new WaitForSeconds(10f);

                Spawn(m_Frames, Vector3.zero);
            }
        }

        private void Update()
        {
            transform.position += (Vector3)(m_Drift * Time.deltaTime);
            m_Timer += Time.deltaTime;
            if (m_Timer >= 1f / m_Fps)
            {
                m_Timer = 0f;
                m_Index++;
                if (m_Index >= m_Frames.Length)
                {
                    Destroy(gameObject);
                    return;
                }
                m_SpriteRenderer.sprite = m_Frames[m_Index];
            }
        }

        /// <summary>Spawns a shooting star at a random position around the given center.</summary>
        public static ShootingStar Spawn(Sprite[] stripFrames, Vector3 center, float spread = 4f)
        {
            GameObject go = new GameObject("ShootingStar");
            go.transform.position = center + new Vector3(
                Random.Range(-spread, spread), Random.Range(-spread * 0.5f, spread * 0.5f), 0f);
            go.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(-25f, 25f));

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 3; // above the background, below the gameplay

            ShootingStar star = go.AddComponent<ShootingStar>();
            star.m_Frames = stripFrames;
            return star;
        }
    }
}