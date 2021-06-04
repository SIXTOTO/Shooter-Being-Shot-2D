using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
#endif

namespace ABS
{
	public class PrefabBuilder : MonoBehaviour
	{
#if UNITY_EDITOR
		public static EditorCurveBinding GetDefaultSpriteCurveBinding()
		{
			return EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
		}

		public virtual EditorCurveBinding MakeSpriteCurveBinding()
		{
			return GetDefaultSpriteCurveBinding();
		}

		public virtual void BindSpriteAndController(GameObject rootObject, Sprite firstSprite, AnimatorController controller) { }
#endif

		public virtual void BindSprite(GameObject rootObject, Sprite firstSprite) { }

		public virtual void BindMaterialAndTextures(GameObject rootObject, Material material, List<AnimationTextureData> animationTextureDataList) { }

		public virtual Transform GetLocationsParent(GameObject rootObject)
		{
			return rootObject.transform;
		}

		public virtual BoxCollider2D GetBoxCollider2D(GameObject rootObject)
		{
			return rootObject.GetComponentInChildren<BoxCollider2D>();
		}
	}
}
