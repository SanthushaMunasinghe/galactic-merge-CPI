using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Oxtail.Utils
{
    public static class ComponentExtensions
    {
        public static RectTransform RectTransform (this Component component)
        {
            if (component.transform is RectTransform rectTransform)
                return rectTransform;

            return null;
        }
    }
}
