using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    /// <summary>
    /// Entry points gameplay code calls to trigger a UI particle popup. Centralizes the
    /// "only in the CPI scene" guard so CPIManager and the upgrade buttons (shared with
    /// every other level manager) don't need to know the spawner even exists.
    /// </summary>
    public static class UIParticleActions
    {
        public static void PlayHpGained(float amount, RectTransform spawnPoint) => Spawn(UIParticleType.HPGain, spawnPoint, amount);

        public static void PlayHpLost(float amount, RectTransform spawnPoint) => Spawn(UIParticleType.HPLoss, spawnPoint, amount);

        public static void PlayShipAdded() => Spawn(UIParticleType.ShipAdded);

        public static void PlayMerge() => Spawn(UIParticleType.Merge);

        public static void PlayLineAdded() => Spawn(UIParticleType.LineAdded);

        private static void Spawn(UIParticleType type, float? value = null) => Spawn(type, null, value);

        private static void Spawn(UIParticleType type, RectTransform spawnPointOverride, float? value = null)
        {
            if (CPIManager.Instance == null || UIParticleSpawner.Instance == null)
                return;

            UIParticleSpawner.Instance.SpawnText(type, value, spawnPointOverride);
        }
    }
}
