#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ABS
{
	public class AssetHelper
    {
        public static T FindAsset<T>(string folderName, string assetName) where T : class
        {
#if UNITY_EDITOR
            string[] assetGuids = AssetDatabase.FindAssets(assetName);
            foreach (string guid in assetGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains(EngineGlobal.PROJECT_PATH_NAME + "/" + folderName + "/"))
                {
                    T foundAsset = AssetDatabase.LoadAssetAtPath(path, typeof(T)) as T;
                    if (foundAsset != null)
                        return foundAsset;
                }
            }
#endif

            return default(T);
        }
    }
}
