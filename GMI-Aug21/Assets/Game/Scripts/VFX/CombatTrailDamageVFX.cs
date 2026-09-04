using DG.Tweening;
using Oxtail.Utils;
using System;
using System.Collections;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    public class CombatTrailDamageVFX : MonoBehaviour
    {
        [Header("Visuals")]
        [SerializeField] private SpriteRenderer m_HeadColor;
        [SerializeField] private TrailRenderer m_TailColor;

        private float m_Speed = 10f;
        private float m_ArcStrength = 0.5f;

        public event Action<CombatTrailDamageVFX> OnDisabled;

        private void OnDestroy()
        {
            transform.DOKill();
        }

        public void SetColor(Color color)
        {
            Gradient gradient = new Gradient();

            GradientColorKey[] colorKeys = new GradientColorKey[2];
            colorKeys[0] = new GradientColorKey(color, 0.0f);
            colorKeys[1] = new GradientColorKey(color, 1.0f);

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1.0f, 0.0f);
            alphaKeys[1] = new GradientAlphaKey(0.0f, 1.0f);

            gradient.SetKeys(colorKeys, alphaKeys);

            m_TailColor.colorGradient = gradient;
            m_HeadColor.color = color;
        }

        public void Shoot(Vector3 startPos, CombatTarget target, int damage)
        {
            Vector3 endPos = target.transform.position;
            Vector2 direction = (endPos - startPos).normalized;
            Vector3 perpendicular = new Vector2(-direction.y, direction.x);
            float sideMultiplier = UnityEngine.Random.value <= 0.5f ? 1f : -1f;
            Vector3 midPoint = Vector3.Lerp(startPos, endPos, 0.5f) + (perpendicular * m_ArcStrength * sideMultiplier);

            Vector3[] path = new Vector3[] { startPos, midPoint, endPos };

            transform.position = startPos;
            transform.forward = Vector2.up;
            transform.DOKill();
            transform.DOPath(path, m_Speed, PathType.CatmullRom)
                .SetEase(Ease.InOutQuad)
                .SetSpeedBased(true)
                .OnUpdate(() => {
                    transform.right = transform.forward;
                })
                .OnComplete(() => 
                {
                    if (!target.IsDead)
                    {
                        BigNumber clampedDamage = target.TakeDamage(damage);
                        CombatLevelManager.Instance.TakeDamage(clampedDamage);
                    }
                    OnDisabled?.Invoke(this);
                    gameObject.SetActive(false);
                });
        }
    }
}
