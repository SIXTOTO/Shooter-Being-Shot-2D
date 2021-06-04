using System;
using UnityEngine;

namespace ABS
{
    public enum PreviewBackgroundType
    {
        Checker,
        SingleColor
    }

    [Serializable]
    public class PreviewProperty : PropertyBase
    {
        public bool on = false;
        public bool isNormalMap = false;
        public PreviewBackgroundType backgroundType = PreviewBackgroundType.Checker;
        public Color backgroundColor = Color.black;
    }
}
