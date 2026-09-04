using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Oxtail.Utils
{
    [RequireComponent(typeof(RawImage))]
    public class UIBackgroundMove : MonoBehaviour
    {
        [SerializeField] private Vector2 m_Speed;

        private RawImage m_Image;

        private void Awake()
        {
            m_Image = GetComponent<RawImage>();
        }

        private void Update()
        {
            m_Image.uvRect = new Rect(m_Image.uvRect.position + new Vector2(m_Speed.x, m_Speed.y) * Time.deltaTime, m_Image.uvRect.size);
        }
    }
}
