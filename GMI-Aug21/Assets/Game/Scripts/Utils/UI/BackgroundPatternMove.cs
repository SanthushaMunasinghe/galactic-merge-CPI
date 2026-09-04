using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Oxtail.Utils
{
    [RequireComponent(typeof(RawImage))]
    public class BackgroundPatternMove : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Vector2 m_RepeatCount;
        [SerializeField] private Vector2 m_Scroll;
        [SerializeField] private Vector2 m_Offset;

        private RectTransform m_RectTransform;
        private RectTransform m_ParentRectTransform;
        private RawImage m_Image;

        private void Awake()
        {
            m_Image = GetComponent<RawImage>();
            m_Image.uvRect = new Rect(m_Offset, m_RepeatCount);
        }

        private void Start()
        {
            m_RectTransform = GetComponent<RectTransform>();
            m_ParentRectTransform = transform.parent.GetComponent<RectTransform>();

            SetScale();
        }

        private void Update()
        {
#if UNITY_EDITOR
            SetScale();
#endif
            m_Offset += m_Scroll * Time.deltaTime;
            m_Image.uvRect = new Rect(m_Offset, m_RepeatCount);
        }

        private void SetScale()
        {
            var parentCorners = new Vector3[4];
            m_ParentRectTransform.GetLocalCorners(parentCorners);
            var diagonal = Vector3.Distance(parentCorners[0], parentCorners[2]);

            m_RectTransform.sizeDelta = new Vector2(diagonal, diagonal);
        }
    }
}
