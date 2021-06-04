using System;

namespace ABS
{
    [Serializable]
    public class TrimmingProperty : PropertyBase
    {
        public bool on = true;
        public int margin = 2;
        public bool isUnifiedForAllViews = false;
    }
}
