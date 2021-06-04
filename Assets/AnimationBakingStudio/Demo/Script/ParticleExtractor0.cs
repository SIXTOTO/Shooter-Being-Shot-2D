using System.Linq;
using UnityEngine;

namespace ABS.Demo
{
    public class ParticleExtractor0 : Extractor
    {
        [SerializeField]
        private Color backgroundColor = Color.black;

        [SerializeField, Range(0, 1)]
        private float colorThreshold = 0;

        public override void Extract(Camera camera, Model model, ref Texture2D resultTexture)
        {
            Color[] pixelsOnBlack = ReadCameraPixels(camera, backgroundColor);
            Color[] resultPixels = Enumerable.Repeat(Color.clear, pixelsOnBlack.Length).ToArray();

            for (int i = 0; i < resultTexture.width * resultTexture.height; i++)
            {
                Color pixel = pixelsOnBlack[i];

                if (colorThreshold == 0)
                {
                    if (pixel == backgroundColor)
                        continue;
                }
                else
                {
                    Vector3 colorVector1 = new Vector3(pixel.r, pixel.g, pixel.b);
                    Vector3 colorVector2 = new Vector3(backgroundColor.r, backgroundColor.g, backgroundColor.b);
                    if ((colorVector1 - colorVector2).magnitude < colorThreshold)
                        continue;
                }

                resultPixels[i] = pixel;
            }

            resultTexture.SetPixels(resultPixels);
            resultTexture.Apply();
        }
    }
}
