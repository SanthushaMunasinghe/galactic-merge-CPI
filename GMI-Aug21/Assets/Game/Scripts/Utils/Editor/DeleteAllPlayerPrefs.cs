using UnityEditor;
using UnityEngine;

namespace Oxtail.Utils
{
    public class DeleteAllPlayerPrefs : Editor
    {
        [MenuItem("Game/Delete All PlayerPrefs")]
        static void DeletePlayerPrefs()
        {
            PlayerPrefs.DeleteAll();
        }
    }
}
