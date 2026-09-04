using Oxtail.Utils;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    public interface ICombatTarget
    {
        public bool IsDead { get; }

        public void SetHealth(BigNumber health);
        public BigNumber TakeDamage(BigNumber damage);
        public void SetDead();
    }
}
