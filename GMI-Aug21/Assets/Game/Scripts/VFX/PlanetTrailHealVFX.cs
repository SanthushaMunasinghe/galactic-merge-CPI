using System;
using System.Collections;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    public class PlanetTrailHealVFX : MonoBehaviour
    {
        public event Action<PlanetTrailHealVFX> OnDisabled;

        public void Shoot(Vector3 startPos, Planet planet, int heal)
        {
            StartCoroutine(MoveInCurve(startPos, planet, heal));
        }

        private IEnumerator MoveInCurve(Vector3 start, Planet planet, int heal)
        {
            float duration = 0.5f;
            float elapsed = 0f;

            Vector3 end = planet.transform.position;

            Vector3 controlPoint = start + (end - start) / 2 + Vector3.up * 5f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                Vector3 m1 = Vector3.Lerp(start, controlPoint, t);
                Vector3 m2 = Vector3.Lerp(controlPoint, end, t);
                transform.position = Vector3.Lerp(m1, m2, t);

                yield return null;
            }

            planet.HealPlanet(heal);
            PlanetLevelManager.Instance.PlanetHealed(heal);

            OnDisabled?.Invoke(this);
            gameObject.SetActive(false);
        }
    }
}
