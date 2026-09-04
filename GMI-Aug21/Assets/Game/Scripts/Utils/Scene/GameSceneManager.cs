using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Oxtail.Utils
{
    public static class GameSceneManager
    {
        public enum SceneLoadMode
        {
            Sync,
            Async
        }

        public static void LoadScene(string sceneName, string loadingScene = "")
        {
            if (string.IsNullOrEmpty(loadingScene))
                LoadSceneSync(sceneName);
            else
                LoadSceneAsync(sceneName, loadingScene);
        }

        private static void LoadSceneSync(string scene)
        {
            Scene previousScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene);
        }

        private static async Task LoadSceneAsync(string scene, string loadingScene)
        {
            Scene previousScene = SceneManager.GetActiveScene();

            AsyncOperation loadingSceneOp = SceneManager.LoadSceneAsync(loadingScene);
            loadingSceneOp.allowSceneActivation = true;

            while (!loadingSceneOp.isDone)
                await Task.Yield();

            AsyncOperation op = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);
            op.allowSceneActivation = false;

            while (!op.isDone)
            {
                if (op.progress >= 0.9f)
                {
                    await Task.Delay(2000);
                    op.allowSceneActivation = true;
                }
                else
                    await Task.Yield();
            }

            SceneManager.UnloadSceneAsync(loadingScene);

            if (previousScene.IsValid())
            {
                AsyncOperation previousOp = SceneManager.UnloadSceneAsync(previousScene);
                while (!previousOp.isDone)
                    await Task.Yield();
            }
        }
    }
}
