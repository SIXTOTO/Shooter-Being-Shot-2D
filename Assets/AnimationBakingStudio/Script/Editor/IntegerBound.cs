namespace ABS
{
    public class IntegerBound
    {
        public IntegerVector min;
        public IntegerVector max;

        public IntegerBound()
        {
            min = new IntegerVector(int.MaxValue, int.MaxValue);
            max = new IntegerVector(int.MinValue, int.MinValue);
        }

        public IntegerBound(int minX, int maxX, int minY, int maxY)
        {
            min = new IntegerVector(minX, minY);
            max = new IntegerVector(maxX, maxY);
        }

        public IntegerBound(IntegerVector min, IntegerVector max)
        {
            this.min = min.Copy();
            this.max = max.Copy();
        }

        public IntegerBound Copy()
        {
            return new IntegerBound(min, max);
        }

        public IntegerBound CopyExtendedBy(int ex)
        {
            return new IntegerBound(min.x - ex, max.x + ex, min.y - ex, max.y + ex);
        }
    };
}
