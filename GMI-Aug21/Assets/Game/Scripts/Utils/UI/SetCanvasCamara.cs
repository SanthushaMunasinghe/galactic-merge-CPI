using UnityEngine;

namespace Oxtail.Utils
{
    [RequireComponent(typeof(Canvas))]
    public class SetCanvasCamara : MonoBehaviour
    {
        private void Awake()
        {
            GetComponent<Canvas>().worldCamera = Camera.main;
        }
    }
}
