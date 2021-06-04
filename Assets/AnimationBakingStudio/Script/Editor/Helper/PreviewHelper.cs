using UnityEngine;

namespace ABS
{
    public class PreviewHelper
    {
        public static Rect ScalePreviewRect(Texture tex, Rect rect)
        {
            float aspectRatio = (float)tex.width / (float)tex.height;

            float widthScaleRatio = tex.width / rect.width;
            float heightScaleRatio = tex.height / rect.height;

            float scaledWidth = rect.width, scaledHeight = rect.height;

            if (tex.width > rect.width && tex.height > rect.height)
            {
                if (widthScaleRatio < heightScaleRatio)
                    ScaleByHeight(rect.height, aspectRatio, out scaledWidth, out scaledHeight);
                else
                    ScaleByWidth(rect.width, aspectRatio, out scaledWidth, out scaledHeight);
            }
            else if (tex.width > rect.width && tex.height < rect.height)
            {
                ScaleByHeight(rect.height, aspectRatio, out scaledWidth, out scaledHeight);
                ScaleMoreByWidthIfOver(rect.width, ref scaledWidth, ref scaledHeight);
            }
            else if (tex.width < rect.width && tex.height > rect.height)
            {
                ScaleByWidth(rect.width, aspectRatio, out scaledWidth, out scaledHeight);
                ScaleMoreByHeightIfOver(rect.height, ref scaledWidth, ref scaledHeight);
            }
            else
            {
                if (widthScaleRatio < heightScaleRatio)
                {
                    ScaleByHeight(rect.height, aspectRatio, out scaledWidth, out scaledHeight);
                    ScaleMoreByWidthIfOver(rect.width, ref scaledWidth, ref scaledHeight);
                }
                else
                {
                    ScaleByWidth(rect.width, aspectRatio, out scaledWidth, out scaledHeight);
                    ScaleMoreByHeightIfOver(rect.height, ref scaledWidth, ref scaledHeight);
                }
            }

            float scaledX = rect.x + (rect.width - scaledWidth) / 2;
            float scaledY = rect.y + (rect.height - scaledHeight) / 2;

            return new Rect(scaledX, scaledY, scaledWidth, scaledHeight);
        }

        private static void ScaleByHeight(float height, float aspectRatio, out float outWidth, out float outHeight)
        {
            outWidth = height * aspectRatio;
            outHeight = height;
        }

        private static void ScaleByWidth(float width, float aspectRatio, out float outWidth, out float outHeight)
        {
            outWidth = width;
            outHeight = width / aspectRatio;
        }

        private static void ScaleMoreByWidthIfOver(float width, ref float scaledWidth, ref float scaledHeight)
        {
            if (scaledWidth > width)
            {
                float scale = scaledWidth / width;
                scaledWidth /= scale;
                scaledHeight /= scale;
            }
        }

        private static void ScaleMoreByHeightIfOver(float height, ref float scaledWidth, ref float scaledHeight)
        {
            if (scaledHeight > height)
            {
                float scale = scaledHeight / height;
                scaledWidth /= scale;
                scaledHeight /= scale;
            }
        }
    }
}
