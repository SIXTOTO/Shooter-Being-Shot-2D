namespace ABS
{
    public class Frame
    {
        public static Frame BEGIN = new Frame(0, 0);

        public int index;
        public float time;

        public Frame(int index, float time)
        {
            this.index = index;
            this.time = time;
        }

        public override bool Equals(object obj)
        {
            Frame other = (Frame)obj;
            return index == other.index;
        }

        public override int GetHashCode()
        {
            return index;
        }
    }
}
