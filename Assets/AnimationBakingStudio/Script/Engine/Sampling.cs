using UnityEngine;

namespace ABS
{
    public class Sampling
    {
        public Texture2D tex;
        public float time;

        public Sampling(Texture2D tex, float time)
        {
            this.tex = tex;
            this.time = time;
        }
    }
}
