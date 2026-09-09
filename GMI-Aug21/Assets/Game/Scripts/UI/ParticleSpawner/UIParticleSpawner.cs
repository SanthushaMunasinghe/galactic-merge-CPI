using Oxtail.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    /// <summary>
    /// Singleton living on the CPI scene's Canvas. Pools TextUIParticle instances and
    /// spawns them at a per-UIParticleType configured screen position, with a
    /// per-type color and text prefix/suffix (e.g. "+" / "HP" -> "+10HP").
    /// </summary>
    public class UIParticleSpawner : MonoSingleton<UIParticleSpawner>
    {
        [Header("Text Particles")]
        [SerializeField] private TextUIParticle m_TextParticlePrefab;
        [SerializeField, Min(1)] private int m_PoolSize = 8;
        [SerializeField] private UIParticleTypeConfig[] m_TypeConfigs;

        [Header("Sprite Particles (reserved, unused)")]
        [SerializeField] private SpriteUIParticle m_SpriteParticlePrefab;

        private readonly Queue<TextUIParticle> m_TextPool = new();
        private Dictionary<UIParticleType, UIParticleTypeConfig> m_ConfigLookup;

        protected override void Awake()
        {
            base.Awake();

            m_ConfigLookup = m_TypeConfigs.ToDictionary(config => config.Type);

            for (int i = 0; i < m_PoolSize; i++)
            {
                CreateTextParticle();
            }
        }

        private void CreateTextParticle()
        {
            TextUIParticle particle = Instantiate(m_TextParticlePrefab, transform);
            particle.Disabled += OnTextParticleDisabled;
            particle.gameObject.SetActive(false);
            m_TextPool.Enqueue(particle);
        }

        private void OnTextParticleDisabled(TextUIParticle particle)
        {
            particle.gameObject.SetActive(false);
            m_TextPool.Enqueue(particle);
        }

        public void SpawnText(UIParticleType type, float? value = null, RectTransform spawnPointOverride = null)
        {
            if (!m_ConfigLookup.TryGetValue(type, out UIParticleTypeConfig config) || (config.SpawnPoint == null && spawnPointOverride == null))
            {
                Debug.LogWarning($"{nameof(UIParticleSpawner)}: no config/spawn point assigned for {type}.", this);
                return;
            }

            RectTransform spawnPoint = spawnPointOverride != null ? spawnPointOverride : config.SpawnPoint;

            string text = value.HasValue
                ? $"{config.Prefix}{value.Value:0}{config.Suffix}"
                : $"{config.Prefix}{config.Suffix}";

            if (m_TextPool.Count == 0)
                CreateTextParticle();

            TextUIParticle particle = m_TextPool.Dequeue();
            particle.transform.position = spawnPoint.position;
            particle.SetText(text);
            particle.SetColor(config.Color);
            particle.gameObject.SetActive(true);
        }
    }
}
