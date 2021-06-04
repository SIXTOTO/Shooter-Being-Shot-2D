using UnityEngine;

namespace ABS
{
    public class IntegerVector
    {
        public int x, y;

        public IntegerVector(Vector3 vector3)
        {
            this.x = (int)vector3.x;
            this.y = (int)vector3.y;
        }

        public IntegerVector(IntegerVector other)
        {
            this.x = other.x;
            this.y = other.y;
        }

        public IntegerVector(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public override string ToString()
        {
            return x + ", " + y;
        }

        public IntegerVector Copy()
        {
            return new IntegerVector(x, y);
        }

        public static IntegerVector operator+(IntegerVector p1, IntegerVector p2)
        {
            return new IntegerVector(p1.x + p2.x, p1.y + p2.y);
        }

        public static IntegerVector operator-(IntegerVector p1, IntegerVector p2)
        {
            return new IntegerVector(p1.x - p2.x, p1.y - p2.y);
        }

        public void SubtractWithMargin(IntegerVector pos, int margin)
        {
            x -= (pos.x - margin);
            y -= (pos.y - margin);
        }
    }
}
