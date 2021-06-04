using System;
using UnityEngine;

namespace ABS
{
    [Serializable]
    public class FrameProperty : PropertyBase
    {
        public Resolution resolution = new Resolution(500, 400);
        public int size = 10;
        public int simulatedIndex = 0;
        public double delay = 0;
    }
}
