using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Oxtail.Utils
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class BackgroundMove : MonoBehaviour
    {
        [SerializeField] private Vector2 m_Speed;

        private SpriteRenderer m_Renderer;
        private Material m_Material;

        private void Awake()
        {
            m_Renderer = GetComponent<SpriteRenderer>();
            m_Material = new Material(m_Renderer.material);
            m_Renderer.material = m_Material;
        }

        private void Update()
        {
            m_Material.mainTextureOffset += m_Speed * Time.deltaTime;
        }
    }
}

