using Oxtail.Utils;
using Shapes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    public class ChangeSpaceshipSorting : MonoBehaviour
    {
        [SerializeField, TagSelector] private string m_SpaceshipTag;
        [SerializeField, SortingLayer] private string m_NewSortingLayer;
        [SerializeField] private int m_NewSortingOrder;

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!collision.CompareTag(m_SpaceshipTag))
                return;

            Spaceship spaceship = collision.GetComponent<Spaceship>();
            spaceship.SetSortingLayer(SortingLayer.NameToID(m_NewSortingLayer));
            spaceship.SetSortingOrder(m_NewSortingOrder);
        }
    }
}
