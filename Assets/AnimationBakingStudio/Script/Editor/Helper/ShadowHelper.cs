using System.IO;
using UnityEngine;
using UnityEditor;

namespace ABS
{
	public class ShadowHelper
    {
        public static void LocateShadowToModel(Model model, Studio studio)
        {
            if (model == null || studio.shadow.obj == null || studio.shadow.type == ShadowType.None)
                return;

            Vector3 modelBottom = model.ComputedBottom;
            modelBottom.y -= 0.01f;

            Transform shadowTransform = studio.shadow.obj.transform;

            if (studio.shadow.type == ShadowType.Simple)
            {
                if (shadowTransform.parent != model.transform)
                {
                    shadowTransform.parent = model.transform;
                    shadowTransform.localRotation = Quaternion.identity;
                }
                shadowTransform.position = modelBottom;
            }
            else
            {
                shadowTransform.position = modelBottom;
            }
        }

        public static void ScaleSimpleShadow(Model model, Studio studio)
        {
            if (model == null || studio.shadow.obj == null)
                return;

            Renderer shadowRenderer = studio.shadow.obj.GetComponent<Renderer>();
            if (!model.IsReady() || shadowRenderer == null)
                return;

            Vector2 shadowScale = model.isSpecificSimpleShadow ? model.simpleShadowScale : studio.shadow.simple.scale;
            if (shadowScale.magnitude == 0f)
                return;

            Transform shadowTransform = studio.shadow.obj.transform;
            shadowTransform.localScale = Vector3.one; // before using shadowRenderer.bounds

            Vector3 modelSize = model.GetDynamicSize();
            Debug.Assert(shadowRenderer.bounds.size.x == shadowRenderer.bounds.size.z);
            float shadowLength = shadowRenderer.bounds.size.x;
            float xScaleRatio = modelSize.x / shadowLength;
            float zScaleRatio = modelSize.z / shadowLength;

            if (xScaleRatio > 0f && zScaleRatio > 0f)
            {
                xScaleRatio *= shadowScale.x;
                zScaleRatio *= shadowScale.y;

                shadowTransform.localScale = new Vector3
                (
                    shadowTransform.localScale.x * xScaleRatio,
                    1.0f,
                    shadowTransform.localScale.z * zScaleRatio
                );
            }
            else
            {
                shadowTransform.localScale = new Vector3
                (
                    shadowScale.x,
                    1.0f,
                    shadowScale.y
                );
            }
        }

        public static void ScaleSimpleShadowDynamically(Vector3 modelBaseSize, Vector3 simpleShadowBaseScale, MeshModel meshModel, Studio studio)
        {
            Vector3 modelCurrentSize = meshModel.GetSize();

            float xScaleRatio = modelBaseSize.x / modelCurrentSize.x;
            float zScaleRatio = modelBaseSize.z / modelCurrentSize.z;
            Debug.Assert(xScaleRatio > 0f && zScaleRatio > 0f);

            Transform shadowTransform = studio.shadow.obj.transform;
            shadowTransform.localScale = simpleShadowBaseScale;

            shadowTransform.localScale = new Vector3
            (
                shadowTransform.localScale.x * xScaleRatio,
                1.0f,
                shadowTransform.localScale.z * zScaleRatio
            );
        }

        public static void GetCameraAndFieldObject(GameObject shadowObj, out Camera camera, out GameObject fieldObj)
        {
            camera = null;
            Transform cameraTransform = shadowObj.transform.Find("Camera");
            if (cameraTransform != null)
            {
                camera = cameraTransform.gameObject.GetComponent<Camera>();
                camera.orthographicSize = Camera.main.orthographicSize;
            }

            fieldObj = null;
            Transform fieldTransform = shadowObj.transform.Find("Field");
            if (fieldTransform != null)
                fieldObj = fieldTransform.gameObject;
        }

        public static void BakeStaticShadow(Camera camera, GameObject fieldObj, Model model, Studio studio)
        {
            if (model == null)
                return;

            camera.CopyFrom(Camera.main);
            camera.targetDisplay = 1;

            camera.targetTexture = new RenderTexture(camera.pixelWidth, camera.pixelHeight, 24, RenderTextureFormat.ARGB32);

            string dirPath = Path.Combine(Application.dataPath, EngineGlobal.PROJECT_PATH_NAME + "/Shadow");
            int assetRootIndex = dirPath.IndexOf("Assets");
            if (assetRootIndex < 0)
            {
                Debug.LogError(string.Format("{0} is out of Assets folder.", dirPath));
                return;
            }
            dirPath = dirPath.Substring(assetRootIndex);

            camera.transform.localPosition = new Vector3(0, 500, 0);
            camera.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            CameraHelper.LookAtModel(camera.transform, model);

            fieldObj.SetActive(false);
            Texture2D rawTex = CapturingHelper.CaptureModelInCamera(model, camera, studio, true);
            fieldObj.SetActive(true);

            string filePath = TextureHelper.SaveTexture(dirPath, EditorGlobal.STATIC_SHADOW_NAME, rawTex);
            AssetDatabase.ImportAsset(filePath);

            fieldObj.transform.localPosition = Vector3.zero;
            fieldObj.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            ScaleShadowField(camera, fieldObj);

            camera.targetTexture = null;
            camera.targetDisplay = 2;
        }

        public static void ScaleShadowField(Camera camera, GameObject fieldObj)
        {
            Renderer renderer = fieldObj.GetComponent<Renderer>();
            if (renderer == null)
                return;

            Vector3 maxWorldPos = camera.ScreenToWorldPoint(new Vector3(camera.pixelWidth, camera.pixelHeight));
            Vector3 minWorldPos = camera.ScreenToWorldPoint(Vector3.zero);
            Vector3 texWorldSize = maxWorldPos - minWorldPos;

            fieldObj.transform.localScale = Vector3.one; // important
            fieldObj.transform.localScale = new Vector3
            (
                texWorldSize.x / renderer.bounds.size.x,
                1f,
                texWorldSize.z / renderer.bounds.size.z
            );
        }

        public static void ScaleMatteField(MeshModel model, GameObject fieldObj, LightProperty lit)
        {
            if (model == null || model.mainRenderer == null || fieldObj == null || lit == null)
                return;

            Renderer fieldRenderer = fieldObj.GetComponent<Renderer>();
            if (fieldRenderer == null)
                return;

            float tan = Mathf.Tan(lit.com.transform.rotation.eulerAngles.x * Mathf.Deg2Rad);
            Vector3 modelSize = model.mainRenderer.bounds.size;
            float modelHalfWidth = Mathf.Max(modelSize.x, modelSize.z) / 2;
            float fieldWidth = (modelSize.y / tan + modelHalfWidth) * 2;

            fieldObj.transform.localScale = Vector3.one; // important
            fieldObj.transform.localScale = new Vector3
            (
                fieldWidth / fieldRenderer.bounds.size.x,
                1f,
                fieldWidth / fieldRenderer.bounds.size.z
            );
        }
    }
}
