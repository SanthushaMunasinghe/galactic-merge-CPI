using DG.Tweening;
using UnityEngine;

namespace Oxtail.MobileTemplate
{
    public class FingerTapTutorial : MonoBehaviour
    {
        [SerializeField] private GameObject m_Finger;

        private Canvas m_Canvas;

        private void Awake()
        {
            m_Canvas = GetComponentInParent<Canvas>();
            m_Finger.SetActive(false);
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Vector2 pos;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    m_Canvas.transform as RectTransform,
                    Input.mousePosition,
                    m_Canvas.worldCamera,
                    out pos
                );

                transform.localPosition = pos;
                m_Finger.SetActive(true);
                transform.DOKill();
                Sequence seq = DOTween.Sequence();
                seq.Append(m_Finger.transform.DOScale(Vector3.one * 0.5f, 0.25f))
                    .Append(m_Finger.transform.DOScale(Vector3.one, 0.25f))
                    .OnComplete(()=>
                    {
                        m_Finger.SetActive(false);
                    });
            }
        }
    }
}
