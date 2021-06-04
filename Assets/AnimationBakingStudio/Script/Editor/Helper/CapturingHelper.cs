using UnityEngine;

namespace ABS
{
    public class CapturingHelper
    {
        public static Texture2D CaptureModelManagingShadow(Model model, Studio studio)
        {
            Texture2D resultTexture;

            if (studio.shadow.type != ShadowType.None)
            {
                if (studio.shadow.shadowOnly)
                {
                    PickOutSimpleShadow(studio);
                    {
                        Vector3 originalPosition = ThrowOutModelFarAway(model);
                        {
                            resultTexture = CaptureModelInCamera(model, Camera.main, studio);
                        }
                        PutModelBackInPlace(model, originalPosition);
                    }
                    PushInSimpleShadow(model, studio);
                }
                else if (!studio.shadow.shadowOnly && (studio.variation.on && studio.variation.excludeShadow))
                {
                    PickOutSimpleShadow(studio);
                    {
                        // Shadow Pass
                        Vector3 originalPosition = ThrowOutModelFarAway(model);
                        Texture2D shadowTexture = CaptureModelInCamera(model, Camera.main, studio, true);
                        PutModelBackInPlace(model, originalPosition);

                        // Model Pass
                        studio.shadow.obj.SetActive(false);
                        Texture2D modelTexture = CaptureModelInCamera(model, Camera.main, studio);
                        studio.shadow.obj.SetActive(true);

                        // merge texture
                        resultTexture = MergeShadowAndBody(studio, shadowTexture, modelTexture);
                    }
                    PushInSimpleShadow(model, studio);
                }
                else
                {
                    resultTexture = CaptureModelInCamera(model, Camera.main, studio);
                }
            }
            else
            {
                resultTexture = CaptureModelInCamera(model, Camera.main, studio);
            }

            return resultTexture;
        }

        private static void PickOutSimpleShadow(Studio studio)
        {
            if (studio.shadow.type == ShadowType.Simple)
                studio.shadow.obj.transform.parent = null;
        }

        private static void PushInSimpleShadow(Model model, Studio studio)
        {
            if (studio.shadow.type == ShadowType.Simple)
                studio.shadow.obj.transform.parent = model.transform;
        }

        private static Vector3 ThrowOutModelFarAway(Model model)
        {
            Vector3 originalPosition = model.transform.position;
            model.transform.position = CreateFarAwayPosition();
            return originalPosition;
        }

        private static void PutModelBackInPlace(Model model, Vector3 originalPosition)
        {
            model.transform.position = originalPosition;
        }

        private static Vector3 CreateFarAwayPosition()
        {
            return new Vector3(10000f, 0f, 0f);
        }

        public static Texture2D CaptureModelInCamera(Model model, Camera camera, Studio studio, bool isShadow = false)
        {
            if (camera == null || studio.extraction.com == null)
                return Texture2D.whiteTexture;

            RenderTexture.active = camera.targetTexture;

            Texture2D resultTexure = new Texture2D(camera.targetTexture.width, camera.targetTexture.height, TextureFormat.ARGB32, false);

            if (isShadow)
                ExtractionHelper.ExtractInShadowCamera(camera, ref resultTexure);
            else
                studio.extraction.com.Extract(camera, model, ref resultTexure);

            if (studio.variation.on && !isShadow)
                ApplyVariation(studio.variation, ref resultTexure);

            RenderTexture.active = null;

            return resultTexure;
        }

        private static void ApplyVariation(VariationProperty variation, ref Texture2D outTex)
        {
            Color[] outPixels = outTex.GetPixels();

            for (int i = 0; i < outTex.width * outTex.height; i++)
            {
                Color pixel = outPixels[i];
                if (pixel.a <= 0f)
                    continue;

                Color outPixel = BlendingHelper.BlendTwoColors(variation.color, pixel,
                    variation.colorBlendFactor, variation.imageBlendFactor);

                outPixels[i] = outPixel;
            }

            outTex.SetPixels(outPixels);
            outTex.Apply();
        }

        private static Texture2D MergeShadowAndBody(Studio studio, Texture2D shadowTexture, Texture2D bodyTexture)
        {
            if (shadowTexture.width != bodyTexture.width || shadowTexture.height != bodyTexture.height)
            {
                Debug.LogError("shadowTexture.width != bodyTexture.width || shadowTexture.height != bodyTexture.height");
                return bodyTexture;
            }

            Color[] bodyPixels = bodyTexture.GetPixels();
            Color[] shadowPixels = shadowTexture.GetPixels();

            for (int i = 0; i < shadowTexture.width * shadowTexture.height; i++)
            {
                Color bodyPixel = bodyPixels[i];
                if (bodyPixel == Color.clear)
                    continue;

                Color shadowPixel = shadowPixels[i];
                Color resultPixel = (shadowPixel == Color.clear ? bodyPixel :
                    BlendingHelper.BlendTwoColors(bodyPixel, shadowPixel, BlendFactor.One, BlendFactor.OneMinusSrcAlpha));

                shadowPixels[i] = resultPixel;
            }

            shadowTexture.SetPixels(shadowPixels);
            shadowTexture.Apply();

            return shadowTexture;
        }

        private static Shader normalMapShader = null;
        private static Shader NormalMapShader
        {
            get
            {
                if (normalMapShader == null)
                    normalMapShader = AssetHelper.FindAsset<Shader>("Shader", "NormalMap");
                return normalMapShader;
            }
        }

        private static Shader grayscaleMapShader = null;
        private static Shader GrayscaleMapShader
        {
            get
            {
                if (grayscaleMapShader == null)
                    grayscaleMapShader = AssetHelper.FindAsset<Shader>("Shader", "GrayscaleMap");
                return grayscaleMapShader;
            }
        }

        public static Texture2D CaptureModelForNormalMap(Model model, bool isGrayscale, GameObject shadowObj)
        {
            if (Camera.main == null)
                return Texture2D.whiteTexture;

            model.BackupAllShaders();
            model.ChangeAllShaders(isGrayscale ? GrayscaleMapShader : NormalMapShader);

            if (shadowObj != null)
                shadowObj.SetActive(false);

            RenderTexture.active = Camera.main.targetTexture;

            Texture2D resultTexture = new Texture2D(Camera.main.targetTexture.width, Camera.main.targetTexture.height, TextureFormat.ARGB32, false);

            Color32 defaultColor = (isGrayscale ? EngineGlobal.CLEAR_COLOR32 : EngineGlobal.NORMALMAP_COLOR32);
            ExtractionHelper.ExtractOpqaue(ref resultTexture, defaultColor, true);

            RenderTexture.active = null;

            if (shadowObj != null)
                shadowObj.SetActive(true);

            model.RestoreAllShaders();

            return resultTexture;
        }
    }
}
