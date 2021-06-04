using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ABS
{
	public class ModelMaker
	{
        [MenuItem("Assets/" + EngineGlobal.PROJECT_NAME + "/Make Models/Mesh Model")]
        private static void MakeRendererModels()
        {
            GameObject groupObject = null;
            List<MeshModel> models = null;

            string[] selectedPathes = GetSelectedPathes();

            string[] allAssetPaths = AssetDatabase.GetAllAssetPaths();
            foreach (string assetPath in allAssetPaths)
            {
                if (!IsInAnyPathes(assetPath, selectedPathes))
                    continue;

                GameObject prefab = AssetDatabase.LoadAssetAtPath(assetPath, typeof(GameObject)) as GameObject;
                if (prefab == null)
                    continue;

                Renderer renderer = prefab.GetComponentInChildren<Renderer>();
                if (renderer == null)
                    continue;

                if (groupObject == null)
                {
                    groupObject = new GameObject("Mesh Model Group");
                    models = new List<MeshModel>();
                }

                GameObject obj = GameObject.Instantiate(prefab, Vector3.zero, Quaternion.identity, groupObject.transform);
                obj.name = prefab.name;

                MeshModel model = obj.GetComponent<MeshModel>();
                if (model == null)
                {
                    model = obj.AddComponent<MeshModel>();

                    if (model.mainRenderer is SkinnedMeshRenderer)
                        model.pivotType = PivotType.Bottom;
                    else
                        model.pivotType = PivotType.Center;
                }

                if (model.mainRenderer == null)
                    model.mainRenderer = MeshModel.FindBiggestRenderer(obj);

                models.Add(model);
            }

            if (models != null)
            {
                foreach (MeshModel model in models)
                {
                    float rangeX = model.mainRenderer.bounds.size.x * models.Count;
                    float rangeZ = model.mainRenderer.bounds.size.z * models.Count;
                    model.transform.position = new Vector3(Random.Range(-rangeX, rangeX), 0, Random.Range(-rangeZ, rangeZ));
                }
            }
        }

        [MenuItem("Assets/" + EngineGlobal.PROJECT_NAME + "/Make Models/Particle Model")]
        private static void MakeParticleModels()
        {
            GameObject groupObject = null;
            List<ParticleModel> models = null;

            string[] selectedPathes = GetSelectedPathes();

            string[] allAssetPaths = AssetDatabase.GetAllAssetPaths();
            foreach (string assetPath in allAssetPaths)
            {
                if (!IsInAnyPathes(assetPath, selectedPathes))
                    continue;

                GameObject prefab = AssetDatabase.LoadAssetAtPath(assetPath, typeof(GameObject)) as GameObject;
                if (prefab == null)
                    continue;

                ParticleSystem particleSystem = prefab.GetComponentInChildren<ParticleSystem>();
                if (particleSystem == null)
                    continue;

                if (groupObject == null)
                {
                    groupObject = new GameObject("Particle Model Group");
                    models = new List<ParticleModel>();
                }

                GameObject obj = GameObject.Instantiate(prefab, Vector3.zero, Quaternion.identity, groupObject.transform);
                obj.name = prefab.name;

                ParticleModel model = obj.GetComponent<ParticleModel>();
                if (model == null)
                    model = obj.AddComponent<ParticleModel>();

                if (model.TrySetMainParticleSystem())
                    model.CheckModel();

                models.Add(model);
            }

            if (models != null)
            {
                const float PARTICLE_LENGTH = 10;
                foreach (ParticleModel model in models)
                {
                    float rangeX = PARTICLE_LENGTH * models.Count;
                    float rangeZ = PARTICLE_LENGTH * models.Count;
                    model.transform.position = new Vector3(Random.Range(-rangeX, rangeX), 0, Random.Range(-rangeZ, rangeZ));
                }
            }
        }

        private static string[] GetSelectedPathes()
        {
            string[] pathes = new string[Selection.assetGUIDs.Length];
            for (int i = 0; i < Selection.assetGUIDs.Length; ++i)
                pathes[i] = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[i]);
            return pathes;
        }

        private static bool IsInAnyPathes(string targetPath, string[] pathes)
        {
            foreach (string path in pathes)
            {
                if (targetPath.IndexOf(path) >= 0)
                    return true;
            }
            return false;
        }
    }
}