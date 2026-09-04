using DG.Tweening;
using Oxtail.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Oxtail.SpaceshipIncremental
{
    public class ExpandButtonsView : MonoBehaviour
    {
        [SerializeField] private Button m_ExpandButton;
        [SerializeField] private Image m_CrossImage;
        [SerializeField] private GameObject m_NotifyIcon;

        [Header("Expand")]
        [SerializeField] private GameObject m_ExpandSection;

        [Header("Notify Icon Tag")]
        [SerializeField, TagSelector] private string m_NotifyIconTag;

        private List<Transform> m_ChildNotifyIcons;

        private bool m_Expanded;

        private void Awake()
        {
            if (SaveLoadManager.Instance.GetMaxLevelUnlockedIndex() == 0 && 
                !SaveLoadManager.Instance.GetPrestigeUnlocked())
            {
                m_ExpandSection.SetActive(false);
                gameObject.SetActive(false);
                return;
            }

            m_ExpandButton.onClick.AddListener(()=> ToggleExpand());
            m_ExpandSection.SetActive(m_Expanded);

            m_ChildNotifyIcons = m_ExpandSection.transform.GetChildsByTag(m_NotifyIconTag);
            m_NotifyIcon.SetActive(false);

            StartCoroutine(CheckShowNotify());
        }

        private void OnDestroy()
        {
            m_CrossImage.transform.DOKill();
        }

        private IEnumerator CheckShowNotify()
        {
            while (true)
            {
                yield return new WaitForSeconds(1f);
                
                foreach (var notifyIcon in m_ChildNotifyIcons)
                {
                    if (!m_Expanded && 
                        !m_NotifyIcon.activeInHierarchy && 
                        notifyIcon.parent.gameObject.activeSelf &&
                        notifyIcon.gameObject.activeSelf)
                    {
                        m_NotifyIcon.SetActive(true);
                    }
                }
            }
        }

        private void ToggleExpand()
        {
            m_Expanded = !m_Expanded;

            m_ExpandSection.SetActive(m_Expanded);

            m_ExpandButton.interactable = false;
            m_CrossImage.transform.DOLocalRotate(new Vector3(0f, 0f, m_Expanded ? 180f : 0f), 0.25f)
                .OnComplete(()=>
                {
                    m_ExpandButton.interactable = true;
                });

            m_NotifyIcon.SetActive(false);
        }

        public void ShowNotify(bool show)
        {
            m_NotifyIcon.SetActive(show);
        }
    }
}
