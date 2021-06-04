using System;

namespace ABS
{
    public enum PackingMethod
    {
        Optimized,
        InOrder
    }

    [Serializable]
    public class PackingProperty : PropertyBase
    {
        public bool on = true;
        public PackingMethod method = PackingMethod.Optimized;
        public int maxAtlasSizeIndex = 5;
        public int minAtlasSizeIndex = 1;
        public int padding = 0;
    }
}
