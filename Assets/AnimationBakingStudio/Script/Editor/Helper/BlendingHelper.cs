using UnityEngine;

namespace ABS
{
    public class BlendingHelper
    {
        public static Color BlendTwoColors(Color srcColor, Color dstColor, BlendFactor srcFactor, BlendFactor dstFactor)
        {
            return srcColor * MakeBlendFactor(srcColor, dstColor, srcFactor) +
                   dstColor * MakeBlendFactor(srcColor, dstColor, dstFactor);
        }

        public static Color MakeBlendFactor(Color srcPixel, Color dstPixel, BlendFactor factor)
        {
            switch (factor)
            {
                case BlendFactor.Zero:
                    return Color.clear;

                case BlendFactor.One:
                    return Color.white;

                case BlendFactor.SrcColor:
                    return new Color(
                        srcPixel.r,
                        srcPixel.g,
                        srcPixel.b,
                        srcPixel.a);

                case BlendFactor.OneMinusSrcColor:
                    return new Color(
                        (1f - srcPixel.r),
                        (1f - srcPixel.g),
                        (1f - srcPixel.b),
                        (1f - srcPixel.a));

                case BlendFactor.DstColor:
                    return new Color(
                        dstPixel.r,
                        dstPixel.g,
                        dstPixel.b,
                        dstPixel.a);

                case BlendFactor.OneMinusDstColor:
                    return new Color(
                        (1f - dstPixel.r),
                        (1f - dstPixel.g),
                        (1f - dstPixel.b),
                        (1f - dstPixel.a));

                case BlendFactor.SrcAlpha:
                    return new Color(
                        srcPixel.a,
                        srcPixel.a,
                        srcPixel.a,
                        srcPixel.a);

                case BlendFactor.OneMinusSrcAlpha:
                    return new Color(
                        (1f - srcPixel.a),
                        (1f - srcPixel.a),
                        (1f - srcPixel.a),
                        (1f - srcPixel.a));

                case BlendFactor.DstAlpha:
                    return new Color(
                        dstPixel.a,
                        dstPixel.a,
                        dstPixel.a,
                        dstPixel.a);

                case BlendFactor.OneMinusDstAlpha:
                    return new Color(
                        (1f - dstPixel.a),
                        (1f - dstPixel.a),
                        (1f - dstPixel.a),
                        (1f - dstPixel.a));
            }

            return Color.white;
        }
    }
}
