using System;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    [Serializable]
    public class UIParticleTypeConfig
    {
        public UIParticleType Type;
        public RectTransform SpawnPoint;
        public Color Color = Color.white;
        public string Prefix;
        public string Suffix;
    }
}
