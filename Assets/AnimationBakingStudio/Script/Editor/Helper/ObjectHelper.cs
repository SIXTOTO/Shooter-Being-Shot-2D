using UnityEngine;

namespace ABS
{
    public class ObjectHelper : MonoBehaviour
    {
        public static GameObject GetOrCreateObject(string assetName, string categoryName, Vector3 position, Transform parent = null)
        {
            GameObject obj = GameObject.Find(assetName);
            if (obj == null)
            {
                GameObject prefab = AssetHelper.FindAsset<GameObject>(categoryName, assetName);

                if (prefab != null)
                {
                    obj = GameObject.Instantiate(prefab, position, Quaternion.identity);
                    obj.name = assetName;

                    if (parent != null)
                    {
                        obj.transform.parent = parent;
                        obj.transform.localRotation = Quaternion.identity;
                    }
                }
            }

            return obj;
        }

        public static void DeleteObject(string name)
        {
            GameObject obj = GameObject.Find(name);
            if (obj != null)
                GameObject.DestroyImmediate(obj);
        }

        public static void DeleteObjectUnder(string name, Transform parent)
        {
            Transform child = parent.Find(name);
            if (child != null && child.gameObject != null)
                GameObject.DestroyImmediate(child.gameObject);
        }
    }
}
