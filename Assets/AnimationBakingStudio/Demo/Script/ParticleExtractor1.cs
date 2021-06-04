using System.Linq;
using UnityEngine;

namespace ABS.Demo
{
    public class ParticleExtractor1 : Extractor
    {
        [SerializeField]
        private Shader uniformShader;
        [SerializeField]
        private AlpahExtractionChannel alphaExtractionChannel;

        [SerializeField, Range(0, 1)]
        private float alphaThreshold = 0.0f;

        public override void Extract(Camera camera, Model model, ref Texture2D resultTexture)
        {
            if (uniformShader != null)
            {
                model.BackupAllShaders();
                model.ChangeAllShaders(uniformShader);
            }

            Color[] pixelsOnBlack = ReadCameraPixels(camera, Color.black);
            Color[] pixelsOnWhite = ReadCameraPixels(camera, Color.white);
            Color[] resultPixels = Enumerable.Repeat(Color.clear, pixelsOnBlack.Length).ToArray();

            for (int i = 0; i < resultTexture.width * resultTexture.height; i++)
            {
                Color pixelOnBlack = pixelsOnBlack[i];
                Color pixelOnWhite = pixelsOnWhite[i];

                float alpha = 1.0f - ExtractAlpha(pixelOnBlack, pixelOnWhite, alphaExtractionChannel);
                if (alpha <= alphaThreshold)
                    continue;

                Color pixel = pixelOnBlack / alpha;
                pixel.a = alpha;

                resultPixels[i] = pixel;
            }

            resultTexture.SetPixels(resultPixels);
            resultTexture.Apply();

            if (uniformShader != null)
                model.RestoreAllShaders();
        }
    }
}
