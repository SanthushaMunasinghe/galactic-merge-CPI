using Oxtail.Utils;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    [CreateAssetMenu(fileName = "GamePowerUps", menuName = "Incremental/Game/Game Power Ups")]
    public class GamePowerUpsSO : ScriptableObjectSingleton<GamePowerUpsSO>
    {
        [SerializeField] private PowerUpSO[] m_PowerUps;

        public PowerUpSO[] PowerUps => m_PowerUps;

        public PowerUpSO GetPowerUpByID(string id)
        {
            foreach (var powerUp in m_PowerUps)
            {
                if (powerUp.PowerUpID == id)
                    return powerUp;
            }

            return null;
        }

        public PowerUpSO GetPowerUpByType(PowerUpType type)
        {
                foreach (var powerUp in m_PowerUps)
                {
                    if (powerUp.PowerUpType == type)
                        return powerUp;
                }
    
                return null;
        }
    }
}
