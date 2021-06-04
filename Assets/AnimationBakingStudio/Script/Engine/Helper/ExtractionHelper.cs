using System.Linq;
using UnityEngine;

namespace ABS
{
    public class ExtractionHelper
    {
        private static ComputeShader shadowExtractionShader = null;
        private static ComputeShader ShadowExtractionShader
        {
            get
            {
                if (shadowExtractionShader == null)
                    shadowExtractionShader = AssetHelper.FindAsset<ComputeShader>("Shader/Compute", "ExtractShadow");
                return shadowExtractionShader;
            }
        }

        public static void ExtractInShadowCamera(Camera camera, ref Texture2D resultTexture)
        {
            Color[] pixelsOnBlack = Extractor.ReadCameraPixels(camera, Color.black);
            Color[] pixelsOnWhite = Extractor.ReadCameraPixels(camera, Color.white);

            int textureSize = resultTexture.width * resultTexture.height;
            Color[] resultPixels = Enumerable.Repeat(Color.clear, textureSize).ToArray();

            ComputeShader computeShader = ShadowExtractionShader;
            if (EngineGlobal.gpuUse && SystemInfo.supportsComputeShaders && computeShader != null && textureSize > EngineGlobal.GPU_THREAD_SIZE)
            {
                int kernelIndex = computeShader.FindKernel("ExtractShadowFunction");

                ComputeBuffer blackBuffer = new ComputeBuffer(textureSize, 16);
                blackBuffer.SetData(pixelsOnBlack);
                computeShader.SetBuffer(kernelIndex, "blackBuffer", blackBuffer);

                ComputeBuffer whiteBuffer = new ComputeBuffer(textureSize, 16);
                whiteBuffer.SetData(pixelsOnWhite);
                computeShader.SetBuffer(kernelIndex, "whiteBuffer", whiteBuffer);

                computeShader.SetInt("width", resultTexture.width);

                ComputeBuffer resultBuffer = new ComputeBuffer(textureSize, 16);
                computeShader.SetBuffer(kernelIndex, "resultBuffer", resultBuffer);

                computeShader.Dispatch(kernelIndex, EngineGlobal.GPU_THREAD_SIZE, 1, 1);
                resultBuffer.GetData(resultPixels);

                blackBuffer.Release();
                whiteBuffer.Release();
                resultBuffer.Release();
            }
            else
            {
                for (int i = 0; i < textureSize; ++i)
                {
                    Color pixelOnBlack = pixelsOnBlack[i];
                    Color pixelOnWhite = pixelsOnWhite[i];

                    Color pixel = Color.black;
                    pixel.a = 1f - (pixelOnWhite.r - pixelOnBlack.r);

                    resultPixels[i] = pixel;
                }
            }

            resultTexture.SetPixels(resultPixels);
            resultTexture.Apply();
        }

        private static ComputeShader opaqueExtractionShader = null;
        private static ComputeShader OpaqueExtractionShader
        {
            get
            {
                if (opaqueExtractionShader == null)
                    opaqueExtractionShader = AssetHelper.FindAsset<ComputeShader>("Shader/Compute", "OpaqueExtract");
                return opaqueExtractionShader;
            }
        }

        public static void ExtractOpqaue(ref Texture2D resultTexture, Color32 defaultColor, bool isNormalMap = false)
        {
            Color32[] pixelsOnBlack = Extractor.ReadCameraPixels32(Camera.main, Color.black);
            Color32[] pixelsOnWhite = Extractor.ReadCameraPixels32(Camera.main, Color.white);

            int textureSize = resultTexture.width * resultTexture.height;
            Color32[] resultPixels = Enumerable.Repeat(defaultColor, textureSize).ToArray();

            ComputeShader computeShader = OpaqueExtractionShader;
            if (EngineGlobal.gpuUse && SystemInfo.supportsComputeShaders && !isNormalMap &&
                computeShader != null && textureSize > EngineGlobal.GPU_THREAD_SIZE)
            {
                int kernelIndex = computeShader.FindKernel("OpaqueExtractFunction");

                ComputeBuffer blackBuffer = new ComputeBuffer(textureSize, 4);
                blackBuffer.SetData(pixelsOnBlack);
                computeShader.SetBuffer(kernelIndex, "blackBuffer", blackBuffer);

                ComputeBuffer whiteBuffer = new ComputeBuffer(textureSize, 4);
                whiteBuffer.SetData(pixelsOnWhite);
                computeShader.SetBuffer(kernelIndex, "whiteBuffer", whiteBuffer);

                computeShader.SetInt("width", resultTexture.width);

                ComputeBuffer resultBuffer = new ComputeBuffer(textureSize, 4);
                computeShader.SetBuffer(kernelIndex, "resultBuffer", resultBuffer);

                computeShader.Dispatch(kernelIndex, EngineGlobal.GPU_THREAD_SIZE, 1, 1);
                resultBuffer.GetData(resultPixels);

                blackBuffer.Release();
                whiteBuffer.Release();
                resultBuffer.Release();
            }
            else
            {
                for (int i = 0; i < textureSize; ++i)
                {
                    if (pixelsOnWhite[i].r == pixelsOnBlack[i].r)
                        resultPixels[i] = pixelsOnBlack[i];
                }
            }

            resultTexture.SetPixels32(resultPixels);
            resultTexture.Apply();
        }
    }
}
