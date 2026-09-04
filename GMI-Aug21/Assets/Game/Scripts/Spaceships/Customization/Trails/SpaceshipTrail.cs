using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    public abstract class SpaceshipTrail : MonoBehaviour
    {
        protected bool m_OverrideColor;

        public abstract void Init(bool overrideColor);
        public abstract void ResetTrail();
        public abstract void SetColor(Color color);
        public abstract void SetSortingLayer(int layer);
        public abstract void SetSortingOrder(int order);
    }
}
