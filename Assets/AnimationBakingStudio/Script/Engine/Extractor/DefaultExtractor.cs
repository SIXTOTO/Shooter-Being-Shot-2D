using System.Linq;
using UnityEngine;

namespace ABS
{
    public class DefaultExtractor : Extractor
    {
        [SerializeField]
        private ComputeShader computeShader;

        [SerializeField, Range(0, 1)]
        private float alphaThreshold = 0.0f;

        public override void Extract(Camera camera, Model model, ref Texture2D resultTexture)
        {
            Color[] pixelsOnBlack = ReadCameraPixels(camera, Color.black);
            Color[] pixelsOnWhite = ReadCameraPixels(camera, Color.white);

            int textureSize = resultTexture.width * resultTexture.height;
            Color[] resultPixels = Enumerable.Repeat(Color.clear, textureSize).ToArray();

            if (EngineGlobal.gpuUse && SystemInfo.supportsComputeShaders && computeShader != null &&
                textureSize > EngineGlobal.GPU_THREAD_SIZE && alphaThreshold == 0)
            {
                int kernelIndex = computeShader.FindKernel("DefaultExtractFunction");

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

                    float redDiff = pixelOnWhite.r - pixelOnBlack.r;
                    float greenDiff = pixelOnWhite.g - pixelOnBlack.g;
                    float blueDiff = pixelOnWhite.b - pixelOnBlack.b;

                    float alpha = 1f - Mathf.Min(Mathf.Min(redDiff, greenDiff), blueDiff);
                    if (alpha <= alphaThreshold)
                        continue;

                    Color pixel = pixelOnBlack / alpha;
                    pixel.a = alpha;

                    resultPixels[i] = pixel;
                }
            }

            resultTexture.SetPixels(resultPixels);
            resultTexture.Apply();
        }
    }
}
