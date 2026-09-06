using UnityEngine;
using UnityEngine.UI;

namespace Oxtail.SpaceshipIncremental
{
    /// <summary>
    /// Reserved prefab type for a future sprite-based UI particle. Not spawned by
    /// UIParticleSpawner yet: text particles are the only kind in use for now.
    /// </summary>
    public class SpriteUIParticle : MonoBehaviour
    {
        [SerializeField] private Image m_Image;
    }
}
