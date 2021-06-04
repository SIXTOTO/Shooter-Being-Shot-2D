using System.Linq;
using UnityEngine;

namespace ABS.Demo
{
    public class ParticleExtractor2 : Extractor
    {
        [SerializeField]
        private Shader alphaUniformShader;
        [SerializeField]
        private AlpahExtractionChannel alphaExtractionChannel;

        [SerializeField]
        private Shader colorUniformShader;
        [SerializeField]
        private ColorExtractionBackground colorExtractionBackground;

        [SerializeField, Range(0, 1)]
        private float alphaThreshold = 0.0f;

        public override void Extract(Camera camera, Model model, ref Texture2D resultTexture)
        {
            if (alphaUniformShader != null || colorUniformShader != null)
                model.BackupAllShaders();

            if (alphaUniformShader != null)
                model.ChangeAllShaders(alphaUniformShader);

            Color[] aePixelsOnBlack = ReadCameraPixels(camera, Color.black);
            Color[] aePixelsOnWhite = ReadCameraPixels(camera, Color.white);

            if (colorUniformShader)
            {
                model.ChangeAllShaders(colorUniformShader);
            }
            else
            {
                if (alphaUniformShader)
                    model.RestoreAllShaders();
            }

            Color[] cePixelsOnBlack = ReadCameraPixels(camera, Color.black);
            Color[] cePixelsOnWhite = ReadCameraPixels(camera, Color.white);

            if (colorUniformShader)
                model.RestoreAllShaders();

            Color[] resultPixels = Enumerable.Repeat(Color.clear, aePixelsOnBlack.Length).ToArray();

            for (int i = 0; i < resultTexture.width * resultTexture.height; i++)
            {
                float alpha = 1.0f - ExtractAlpha(aePixelsOnBlack[i], aePixelsOnWhite[i], alphaExtractionChannel);
                if (alpha <= alphaThreshold)
                    continue;

                Color cePixel = Color.clear;
                if (colorExtractionBackground == ColorExtractionBackground.Black)
                    cePixel = cePixelsOnBlack[i];
                else if (colorExtractionBackground == ColorExtractionBackground.White)
                    cePixel = cePixelsOnWhite[i];

                Color pixel = cePixel / alpha;
                pixel.a = alpha;

                resultPixels[i] = pixel;
            }

            resultTexture.SetPixels(resultPixels);
            resultTexture.Apply();
        }
    }
}
