using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Oxtail.Utils
{
    [CreateAssetMenu(fileName = "SO_GameScenes", menuName = "Oxtail/Game/Game Scenes")]
    public class GameScenesSO : ScriptableObjectSingleton<GameScenesSO>
    {
        [Header("Scenes")]
        [SerializeField, Scene] private string m_LobbyScene;
        [SerializeField, Scene] private string m_TutorialScene;
        [SerializeField, Scene] private string m_GameScene;

        public string LobbyScene => m_LobbyScene;
        public string TutorialScene => m_TutorialScene;
        public string GameScene => m_GameScene;
    }
}
