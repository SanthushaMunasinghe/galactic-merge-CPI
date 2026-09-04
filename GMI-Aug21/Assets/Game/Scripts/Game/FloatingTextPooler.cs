using Oxtail.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    public class FloatingTextPooler : MonoSingleton<FloatingTextPooler>
    {
        [SerializeField] private FloatingText m_TextPrefab;
        [SerializeField] private int m_PoolSize;

        private Queue<FloatingText> m_Pool = new();

        private Color? m_GlowColorOverride;

        private readonly Vector3 m_DefaultScale = new Vector3(2f, 2f, 2f);

        protected override void Awake()
        {
            base.Awake();
            CreatePool();
        }

        private void OnDestroy()
        {
            foreach (var view in m_Pool)
            {
                view.Disabled -= TextDisabled;
            }
        }

        private void CreatePool()
        {
            for (int i = 0; i < m_PoolSize; i++)
            {
                CreateText();
            }
        }

        private void CreateText()
        {
            FloatingText view = Instantiate(m_TextPrefab);
            view.transform.SetParent(transform);
            view.Disabled += TextDisabled;
            view.gameObject.SetActive(false);
            view.SetGlowColorOverride(m_GlowColorOverride);
            m_Pool.Enqueue(view);
        }

        /// <summary>
        /// Forces every floating text's glow color, pooled and future, regardless of what each
        /// caller tints the text itself. Used by CPIManager for local testing; pass null to let
        /// callers control the glow color again.
        /// </summary>
        public void SetGlowColorOverride(Color? color)
        {
            m_GlowColorOverride = color;

            foreach (var text in m_Pool)
            {
                text.SetGlowColorOverride(color);
            }
        }

        private void TextDisabled(FloatingText view)
        {
            view.gameObject.SetActive(false);
            m_Pool.Enqueue(view);
        }

        public FloatingText GetText()
        {
            if (m_Pool.Count == 0)
                CreateText();

            FloatingText text = m_Pool.Dequeue();
            text.transform.position = Vector3.zero;

            return text;
        }
    }

}
