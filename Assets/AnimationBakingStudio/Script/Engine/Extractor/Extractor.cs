using System;
using System.Reflection;
using UnityEngine;

namespace ABS
{
    public abstract class Extractor : MonoBehaviour
    {
        public enum AlpahExtractionChannel
        {
            Red,
            Green,
            Blue,
            Mixed
        }

        public enum ColorExtractionBackground
        {
            Black,
            White
        }

        public abstract void Extract(Camera camera, Model model, ref Texture2D resultTexture);

        protected float ExtractAlpha(Color pixelOnBlack, Color pixelOnWhite, AlpahExtractionChannel channel)
        {
            float colorDiff = 0f;

            switch (channel)
            {
                case AlpahExtractionChannel.Red:
                    colorDiff = pixelOnWhite.r - pixelOnBlack.r;
                    break;

                case AlpahExtractionChannel.Green:
                    colorDiff = pixelOnWhite.g - pixelOnBlack.g;
                    break;

                case AlpahExtractionChannel.Blue:
                    colorDiff = pixelOnWhite.b - pixelOnBlack.b;
                    break;

                case AlpahExtractionChannel.Mixed:
                    float redDiff = pixelOnWhite.r - pixelOnBlack.r;
                    float greenDiff = pixelOnWhite.g - pixelOnBlack.g;
                    float blueDiff = pixelOnWhite.b - pixelOnBlack.b;
                    colorDiff = Mathf.Min(Mathf.Min(redDiff, greenDiff), blueDiff);
                    break;
            }

            return Mathf.Clamp01(colorDiff);
        }

        public static Color[] ReadCameraPixels(Camera camera, Color color)
        {
            camera.backgroundColor = color;
            SetHdrpCameraBackgroundColor(camera, color);
            camera.Render();
            Texture2D tex = new Texture2D(camera.targetTexture.width, camera.targetTexture.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, camera.targetTexture.width, camera.targetTexture.height), 0, 0);
            return tex.GetPixels();
        }

        public static Color32[] ReadCameraPixels32(Camera camera, Color color)
        {
            camera.backgroundColor = color;
            SetHdrpCameraBackgroundColor(camera, color);
            camera.Render();
            Texture2D tex = new Texture2D(camera.targetTexture.width, camera.targetTexture.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, camera.targetTexture.width, camera.targetTexture.height), 0, 0);
            return tex.GetPixels32();
        }

        public static void SetHdrpCameraBackgroundColor(Camera camera, Color color)
        {
            Type hdrpCameraType = GetHdrpCameraType();
            if (hdrpCameraType == null)
                return;

            Component hdrpCameraComponent = camera.gameObject.GetComponent(hdrpCameraType);
            FieldInfo hdrpCameraBackgroundColorHdrField = hdrpCameraType.GetField("backgroundColorHDR");
            if (hdrpCameraComponent != null && hdrpCameraBackgroundColorHdrField != null)
                hdrpCameraBackgroundColorHdrField.SetValue(hdrpCameraComponent, color);
        }

        public static Type GetHdrpCameraType()
        {
            if (Camera.main == null)
                return null;

            Type hdrpCameraTypeA = Type.GetType(
                "UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData, " +
                "Unity.RenderPipelines.HighDefinition.Runtime",
                false, true
            );
            Type hdrpCameraTypeB = Type.GetType(
                "UnityEngine.Experimental.Rendering.HDPipeline.HDAdditionalCameraData, " +
                "Unity.RenderPipelines.HighDefinition.Runtime",
                false, true
            );

            return hdrpCameraTypeA ?? hdrpCameraTypeB;
        }
    }
}
