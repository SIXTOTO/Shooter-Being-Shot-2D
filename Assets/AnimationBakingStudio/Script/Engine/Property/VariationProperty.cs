using System;
using UnityEngine;

namespace ABS
{
    public enum BlendFactor
    {
        Zero,
        One,
        SrcColor,
        OneMinusSrcColor,
        DstColor,
        OneMinusDstColor,
        SrcAlpha,
        OneMinusSrcAlpha,
        DstAlpha,
        OneMinusDstAlpha
    }

    [Serializable]
    public class VariationProperty : PropertyBase
    {
        public bool on = false;
        public Color color = new Color(1f, 0f, 0f, .5f);
        public BlendFactor colorBlendFactor = BlendFactor.SrcAlpha;
        public BlendFactor imageBlendFactor = BlendFactor.OneMinusSrcAlpha;
        public bool excludeShadow = true;
    }
}
