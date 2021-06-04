using System;
using UnityEngine;

namespace ABS
{
    [Serializable]
    public class LightProperty : PropertyBase
    {
        public Light com = null;
        public float slopeAngle = 70f;
        public float turnAngle = 0f;
        public bool cameraPositionFollow = true;
        public bool cameraRotationFollow = true;
        public Vector3 pos = Vector3.zero;
    }
}
