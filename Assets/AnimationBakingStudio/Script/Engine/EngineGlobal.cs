using UnityEngine;

namespace ABS
{
    public delegate void CompletionCallback();

    public class EngineGlobal
    {
        public const string PROJECT_NAME = "Animation Baking Studio";
        public const string PROJECT_PATH_NAME = "AnimationBakingStudio";

        public static readonly Color32 CLEAR_COLOR32 = new Color32(0, 0, 0, 0);
        public static readonly Color32 NORMALMAP_COLOR32 = new Color32(127, 127, 255, 255);

        public const int GPU_THREAD_SIZE = 1024;
        public static bool gpuUse = false;
    }
}
