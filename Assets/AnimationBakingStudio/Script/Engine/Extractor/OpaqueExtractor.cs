using System.Linq;
using UnityEngine;

namespace ABS
{
	public class OpaqueExtractor : Extractor
    {
        public override void Extract(Camera camera, Model model, ref Texture2D resultTexture)
        {
            ExtractionHelper.ExtractOpqaue(ref resultTexture, EngineGlobal.CLEAR_COLOR32);
        }
    }
}
