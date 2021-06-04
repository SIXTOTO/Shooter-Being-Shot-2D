using System;
using UnityEngine;

namespace ABS
{
    public enum ShadowType
    {
        None,
        Simple,
        TopDown,
        Matte
    }

    [Serializable]
    public class ShadowProperty : PropertyBase
    {
        public ShadowType type = ShadowType.None;
        public GameObject obj = null;
        public SimpleShadowProperty simple = new SimpleShadowProperty();
        public float opacity = 0.8f;
        public bool shadowOnly = false;
    }
}
