using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    /// <summary>
    /// An Animation Event can only call a public method on a component attached to the same GameObject as
    /// the Animator — but Asteroid.OnBossAttackLanded lives one level up, on the boss's root GameObject,
    /// since the Animator sits on its sprite child instead. This just forwards the call up to it. Add it to
    /// whichever child GameObject carries the boss's Animator (the one wired to Asteroid's Boss Animator
    /// field), and wire the Attack clip's Animation Event to this component's OnBossAttackLanded instead of
    /// calling Asteroid's directly.
    /// </summary>
    public class BossAttackRelay : MonoBehaviour
    {
        private Asteroid m_Owner;

        private void Awake()
        {
            m_Owner = GetComponentInParent<Asteroid>();
        }

        public void OnBossAttackLanded()
        {
            m_Owner.OnBossAttackLanded();
        }
    }
}
