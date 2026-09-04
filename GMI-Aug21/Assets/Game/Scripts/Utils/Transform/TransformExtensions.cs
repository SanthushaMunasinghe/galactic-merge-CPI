using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Oxtail.Utils
{
    public static class TransformExtensions
    {
        public static Transform GetChildByName(this Transform parent, string childName)
        {
            Transform[] childs = parent.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in childs)
            {
                if (child.name == childName)
                    return child;
            }

            return null;
        }

        public static Transform GetChildByTag(this Transform parent, string tag)
        {
            Transform[] childs = parent.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in childs)
            {
                if (child.CompareTag(tag))
                    return child;
            }

            return null;
        }

        public static List<Transform> GetChildsByTag(this Transform parent, string tag)
        {
            List<Transform> returnChilds = new();

            Transform[] childs = parent.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in childs)
            {
                if (child.CompareTag(tag))
                    returnChilds.Add(child);
            }

            return returnChilds;
        }

        public static bool IsLookingRight2D(this Transform self)
        {
            return self.localScale.x > 0;
        }

        public static bool IsLookingWorldRight2D(this Transform self)
        {
            return self.lossyScale.x > 0;
        }

        public static void LookAt2D(this Transform self, Vector3 worldPosition, float initalRotation = 0f)
        {
            worldPosition.z = self.position.z;
            self.right = worldPosition - self.position;
            //self.eulerAngles += new Vector3(0f, 0f, -90);
            //Debug.DrawLine(worldPosition, self.right);
            //Vector3 direction = worldPosition - self.position;
            //float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            //self.rotation = Quaternion.Euler(new Vector3(0, 0, angle + initalRotation - 90f));
        }

        public static void DestroyAllChilds(this Transform self)
        {
            for (int i = self.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(self.transform.GetChild(i).gameObject);
            }
        }

        public static void DestroyAllChildsImmediate(this Transform self)
        {
            for (int i = self.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(self.transform.GetChild(i).gameObject);
            }
        }

        public static void FlipHorizontal(this Transform self)
        {
            self.localScale = Vector3.Scale(self.localScale, new Vector3(-1f, 1f, 1f));
        }

        public static void FlipToTarget(this Transform self, Transform other)
        {
            if (self.IsLookingRight2D() && other.transform.position.x < self.transform.position.x
                || !self.IsLookingRight2D() && other.transform.position.x > self.transform.position.x)
                FlipHorizontal(self);
        }
    }
}
