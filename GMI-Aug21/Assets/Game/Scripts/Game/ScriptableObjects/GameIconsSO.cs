using Oxtail.Utils;
using System.Linq;
using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    public enum CurrencyType
    {
        Coins,
        Gems,
        Celestium
    }

    [CreateAssetMenu(fileName = "GameIcons", menuName = "Incremental/Game/Game Icons")]
    public class GameIconsSO : ScriptableObjectSingleton<GameIconsSO>
    {
        [System.Serializable]
        private struct GameCurrencies
        {
            public CurrencyType Currency;
            public Sprite Icon;
        }

        [Header("Currency")]
        [SerializeField] private GameCurrencies[] m_Currencies;

        public Sprite GetCurrencyIcon(CurrencyType currency)
        {
            return m_Currencies.FirstOrDefault(x => x.Currency == currency).Icon;
        }
    }
}
